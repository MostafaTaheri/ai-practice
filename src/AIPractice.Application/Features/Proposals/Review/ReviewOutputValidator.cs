using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIPractice.Application.Features.Proposals.Review;

public static class ReviewOutputValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static ReviewItem Parse(string json) =>
        JsonSerializer.Deserialize<ReviewItem>(json, JsonOptions)
        ?? throw new JsonException("Review cannot be null.");

    public static List<string> Validate(ReviewItem review, string proposal)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(review.Summary)
            || review.Summary.Length > 1500)
            errors.Add("summary must contain 1–1500 characters.");

        CheckList(review.Assumptions, "assumptions", errors);
        CheckList(review.Alternatives, "alternatives", errors);
        CheckList(review.Recommendations, "recommendations", errors);
        CheckList(review.MissingInformation, "missingInformation", errors);

        if (review.Risks is null || review.Risks.Length > 5)
        {
            errors.Add("risks must be a non-null array of at most 5 items.");
            return errors;
        }

        foreach (var risk in review.Risks)
        {
            if (risk is null)
            {
                errors.Add("risks cannot contain null.");
                continue;
            }

            if (risk.Category is not (
                "reliability" or "security" or "scalability"
                or "operations" or "cost" or "team"))
                errors.Add("risk category is not allowed.");

            if (risk.Severity is not ("low" or "medium" or "high"))
                errors.Add("risk severity is not allowed.");

            if (string.IsNullOrWhiteSpace(risk.Description)
                || risk.Description.Length > 1000)
                errors.Add("risk description must contain 1–1000 characters.");

            if (string.IsNullOrWhiteSpace(risk.EvidenceQuote)
                || risk.EvidenceQuote.Length > 1000
                || !proposal.Contains(
                    risk.EvidenceQuote, StringComparison.Ordinal))
                errors.Add("risk evidenceQuote must be a verbatim proposal excerpt.");
        }

        return errors;
    }

    private static void CheckList(
        string[]? items, string name, List<string> errors)
    {
        if (items is null || items.Length > 5)
        {
            errors.Add(name + " must be a non-null array of at most 5 items.");
            return;
        }

        if (items.Any(item =>
            string.IsNullOrWhiteSpace(item) || item.Length > 1000))
            errors.Add(name + " items must contain 1–1000 characters.");
    }
}
