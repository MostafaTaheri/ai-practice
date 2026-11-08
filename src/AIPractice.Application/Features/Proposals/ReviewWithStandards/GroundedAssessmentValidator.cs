using System.Text.Json;
using System.Text.Json.Serialization;
using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Proposals.ReviewWithStandards;

/// <summary>
/// A citation is accepted only if its source was retrieved and the quote appears verbatim in it
/// (compared with whitespace runs collapsed).
/// This does not establish that the guidance follows from the quote.
/// </summary>
public static class GroundedAssessmentValidator
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static StandardsAssessment ParseAndValidate(string json, IReadOnlyList<SourceHit> hits)
    {
        var result = JsonSerializer.Deserialize<StandardsAssessment>(json, Options)
            ?? throw new JsonException("Missing assessment.");

        if (result.Status is not ("supported" or "insufficient_evidence")
            || result.Findings is null || result.Findings.Length > 3
            || result.UnresolvedQuestions is null
            || result.UnresolvedQuestions.Length > 5
            || result.UnresolvedQuestions.Any(question =>
                string.IsNullOrWhiteSpace(question) || question.Length > 500))
            throw new InvalidDataException("Invalid assessment fields.");

        if ((result.Status == "supported" && result.Findings.Length == 0)
            || (result.Status == "insufficient_evidence"
                && (result.Findings.Length != 0
                    || result.UnresolvedQuestions.Length == 0)))
            throw new InvalidDataException("Inconsistent evidence status.");

        var sources = hits.ToDictionary(
            hit => hit.Source.Id, hit => hit.Source, StringComparer.Ordinal);

        foreach (var finding in result.Findings)
        {
            if (finding is null
                || string.IsNullOrWhiteSpace(finding.Guidance)
                || finding.Guidance.Length > 1000
                || string.IsNullOrWhiteSpace(finding.SourceId)
                || !sources.TryGetValue(finding.SourceId, out var source)
                || string.IsNullOrWhiteSpace(finding.Quote)
                || finding.Quote.Length > 600
                || !NormalizeWhitespace(source.Text).Contains(
                    NormalizeWhitespace(finding.Quote), StringComparison.Ordinal))
                throw new InvalidDataException("Invalid source citation.");
        }

        return result;
    }

    // Line breaks in a source are layout, not content; models routinely join lines with a space.
    private static string NormalizeWhitespace(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
