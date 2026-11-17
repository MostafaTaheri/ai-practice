using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public record EvalCase(
    string Id, string Split, string Proposal, string Question,
    string[] ExpectedSourceIds, string ExpectedStatus);

public record CallResult(
    int? HttpStatus, double ElapsedMs, string Body, string? Error);

public record Hit(string Id, string Text, string Revision, double Score);

public sealed class CaseResult
{
    public required EvalCase Case { get; init; }
    public required CallResult SearchCall { get; init; }
    public required CallResult ReviewCall { get; init; }
    public bool RetrievalContractOk { get; set; }
    public bool RetrievalSnapshotOk { get; set; }
    public double? RecallAt3 { get; set; }
    public bool ReviewContractOk { get; set; }
    public bool ReviewSnapshotOk { get; set; }
    public bool CitationIntegrityOk { get; set; }
    public bool ExpectedSourceCited { get; set; }
    public bool StatusCorrect { get; set; }
    public bool SameRetrieval { get; set; }
    public bool SameEmbeddingModel { get; set; }
    public string? EmbeddingModel { get; set; }
    public string? PromptVersion { get; set; }
    public string? ObservedStatus { get; set; }
    public List<string> Errors { get; } = [];

    // A mechanical learning gate, not an assessment of claim support.
    public bool MechanicalPass =>
        RetrievalContractOk && RetrievalSnapshotOk
        && ReviewContractOk && ReviewSnapshotOk
        && CitationIntegrityOk && StatusCorrect
        && SameRetrieval && SameEmbeddingModel
        && (Case.ExpectedSourceIds.Length == 0
            || (RecallAt3 == 1 && ExpectedSourceCited));
}

public static class Scoring
{
    public static string Hash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static JsonElement Parse(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    // The API wraps every payload as { data, success, errors }.
    public static JsonElement Unwrap(string body)
    {
        var envelope = Parse(body);
        Keys(envelope, "data", "success", "errors");
        if (envelope.GetProperty("success").ValueKind != JsonValueKind.True)
            throw new InvalidDataException("Envelope reports failure.");
        return envelope.GetProperty("data");
    }

    public static string Text(JsonElement value, string name)
    {
        var item = value.GetProperty(name);
        if (item.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(item.GetString()))
            throw new InvalidDataException("Missing string: " + name);
        return item.GetString()!;
    }

    public static void Keys(JsonElement value, params string[] keys)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Expected an object.");
        var names = value.EnumerateObject().Select(p => p.Name).ToArray();
        if (names.Length != keys.Length
            || !names.ToHashSet(StringComparer.Ordinal).SetEquals(keys))
            throw new InvalidDataException("Unexpected or missing fields.");
    }

    private static JsonElement[] ReadArray(
        JsonElement value, string name, int maximum)
    {
        var array = value.GetProperty(name);
        if (array.ValueKind != JsonValueKind.Array
            || array.GetArrayLength() > maximum)
            throw new InvalidDataException("Invalid array: " + name);
        return array.EnumerateArray().ToArray();
    }

    private static List<Hit> ReadHits(JsonElement root)
    {
        var hits = new List<Hit>();
        foreach (var item in ReadArray(root, "retrievedSources", 3))
        {
            Keys(item, "id", "title", "text", "revision", "score");
            var source = item;
            _ = Text(source, "title");
            var score = item.GetProperty("score").GetDouble();
            if (!double.IsFinite(score) || Math.Abs(score) > 1.000001)
                throw new InvalidDataException("Invalid cosine score.");
            hits.Add(new Hit(
                Text(source, "id"), Text(source, "text"),
                Text(source, "revision"), score));
        }
        if (hits.Count != 3
            || hits.Select(h => h.Id).Distinct().Count() != hits.Count)
            throw new InvalidDataException("Expected 3 distinct candidates.");
        if (hits.Zip(hits.Skip(1)).Any(pair =>
            pair.First.Score < pair.Second.Score))
            throw new InvalidDataException("Candidates are not ranked.");
        return hits;
    }

    private static bool MatchesSnapshot(
        List<Hit> hits, IReadOnlyDictionary<string, string> corpus) =>
        hits.All(hit => corpus.TryGetValue(hit.Id, out var text)
            && text == hit.Text && Hash(text) == hit.Revision);

