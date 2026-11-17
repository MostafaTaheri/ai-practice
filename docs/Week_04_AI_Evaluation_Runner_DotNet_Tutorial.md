# Week 4 — Build a .NET Evaluation Runner for Your AI Architecture Assistant

**Prepared for:** Mostafa Taheri — Technical Lead | Software Architect  
**Budget:** 3 hours  
**Practice feature:** A console runner that saves evaluation evidence and compares baseline/candidate runs  
**Prerequisite:** Week 3 API and its ten synthetic standards work locally  
**Scope:** Evaluate the Week 3 RAG endpoint; preserve the earlier review and summarization features.

## 1. The outcome

At the end of this lesson, you can run a fixed set of architecture cases, inspect retrieval separately from generation, and compare a change against a saved baseline.

The runner checks:

- Whether the expected document was retrieved.
- Whether the API response matches the contract.
- Whether returned sources match the local corpus snapshot.
- Whether citations identify retrieved sources and quote real text.
- Whether the model answered or abstained as expected.
- Whether baseline and candidate cases changed from mechanical pass to fail.

It also preserves raw responses, timing, model labels, prompt version, and source provenance. A human still judges whether claims follow from quotes and whether the guidance helps an architecture decision.

**Important limitation:** ten synthetic cases are a learning regression suite. They do not establish production readiness, security, or general model quality.

## 2. Your three-hour session

| Session | Minutes | Work |
|---|---:|---|
| Study | 30 | Understand the metrics, evaluation split, and human rubric |
| Practice | 60 | Add a retrieval diagnostic endpoint; create the console runner using the supplied code |
| Project | 90 | Run self-tests, collect a baseline, make one controlled change, compare, and inspect answers |

Suggested project split: 10 minutes for self-tests, 20 for baseline collection/inspection, 25 for a single candidate change and comparison, 20 for manual grading, and 15 for reserved validation and notes. Slow inference can exceed these estimates. Stop at the time budget and continue the same week rather than skipping analysis.

### Selected reading

