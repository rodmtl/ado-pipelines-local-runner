using AdoPipelinesLocalRunner.Contracts.Configuration;

namespace AdoPipelinesLocalRunner.Contracts;

public interface IErrorReporter
{
    ReportOutput GenerateReport(
        string sourcePath,
        IReadOnlyList<ValidationError> errors,
        IReadOnlyList<ValidationError> warnings,
        OutputFormat format);
}

public record ReportOutput
{
    public required string Content { get; init; }
    public required OutputFormat Format { get; init; }
    public string? FileNameSuggestion { get; init; }
}
