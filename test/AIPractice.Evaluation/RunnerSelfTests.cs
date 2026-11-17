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
                data = new { retrievedSources = hits, embeddingModel = "fixture-model" },
                success = true, errors = Array.Empty<string>()
            }, options), null);
        var test = new EvalCase(
            "FIXTURE", "development", "Accepted work must survive an outage.",
            "What is required?", ["STD-001"], "supported");

        CallResult Review(
            string status, string id, string quote, string guidance) =>
            new(200, 1, JsonSerializer.Serialize(new
            {
                data = new
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
                },
                success = true, errors = Array.Empty<string>()
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