| Resource | Difficulty | Selected time | Read |
|---|---|---:|---|
| [HttpClient guidelines — Microsoft](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) | Intermediate | 5 min | Client lifetime and timeout considerations |
| [Required properties — System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/required-properties) | Intermediate | 5 min | Why deserialization alone is not a complete acceptance policy |
| [List local models — Ollama](https://docs.ollama.com/api/tags) | Beginner | 5 min | Capture model tags/digests as experiment evidence |
| [AI Engineering — official contents](https://github.com/chiphuyen/aie-book/blob/main/ToC.md) | Intermediate | Optional: replace 10 min of study | Book Ch. 3–4, component evaluation and evaluation guidelines |

The documentation is free; the book is optional and is the only paid reference. Spend the remaining 15 study minutes on the metric table and human rubric below. Times are for selected sections. Live documentation retrieval was unavailable during authoring; these links are provided as reference material, not claimed as freshly verified product behavior.

## 3. Define success before changing the prompt

| Measure | Definition in this lesson | What it does not establish |
|---|---|---|
| Retrieval contract | Search response has three distinct ranked candidates with valid fields | That candidates are relevant |
| Recall@3 | Expected relevant source IDs found in top three / expected relevant source IDs | Whether generation used them well |
| Snapshot match | Returned source text and revision equal the local corpus | Whether the document is correct or current business policy |
| Review contract | Required fields, allowed status, lengths, and array rules pass | Truth of the content |
| Citation integrity | A finding references a retrieved source and quotes its text exactly | That its conclusion follows |
| Expected source cited | At least one labeled relevant source appears in a finding | Full coverage of all required evidence |
| Correct abstention | An unanswerable case returns insufficient_evidence | Appropriate behavior on all unknown questions |
| False abstention | An answerable case returns insufficient_evidence | Why evidence was missed or ignored |
| Same retrieval | Diagnostic and generation requests return the same ordered source IDs/revisions | Identical scores or internal computation |
| Human support grade | Reviewer judges claims against the source and proposal | A statistically representative quality estimate |

For this dataset there is one labeled relevant source per answerable case. Recall@3 therefore equals Hit@3 per case. Unanswerable cases have no retrieval-recall denominator.

A non-200 response is an operational/contract failure. It is not a correct abstention. An unavailable retrieval score is saved as null; the aggregate learning-gate recall treats it as zero and reports how many scores were available.

MechanicalPass combines the automated checks. It is deliberately stricter than “the endpoint returned 200” but does not include human claim support.

## 4. Expose retrieval independently

Week 3 returns source candidates only when the whole grounded review succeeds. A generation failure can otherwise hide a good retrieval result.

Add this mapping to the API's Program.cs **after index initialization and before app.Run()**. It uses the existing standardsIndex and OllamaEmbeddingClient.

```csharp
app.MapPost("/standards/search", async (
    StandardsSearchRequest request,
    OllamaEmbeddingClient embeddings,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question)
        || request.Question.Length > 200)
        return Results.BadRequest(new
        {
            error = "Question must contain 1–200 characters."
        });

    using var deadline =
        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    deadline.CancelAfter(TimeSpan.FromSeconds(30));

    try
    {
        if (embeddings.Model != standardsIndex.EmbeddingModel)
            throw new InvalidDataException("Embedding model changed.");

        var vectors = await embeddings.EmbedAsync(
            [request.Question], deadline.Token);

        return Results.Ok(new
        {
            retrievedSources = standardsIndex.Search(vectors[0]),
            embeddingModel = standardsIndex.EmbeddingModel
        });
    }
    catch (OperationCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        return Results.Problem(
            title: "Retrieval deadline exceeded.", statusCode: 504);
    }
    catch (HttpRequestException)
    {
        return Results.Problem(
            title: "Embedding dependency failed.", statusCode: 502);
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.Problem(
            title: "Invalid embedding response.", statusCode: 502);
    }
    catch (InvalidDataException)
    {
        return Results.Problem(
            title: "Embedding validation failed.", statusCode: 502);
    }
});
```

Add this type at the end of Program.cs, alongside its existing record declarations:

```csharp
public record StandardsSearchRequest(string? Question);
```

The search endpoint does not generate an answer. Keep it local and limited to this synthetic learning corpus. If your application later contains restricted documents, it needs the same caller authorization as the RAG path.

Restart the API:

```bash
dotnet build AIPractice.slnx
dotnet run --project src/AIPractice.Api --no-launch-profile --urls http://localhost:5187
```

Quick smoke request:

```bash
curl --max-time 35 http://localhost:5187/api/v1/standards/search \
  -H 'Content-Type: application/json' \
  -d '{"question":"How should accepted claims survive an insurer outage?"}'
```

Expect three candidates and an embeddingModel label. Verify STD-001 appears before making generation changes.

## 5. Create the console project

In this repository the runner already lives at test/AIPractice.Evaluation (a sibling of the API under src/). If you are starting from scratch, create it from the repository root:

```bash
dotnet new console -n AIPractice.Evaluation -o test/AIPractice.Evaluation
dotnet sln AIPractice.slnx add test/AIPractice.Evaluation
```

Use .NET 8 or newer with C# 12. No new NuGet package or API project reference is required. The runner checks response JSON independently instead of reusing the API validator.

The tutorial supplies three code files and one dataset. Keep the default namespace consistent and place them directly in the console project.

## 6. Freeze cases.json

Create cases.json with the following content. Do not edit this file between baseline and candidate collection. Its hash is saved in each report.

E01–E08 are development cases. Reserve E09–E10 for validation after choosing a candidate. Since the cases are visible here, they are not a truly unseen external benchmark. Do not tune against repeated validation results and continue calling them held out.

```json
[
  {
    "id": "E01",
    "split": "development",
    "proposal": "An insurance back office calls external insurers over HTTP. Claims must remain accepted during an insurer outage. Durable intake has not been designed.",
    "question": "How should accepted claims survive an insurer outage?",
    "expectedSourceIds": [
      "STD-001"
    ],
    "expectedStatus": "supported"
  },
  {
    "id": "E02",
    "split": "development",
    "proposal": "A staff portal exposes customer financial reports. Authentication is planned, but report authorization has not been defined.",
    "question": "How should report access be controlled?",
    "expectedSourceIds": [
      "STD-002"
    ],
    "expectedStatus": "supported"
  },
  {
    "id": "E03",
    "split": "development",
    "proposal": "Two engineers will build and operate twelve independently deployed services. No separate scaling or release needs have been identified.",
    "question": "What should guide the choice of service boundaries for this small team?",
    "expectedSourceIds": [
      "STD-003"
    ],
    "expectedStatus": "supported"
  },
  {
    "id": "E04",
    "split": "development",
    "proposal": "The domain project directly references a model-provider SDK and a database client.",
    "question": "How should domain rules be separated from provider and database implementations?",
    "expectedSourceIds": [
      "STD-004"
    ],
    "expectedStatus": "supported"
  },
  {
    "id": "E05",
    "split": "development",
    "proposal": "A message consumer applies account changes whenever a message arrives. Duplicate deliveries are possible.",
    "question": "How should consumers handle duplicate message deliveries?",
    "expectedSourceIds": [
      "STD-005"
    ],
    "expectedStatus": "supported"
  },
  {
    "id": "E06",
    "split": "development",
    "proposal": "All modules must share one database, but each module must own an independent database. Data ownership has not been resolved.",
    "question": "How should conflicting data ownership requirements be handled?",
    "expectedSourceIds": [
      "STD-006"
    ],
    "expectedStatus": "supported"
  },
  {
    "id": "E07",
    "split": "development",
    "proposal": "The team wants to choose hosting. No approved spending figure appears in the proposal.",
    "question": "What exact monthly infrastructure budget in dollars has our organization approved?",
    "expectedSourceIds": [],
    "expectedStatus": "insufficient_evidence"
  },
  {
    "id": "E08",
    "split": "development",
    "proposal": "The organization is discussing hosting customer data in Australia.",
    "question": "What exact Australian data-residency policy has our organization approved?",
    "expectedSourceIds": [],
    "expectedStatus": "insufficient_evidence"
  },
  {
    "id": "E09",
    "split": "validation",
    "proposal": "Backups are configured for a claims service, but no restore drill or recovery objectives exist.",
    "question": "How should recovery objectives and restore readiness be established?",
    "expectedSourceIds": [
      "STD-009"
    ],
    "expectedStatus": "supported"
  },
  {
    "id": "E10",
    "split": "validation",
    "proposal": "The staff portal creates audit logs. No retention policy has been supplied.",
    "question": "For exactly how many years does our approved organizational policy require retaining audit logs?",
    "expectedSourceIds": [],
    "expectedStatus": "insufficient_evidence"
  }
]
```

The fixtures refer to Week 3’s exact ten-document corpus. E07–E08 and E10 ask for specific organizational policies or figures that those documents do not provide. A general cost/observability policy is not evidence of an approved dollar amount or retention period.

This small set does not cover every standard or failure mode. Document those gaps rather than inflating its scope.

## 7. Add Scoring.cs

The scorer rejects missing and unexpected response fields. It checks citations against source text loaded from your local standards directory, not only against text asserted by the API.

Each result stores retrieval and review failures separately. For a valid abstention, zero findings make citation integrity vacuously true; do not interpret that as successful citation use. Inspect answerable cases and their cited sources separately.

```csharp
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

    public static string Text(JsonElement value, string name)
    {
        var item = value.GetProperty(name);
        if (item.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(item.GetString()))
            throw new InvalidDataException("Missing string: " + name);
        return item.GetString()!;
    }

    private static void Keys(JsonElement value, params string[] keys)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Expected an object.");
        var names = value.EnumerateObject().Select(p => p.Name).ToArray();
        if (names.Length != keys.Length
            || !names.ToHashSet(StringComparer.Ordinal).SetEquals(keys))
            throw new InvalidDataException("Unexpected or missing fields.");
    }

    private static JsonElement[] Array(
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
        foreach (var item in Array(root, "retrievedSources", 3))
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
            var root = Parse(search.Body);
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
            var root = Parse(review.Body);
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

            var findings = Array(assessment, "findings", 3);
            var questions = Array(assessment, "unresolvedQuestions", 5);
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
```

## 8. Replace Program.cs in the console project

The runner makes calls sequentially and does not automatically retry failed cases. Each case receives one independent search request and one grounded-review request. The latter performs its own retrieval inside the API, so two query embeddings occur per case.

Search results and RAG results are compared for consistency. If source/model changes or ranking differences appear, treat the run as unstable and investigate before interpreting quality changes.

```csharp
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
};

if (args.Length == 3 && args[0] == "compare")
{
    var oldRoot = Scoring.Parse(await File.ReadAllTextAsync(args[1]));
    var newRoot = Scoring.Parse(await File.ReadAllTextAsync(args[2]));

    if (!oldRoot.GetProperty("completed").GetBoolean()
        || !newRoot.GetProperty("completed").GetBoolean())
    {
        Console.Error.WriteLine("Comparison blocked: incomplete run.");
        return 2;
    }

    foreach (var key in new[] { "datasetHash", "split", "scorerVersion" })
        if (Scoring.Text(oldRoot, key) != Scoring.Text(newRoot, key))
        {
            Console.Error.WriteLine("Comparison blocked: mismatch in " + key);
            return 2;
        }

    if (Scoring.Text(oldRoot, "corpusHash")
        != Scoring.Text(newRoot, "corpusHash"))
        Console.WriteLine(
            "Corpus changed: interpret this as a corpus experiment.");

    var oldRows = oldRoot.GetProperty("results").EnumerateArray()
        .ToDictionary(row => Scoring.Text(row.GetProperty("case"), "id"));
    var newRows = newRoot.GetProperty("results").EnumerateArray()
        .ToDictionary(row => Scoring.Text(row.GetProperty("case"), "id"));
    if (!oldRows.Keys.ToHashSet().SetEquals(newRows.Keys))
        throw new InvalidDataException("Case IDs differ.");

    Console.WriteLine("Case | Baseline | Candidate | Result");
    var regressions = 0;
    foreach (var id in oldRows.Keys.OrderBy(id => id))
    {
        var before = oldRows[id].GetProperty("mechanicalPass").GetBoolean();
        var after = newRows[id].GetProperty("mechanicalPass").GetBoolean();
        var change = before == after ? "unchanged"
            : after ? "improved" : "REGRESSION";
        if (before && !after) regressions++;
        Console.WriteLine(
            $"{id} | {before} | {after} | {change}");
    }
    Console.WriteLine(
        "Inspect component metrics and raw answers; semantic review is pending.");
    return regressions > 0 ? 1 : 0;
}

if (args.Length == 1 && args[0] == "self-test")
{
    RunnerSelfTests.Run(json);
    return 0;
}

if (args.Length != 5 || args[0] != "collect")
{
    Console.WriteLine(
        "collect <cases.json> <standards-folder> <run-label> <split>");
    Console.WriteLine("compare <baseline.json> <candidate.json>");
    return 2;
}

var casesText = await File.ReadAllTextAsync(args[1]);
var tests = JsonSerializer.Deserialize<EvalCase[]>(casesText, json)
    ?? throw new InvalidDataException("Missing cases.");

var corpus = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var file in Directory.GetFiles(args[2], "*.md")
    .OrderBy(file => file, StringComparer.Ordinal))
    corpus.Add(
        Path.GetFileNameWithoutExtension(file),
        await File.ReadAllTextAsync(file));

if (corpus.Count != 10 || tests.Length != 10
    || tests.Any(test => test is null)
    || tests.Select(test => test.Id).Distinct().Count() != tests.Length)
    throw new InvalidDataException("Expected 10 documents and 10 unique cases.");

foreach (var test in tests)
{
    if (string.IsNullOrWhiteSpace(test.Id)
        || test.Split is not ("development" or "validation")
        || string.IsNullOrWhiteSpace(test.Proposal)
        || test.Proposal.Length > 12000
        || string.IsNullOrWhiteSpace(test.Question)
        || test.Question.Length > 200
        || test.ExpectedSourceIds is null
        || test.ExpectedSourceIds.Distinct().Count()
            != test.ExpectedSourceIds.Length
        || test.ExpectedSourceIds.Any(id => !corpus.ContainsKey(id))
        || test.ExpectedStatus is not ("supported" or "insufficient_evidence")
        || ((test.ExpectedStatus == "supported")
            != (test.ExpectedSourceIds.Length > 0)))
        throw new InvalidDataException("Invalid evaluation case.");
}

var selected = tests.Where(test => test.Split == args[4]).ToArray();
if (selected.Length == 0)
    throw new InvalidDataException("No cases in requested split.");

var label = args[3];
if (label.Length == 0 || label.Length > 60
    || label.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
    throw new InvalidDataException("Use a simple run label.");

Directory.CreateDirectory("results");
var output = Path.Combine("results", label + ".json");
if (File.Exists(output))
    throw new IOException("Use a new run label; results will not be overwritten.");

using var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5187/"),
    Timeout = TimeSpan.FromSeconds(100)
};
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stop.Cancel();
};

var startedAt = DateTimeOffset.UtcNow;
var results = new List<CaseResult>();
var interrupted = false;

foreach (var test in selected)
{
    if (stop.IsCancellationRequested) { interrupted = true; break; }

    var search = await CallAsync(
        "standards/search", new { question = test.Question });
    if (stop.IsCancellationRequested) { interrupted = true; break; }

    var review = await CallAsync(
        "proposals/review-with-standards",
        new { proposal = test.Proposal, question = test.Question });

    results.Add(Scoring.Evaluate(test, search, review, corpus));
    var row = results[^1];
    Console.WriteLine(
        $"{test.Id}: retrieval={row.RetrievalContractOk}, "
        + $"contract={row.ReviewContractOk}, "
        + $"status={row.StatusCorrect}, gate={row.MechanicalPass}");
}

var completed = !interrupted && !stop.IsCancellationRequested
    && results.Count == selected.Length;
var answerable = results.Where(
    row => row.Case.ExpectedSourceIds.Length > 0).ToArray();
var unknowns = results.Where(
    row => row.Case.ExpectedSourceIds.Length == 0).ToArray();

var modelManifest = File.Exists("runtime-models.json")
    ? await File.ReadAllTextAsync("runtime-models.json") : null;

var report = new
{
    runLabel = label,
    split = args[4],
    scorerVersion = "week4-v1",
    startedAt,
    finishedAt = DateTimeOffset.UtcNow,
    completed,
    requestedCases = selected.Length,
    processedCases = results.Count,
    datasetHash = Scoring.Hash(casesText),
    corpusHash = Scoring.Hash(string.Join("\n",
        corpus.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Key + ":" + Scoring.Hash(pair.Value)))),
    runtimeModelsSnapshot = modelManifest,
    humanReviewStatus = "pending",
    summary = new
    {
        mechanicalPasses = results.Count(row => row.MechanicalPass),
        retrievalContracts = results.Count(row => row.RetrievalContractOk),
        reviewContracts = results.Count(row => row.ReviewContractOk),
        answerableCasesProcessed = answerable.Length,
        retrievalScoresAvailable = answerable.Count(row => row.RecallAt3.HasValue),
        // Unavailable retrieval receives zero in this learning gate summary.
        meanRecallAt3 = answerable.Length == 0 ? (double?)null
            : answerable.Average(row => row.RecallAt3 ?? 0),
        expectedAbstentionsProcessed = unknowns.Length,
        correctAbstentions = unknowns.Count(row => row.StatusCorrect),
        falseAbstentions = answerable.Count(
            row => row.ObservedStatus == "insufficient_evidence")
    },
    results
};

await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, json));
Console.WriteLine("Saved: " + output);
Console.WriteLine("Human assessment of claim support remains pending.");
return completed && results.All(row => row.MechanicalPass) ? 0 : 1;

async Task<CallResult> CallAsync(string path, object payload)
{
    var watch = Stopwatch.StartNew();
    try
    {
        using var response = await client.PostAsJsonAsync(
            path, payload, stop.Token);
        var body = await response.Content.ReadAsStringAsync(stop.Token);
        return new CallResult(
            (int)response.StatusCode, watch.Elapsed.TotalMilliseconds,
            body, null);
    }
    catch (OperationCanceledException)
    {
        return new CallResult(
            null, watch.Elapsed.TotalMilliseconds, "",
            stop.IsCancellationRequested ? "cancelled" : "timeout");
    }
    catch (HttpRequestException)
    {
        return new CallResult(
            null, watch.Elapsed.TotalMilliseconds, "", "transport_failure");
    }
}
```

The report stores complete synthetic responses so you can inspect failures. Avoid using this raw-output collection policy unchanged with confidential proposals.

Transport failures are recorded as timeout, cancellation, or transport_failure. A non-200 HTTP body is preserved. Incomplete runs are saved and marked completed=false; the comparison mode rejects them.

## 9. Add RunnerSelfTests.cs

This mode needs neither the API nor Ollama. It checks the scorer against deliberately constructed responses. Its final check demonstrates a known blind spot: a genuine quote with an unsupported capacity claim can pass all mechanical gates.

```csharp
using System.Text.Json;

public static class RunnerSelfTests
{
    public static void Run(JsonSerializerOptions options)
    {
        var corpus = new Dictionary<string, string>
        {
            ["STD-001"] = "Durably record accepted work.",
            ["STD-002"] = "Authorize access to reports.",
            ["STD-003"] = "Choose service boundaries deliberately."
        };
        var hits = corpus.Select((pair, i) => new
        {
            id = pair.Key, title = pair.Key,
            text = pair.Value, revision = Scoring.Hash(pair.Value),
            score = 0.9 - i * 0.1
        }).ToArray();
        var search = new CallResult(200, 1,
            JsonSerializer.Serialize(new
            {
                retrievedSources = hits, embeddingModel = "fixture-model"
            }, options), null);
        var test = new EvalCase(
            "FIXTURE", "development", "Accepted work must survive an outage.",
            "What is required?", ["STD-001"], "supported");

        CallResult Review(
            string status, string id, string quote, string guidance) =>
            new(200, 1, JsonSerializer.Serialize(new
            {
                assessment = new
                {
                    status,
                    findings = new[] { new { guidance, sourceId = id, quote } }
                        .Where(_ => status == "supported").ToArray(),
                    unresolvedQuestions = status == "supported"
                        ? Array.Empty<string>() : new[] { "Missing policy?" }
                },
                retrievedSources = hits, embeddingModel = "fixture-model",
                promptVersion = "fixture-v1"
            }, options), null);

        var valid = Review(
            "supported", "STD-001", corpus["STD-001"], "Consider durable intake.");
        var checks = 0;
        void Assert(bool condition)
        {
            if (!condition) throw new Exception("Runner self-test failed.");
            checks++;
        }

        Assert(Scoring.Evaluate(test, search, valid, corpus).MechanicalPass);
        Assert(!Scoring.Evaluate(test, search,
            Review("supported", "UNKNOWN", corpus["STD-001"], "Guidance."),
            corpus).CitationIntegrityOk);
        Assert(!Scoring.Evaluate(test, search,
            Review("supported", "STD-001", "Invented quotation.", "Guidance."),
            corpus).CitationIntegrityOk);
        Assert(!Scoring.Evaluate(test, search,
            valid with { Body = "{}" }, corpus).ReviewContractOk);
        Assert(!Scoring.Evaluate(test, search,
            Review("insufficient_evidence", "", "", ""),
            corpus).StatusCorrect);

        var failedGeneration = Scoring.Evaluate(
            test, search, new CallResult(502, 1, "{}", null), corpus);
        Assert(failedGeneration.RecallAt3 == 1
            && !failedGeneration.ReviewContractOk);

        // A deliberate blind spot: a real quote cannot prove a capacity claim.
        Assert(Scoring.Evaluate(test, search,
            Review("supported", "STD-001", corpus["STD-001"],
                "This design guarantees 100K RPS."),
            corpus).MechanicalPass);
        Console.WriteLine(
            $"{checks} checks passed. Claim support still requires human review.");
    }
}
```

Build and run the offline checks:

```bash
dotnet build AIPractice.slnx
dotnet run --project test/AIPractice.Evaluation -- self-test
```

Expected: seven checks pass. This expected output is not a captured execution from the authoring environment.

If a self-test fails, fix the scorer before measuring a model. These tests cover the selected acceptance rules; they are not an exhaustive parser, transport, or regression-comparison test suite.

## 10. Capture a baseline

Start the API in its own terminal. Run every console command from the repository root (the folder containing AIPractice.slnx) with --project, so relative paths and the results/ folder resolve consistently.

Optionally capture local model metadata before each collection:

```bash
curl --fail --max-time 10 http://localhost:11434/api/tags \
  -o runtime-models.json
```

If this command fails, remove the incomplete snapshot file rather than treating it as valid model metadata. This capture describes installed local models; it does not prove which weights served an earlier request. Verify the configured chat and embedding model tags against it and avoid changing models while a run is in progress.

Warm up one grounded request outside the measured case set if you want approximate warm-run timings. Use the same warm-up policy for both experiments.

Collect the development baseline:

```bash
dotnet run --project test/AIPractice.Evaluation -- collect test/AIPractice.Evaluation/cases.json src/AIPractice.Api/standards baseline-dev development
```

Output: results/baseline-dev.json.

The runner always attempts to save a completed report even when some cases fail mechanically. Exit code 1 can therefore accompany a useful baseline report. Open it and classify the failures.

### Report interpretation

| Field | Meaning |
|---|---|
| completed | All requested cases were processed without interruption |
| datasetHash | SHA-256 of exact dataset text |
| corpusHash | SHA-256 of sorted source ID/content-hash pairs |
| scorerVersion | Version of these scoring rules; update it when rules change |
| runtimeModelsSnapshot | Optional installed-model metadata captured before the run |
| humanReviewStatus | Always pending until you grade the results separately |
| results | Per-case metrics, prompts, raw HTTP responses, and timing |
| summary | Counts and retrieval/abstention aggregates |

The app's promptVersion and embeddingModel are captured per valid response. Record the API code revision and effective configuration separately in your experiment notes. A prompt version is a declared label, not a cryptographic hash of its contents. Configured model tags can also be mutable.

The corpus hash covers the local snapshot. If returned text differs, the snapshot checks fail. Keep files unchanged until the API has indexed them and both runs finish.

### Exit codes

| Command | 0 | 1 | 2 |
|---|---|---|---|
| collect | Complete run; all mechanical gates pass | A gate failed or collection was incomplete | Invalid command syntax |
| compare | Complete, compatible runs; no pass-to-fail regression | At least one mechanical regression | Incomplete/incompatible reports or invalid command syntax |

Invalid files, invalid case metadata, and existing output paths throw and exit nonzero; their exact process code is not specified. Reports are never silently overwritten.

Neither exit code 0 nor MechanicalPass means the guidance is correct or approved for use.

## 11. Diagnose before improving

Use this table to choose the next experiment.

| Observation | Likely failure stage | Next action |
|---|---|---|
| Expected source absent | Retrieval | Inspect question wording and chunk content |
| Search correct, grounded endpoint 502 | Generation/envelope/citation acceptance | Inspect safe API diagnostics; preserve failed case |
| Source text/revision mismatch | Provenance or stale index | Restart/reindex and verify the local corpus path |
| Good retrieved source, wrong or irrelevant citation | Evidence use | Clarify the instruction tying guidance to quoted evidence |
| Correct quote, invented conclusion | Semantic reasoning | Tighten prompt; manually inspect claims |
| Answerable question abstains | Retrieval or evidence interpretation | Check whether its relevant source was supplied |
| Unknown policy question confidently answers | Abstention | Require explicit evidence for exact figures/organizational rules |
| Both runs fail identically | No demonstrated improvement | Do not count a rewritten answer as a measurable gain |

A 502 may hide rejected model content because Week 3 intentionally does not return invalid output. Do not disable application validation to improve evaluation scores. Add safe diagnostics later if needed.

## 12. Make one controlled change

Choose a failure observed in a development case. Change only one relevant instruction, retrieval setting, or source document, and record the hypothesis before rerunning.

Example hypothesis:

> For exact organizational figures, requiring an explicit numeric statement in the supplied source will reduce unsupported answers on E07 without causing false abstentions on E01–E06.

For this experiment, add a narrow instruction to GroundedStandardsClient's system prompt:

```text
If the question asks for an exact approved budget, retention period, or
organizational policy, a general recommendation is insufficient evidence.
Only report it when the supplied source explicitly states that fact.
Otherwise use insufficient_evidence and ask for the missing policy.
```

Only use that change if it addresses an observed development failure. If your baseline already handles unknowns correctly, do not claim this addition improved quality without supporting results. Choose a different measured failure or report that no gain was demonstrated.

Update the API response's prompt-version label to standards-review-v2 when changing this prompt. That label is emitted in Week 3's endpoint. Keep the version consistent with your notes.

Restart the API, refresh optional model metadata, and collect:

```bash
dotnet run --project test/AIPractice.Evaluation -- collect test/AIPractice.Evaluation/cases.json src/AIPractice.Api/standards candidate-dev development
dotnet run --project test/AIPractice.Evaluation -- compare results/baseline-dev.json results/candidate-dev.json
```

The comparison pairs case IDs and reports mechanical pass/fail transitions. It blocks different dataset hashes, splits, scorer versions, and incomplete runs. If the corpus hash changes, it explicitly identifies a corpus experiment; record which document changed and why.

The comparison is deliberately simple. Improvement in Recall@3 from 0 to 1/3 can leave MechanicalPass=false in both runs. Inspect component metrics and raw answers, not only the aggregate pass transition.

## 13. Grade the answers manually

Create an experiment note in your own project, with one row per case and variant. Judge baseline and candidate using the same rubric. Hide variant labels during review if practical.

| Case | Variant | Claims supported by cited text | Proposal facts preserved | Guidance useful | Unknowns handled | Material error and rationale |
|---|---|---|---|---|---|---|
| E01 | baseline | Pass / Fail | Pass / Fail | Pass / Fail | Pass / Fail | Fill in |
| E01 | candidate | Pass / Fail | Pass / Fail | Pass / Fail | Pass / Fail | Fill in |
| Repeat for selected cases | | | | | | |

Apply these rules:

- **Claim support:** each material finding follows from its quote, without inventing business requirements.
- **Proposal preservation:** planned mechanisms are not described as already implemented.
- **Usefulness:** guidance connects the source to a practical next decision or action.
- **Unknown handling:** missing policy/figures remain unknown; answerable questions are not unnecessarily refused.
- **Material error:** changes the architecture decision or creates false confidence. A real quote does not excuse such an error.

Record counts and denominators. With eight development cases, one case is 1/8 of the suite, not evidence of a comparable production improvement.

The runner does not automatically modify humanReviewStatus. Save your human assessment and decision alongside the JSON reports; keep the raw reports immutable.

## 14. Use the reserved validation cases

After choosing the candidate from development evidence:

1. Restore the baseline version and collect baseline-validation.
2. Restore the chosen candidate and collect candidate-validation.
3. Use the same corpus, scorer, model settings, and warm-up procedure.
4. Compare the two validation reports and manually inspect both cases.

```bash
dotnet run --project test/AIPractice.Evaluation -- collect test/AIPractice.Evaluation/cases.json src/AIPractice.Api/standards baseline-validation validation
```

Run the baseline command while the API actually serves the baseline revision. Then switch the API to the candidate and restart before:

```bash
dotnet run --project test/AIPractice.Evaluation -- collect test/AIPractice.Evaluation/cases.json src/AIPractice.Api/standards candidate-validation validation
dotnet run --project test/AIPractice.Evaluation -- compare results/baseline-validation.json results/candidate-validation.json
```

A run label does not switch API versions for you. Verify prompt versions and your recorded code/config revision before accepting the comparison.

If validation fails, document it. You may revise the candidate, but repeated tuning against E09–E10 turns them into development cases; add new reserved cases before making a fresh generalization claim.

## 15. Timing, cost, and reproducibility

ElapsedMs measures the full HTTP round trip, including serialization, network, retrieval, generation, and response reading as applicable. Search and review timings describe separate requests. Do not subtract them to infer precise model generation time.

Use sequential runs for this lesson. Record cold versus warm execution, local hardware, model metadata, code/config version, dataset/corpus hashes, and prompt version. Do not claim a stable p95 from this tiny suite.

One run uses two HTTP calls per case: one retrieval-only call and one RAG call. There is no paid API requirement. Local execution still consumes time, memory, and electricity. No token-cost metric is invented because the Week 3 response does not expose usage.

Low temperature and fixed inputs do not guarantee identical model output. If a conclusion depends on one surprising answer, repeat the same configuration with a new label and report the variation.

## 16. Completion checkpoint

- [ ] The retrieval-only endpoint works.
- [ ] The runner builds and all seven offline checks pass.
- [ ] Ten fixed cases load and both split labels work.
- [ ] Baseline results include failed cases rather than silently dropping them.
- [ ] Retrieval metrics remain observable if generation fails.
- [ ] Candidate collection changes one declared factor.
- [ ] Comparison rejects incompatible or incomplete reports.
- [ ] Source IDs, quotes, and local revisions are checked.
- [ ] Abstention and false abstention are inspected separately.
- [ ] Manual claim-support and usefulness notes exist.
- [ ] The reserved cases were not used to choose the candidate.
- [ ] The final decision states improvement, regression, or no demonstrated gain.

Suggested learning gate: no observed citation-integrity failures among returned supported answers, correct abstention on the tested unknowns, and no new material human-judged error. These are small-suite criteria, not a production guarantee.

## 17. Architecture reflection and next week

Write brief answers:

1. Which failure would you fix in retrieval, and which in the prompt?
2. Why must model failures remain in the evaluation denominator?
3. When is an automated judge worth adding, and what human calibration would it require?
4. What business consequence makes an error material?
5. Which cases and failure distributions are absent from this suite?

**Week 5:** add one read-only service-catalog tool with validated arguments, caller permissions, a bounded call count, and an audit trail. Reuse this evaluation harness to verify that tool use improves the task without introducing unauthorized access or uncontrolled loops.

## 18. Verification note

Prepared on **2026-11-08**. The authoring environment has no .NET SDK or Ollama, and live documentation lookup was unavailable. The implementation therefore has not been compiled or executed here.

Markdown, embedded JSON, dataset/source-ID consistency, code structure, and fixture expectations were reviewed statically. The local self-tests, build, API smoke checks, and real baseline/candidate runs remain required verification steps.

The synthetic corpus and metrics are learning choices. The complete source snapshots and raw outputs are suitable for these synthetic exercises, not a default logging policy for confidential architecture proposals.

