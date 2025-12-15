using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Core.Reporting;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Reporting;

public class ErrorReporterTests
{
    private static IErrorReporter Create() => new ErrorReporter();

    private static IReadOnlyList<ValidationError> SampleErrors() => new List<ValidationError>
    {
        new ValidationError
        {
            Code = "TEST_ERROR",
            Message = "Something went wrong",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = "pipeline.yml", Line = 10, Column = 1 }
        }
    };

    private static IReadOnlyList<ValidationError> SampleWarnings() => new List<ValidationError>
    {
        new ValidationError
        {
            Code = "TEST_WARNING",
            Message = "This could be improved",
            Severity = Severity.Warning,
            Location = new SourceLocation { FilePath = "pipeline.yml", Line = 5, Column = 1 }
        }
    };

    [Theory]
    [InlineData(OutputFormat.Text)]
    [InlineData(OutputFormat.Markdown)]
    [InlineData(OutputFormat.Json)]
    [InlineData(OutputFormat.Sarif)]
    public void GenerateReport_ShouldProduceContent(OutputFormat format)
    {
        var reporter = Create();
        var report = reporter.GenerateReport("pipeline.yml", SampleErrors(), SampleWarnings(), format);

        report.Should().NotBeNull();
        report.Content.Should().NotBeNullOrEmpty();
        report.Format.Should().Be(format);
        report.FileNameSuggestion.Should().NotBeNullOrEmpty();
    }
}
