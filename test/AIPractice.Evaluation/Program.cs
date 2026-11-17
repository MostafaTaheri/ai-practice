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
    BaseAddress = new Uri(Environment.GetEnvironmentVariable("EVAL_API_URL") ?? "http://localhost:5187/"),
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
        "api/v1/standards/search", new { question = test.Question });
    if (stop.IsCancellationRequested) { interrupted = true; break; }

    var review = await CallAsync(
        "api/v1/proposals/review-with-standards",
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