    public static CaseResult Evaluate(
        EvalCase test, CallResult search, CallResult review,
        IReadOnlyDictionary<string, string> corpus)
    {
        var result = new CaseResult
        {
            Case = test, SearchCall = search, ReviewCall = review
        };
        List<Hit>? searchHits = null;

        try
        {
            if (search.HttpStatus != 200)
                throw new InvalidDataException("Search did not return HTTP 200.");
            var root = Unwrap(search.Body);
            Keys(root, "retrievedSources", "embeddingModel");
            result.EmbeddingModel = Text(root, "embeddingModel");
            searchHits = ReadHits(root);
            result.RetrievalContractOk = true;
            result.RetrievalSnapshotOk = MatchesSnapshot(searchHits, corpus);

            if (result.RetrievalSnapshotOk
                && test.ExpectedSourceIds.Length > 0)
                result.RecallAt3 = (double)test.ExpectedSourceIds.Count(id =>
                    searchHits.Any(hit => hit.Id == id))
                    / test.ExpectedSourceIds.Length;
        }
        catch (Exception exception) when (
            exception is JsonException or InvalidDataException
            or KeyNotFoundException or InvalidOperationException
            or FormatException)
        {
            result.Errors.Add("Retrieval: " + exception.GetType().Name);
        }

        try
        {
            if (review.HttpStatus != 200)
                throw new InvalidDataException("Review did not return HTTP 200.");
            var root = Unwrap(review.Body);
            Keys(root, "assessment", "retrievedSources",
                "embeddingModel", "promptVersion");
            var reviewHits = ReadHits(root);
            var model = Text(root, "embeddingModel");
            result.PromptVersion = Text(root, "promptVersion");
            result.ReviewSnapshotOk = MatchesSnapshot(reviewHits, corpus);
            result.SameEmbeddingModel = model == result.EmbeddingModel;
            result.SameRetrieval = searchHits is not null
                && searchHits.Select(hit => (hit.Id, hit.Revision))
                    .SequenceEqual(reviewHits.Select(hit =>
                        (hit.Id, hit.Revision)));

            var assessment = root.GetProperty("assessment");
            Keys(assessment, "status", "findings", "unresolvedQuestions");
            var status = Text(assessment, "status");
            if (status is not ("supported" or "insufficient_evidence"))
                throw new InvalidDataException("Unknown assessment status.");

            var findings = ReadArray(assessment, "findings", 3);
            var questions = ReadArray(assessment, "unresolvedQuestions", 5);
            if (questions.Any(q => q.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(q.GetString())
                || q.GetString()!.Length > 500))
                throw new InvalidDataException("Invalid questions.");
            if ((status == "supported" && findings.Length == 0)
                || (status == "insufficient_evidence"
                    && (findings.Length != 0 || questions.Length == 0)))
                throw new InvalidDataException("Inconsistent status.");

            var citations = new List<(string SourceId, string Quote)>();
            foreach (var finding in findings)
            {
                Keys(finding, "guidance", "sourceId", "quote");
                if (Text(finding, "guidance").Length > 1000
                    || Text(finding, "quote").Length > 600)
                    throw new InvalidDataException("Overlong finding.");
                citations.Add((
                    Text(finding, "sourceId"), Text(finding, "quote")));
            }

            result.ReviewContractOk = true;
            result.ObservedStatus = status;
            result.StatusCorrect = status == test.ExpectedStatus;
            result.CitationIntegrityOk = result.ReviewSnapshotOk
                && citations.All(citation => reviewHits.Any(hit =>
                    hit.Id == citation.SourceId
                    && hit.Text.Contains(
                        citation.Quote, StringComparison.Ordinal)));
            result.ExpectedSourceCited = test.ExpectedSourceIds.Length > 0
                && citations.Any(citation =>
                    test.ExpectedSourceIds.Contains(citation.SourceId));
        }
        catch (Exception exception) when (
            exception is JsonException or InvalidDataException
            or KeyNotFoundException or InvalidOperationException
            or FormatException)
        {
            result.Errors.Add("Review: " + exception.GetType().Name);
        }

        return result;
    }
}
