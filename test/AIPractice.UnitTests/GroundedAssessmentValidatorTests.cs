using System.Text.Json;
using AIPractice.Application.Features.Proposals.ReviewWithStandards;
using AIPractice.BuildingBlock;

namespace AIPractice.UnitTests;

public class GroundedAssessmentValidatorTests
{
    private static readonly SourceHit[] Hits =
    [
        new(new StandardSource("TEST-001", "Test policy", "Record accepted work durably.", "fixture-v1"), 1.0)
    ];

    private const string ValidJson = """
        {
          "status": "supported",
          "findings": [{
            "guidance": "Consider durable intake for accepted claims.",
            "sourceId": "TEST-001",
            "quote": "Record accepted work durably."
          }],
          "unresolvedQuestions": []
        }
        """;

    [Fact]
    public void ValidCitation_IsAccepted()
    {
        var result = GroundedAssessmentValidator.ParseAndValidate(ValidJson, Hits);

        Assert.Equal("supported", result.Status);
        Assert.Single(result.Findings);
    }

    [Fact]
    public void UnknownSourceId_IsRejected() =>
        Assert.Throws<InvalidDataException>(() =>
            GroundedAssessmentValidator.ParseAndValidate(ValidJson.Replace("TEST-001", "UNKNOWN-001"), Hits));

    [Fact]
    public void InventedQuote_IsRejected() =>
        Assert.Throws<InvalidDataException>(() =>
            GroundedAssessmentValidator.ParseAndValidate(
                ValidJson.Replace("Record accepted work durably.\"\n", "A quotation that does not exist.\"\n"), Hits));

    [Fact]
    public void InsufficientEvidenceWithFindings_IsRejected() =>
        Assert.Throws<InvalidDataException>(() =>
            GroundedAssessmentValidator.ParseAndValidate(
                ValidJson.Replace("\"supported\"", "\"insufficient_evidence\""), Hits));

    [Fact]
    public void Abstention_WithUnresolvedQuestion_IsAccepted()
    {
        const string json = """
            {"status":"insufficient_evidence","findings":[],"unresolvedQuestions":["Which policy applies?"]}
            """;

        var result = GroundedAssessmentValidator.ParseAndValidate(json, Hits);

        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Abstention_WithoutUnresolvedQuestion_IsRejected() =>
        Assert.Throws<InvalidDataException>(() =>
            GroundedAssessmentValidator.ParseAndValidate(
                """{"status":"insufficient_evidence","findings":[],"unresolvedQuestions":[]}""", Hits));

    [Fact]
    public void UnexpectedProperty_IsRejected() =>
        Assert.Throws<JsonException>(() =>
            GroundedAssessmentValidator.ParseAndValidate(
                ValidJson.Replace("\"status\"", "\"extra\": 1, \"status\""), Hits));

    [Fact]
    public void Quote_SpanningLineBreak_IsAcceptedWhenWhitespaceDiffers()
    {
        SourceHit[] hits =
        [
            new(new StandardSource("T-2", "t", "First line.\nSecond line.", "v"), 1.0)
        ];
        const string json = """
            {"status":"supported","findings":[{"guidance":"g","sourceId":"T-2","quote":"First line. Second line."}],"unresolvedQuestions":[]}
            """;

        Assert.Single(GroundedAssessmentValidator.ParseAndValidate(json, hits).Findings);
    }
}
