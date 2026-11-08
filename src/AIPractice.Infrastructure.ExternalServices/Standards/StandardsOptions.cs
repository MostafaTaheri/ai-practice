namespace AIPractice.Infrastructure.ExternalServices.Standards;

public class StandardsOptions
{
    public const string SectionName = "Standards";

    /// <summary>Defaults to a "standards" folder next to the application binaries.</summary>
    public string? Directory { get; set; }
}
