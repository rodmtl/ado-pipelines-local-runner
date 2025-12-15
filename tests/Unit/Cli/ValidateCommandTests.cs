using System.CommandLine;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Core.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using OutputFormatConfig = AdoPipelinesLocalRunner.Contracts.Configuration.OutputFormat;

namespace AdoPipelinesLocalRunner.Tests.Unit.Cli;

public class ValidateCommandTests
{
    [Fact]
    public async Task ValidateCommand_ShouldReturnZero_OnSuccess()
    {
        var orchestrator = new Mock<IValidationOrchestrator>();
        orchestrator.Setup(o => o.ValidateAsync(It.IsAny<ValidateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateResponse
            {
                Status = ValidationStatus.Success,
                Summary = new ValidationSummary
                {
                    FilesValidated = 1,
                    ErrorCount = 0,
                    WarningCount = 0,
                    InfoCount = 0,
                    TemplatesResolved = 0,
                    VariablesResolved = 0
                },
                Details = new ValidationDetails
                {
                    SyntaxValidation = null,
                    SchemaValidation = null,
                    TemplateResolution = null,
                    VariableProcessing = null,
                    AllErrors = new List<ValidationError>(),
                    AllWarnings = new List<ValidationError>()
                },
                Metrics = new ProcessingMetrics
                {
                    TotalTimeMs = 0,
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow
                },
                ValidatedDocument = new PipelineDocument()
            });

        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<OutputFormatConfig>()))
            .Returns(new ReportOutput { Content = "report", Format = OutputFormatConfig.Text });

        var services = new ServiceCollection();
        services.AddSingleton(orchestrator.Object);
        services.AddSingleton(reporter.Object);
        var provider = services.BuildServiceProvider();

        var root = Program.BuildRootCommand(provider);

        var exitCode = await root.InvokeAsync(new[] { "validate", "--pipeline", "pipe.yml" });

        exitCode.Should().Be(0);
        reporter.Verify(r => r.GenerateReport("pipe.yml", It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<OutputFormatConfig>()), Times.Once);
    }

    [Fact]
    public async Task ValidateCommand_ShouldReturnOne_WhenStrictWarnings()
    {
        var warning = new ValidationError { Code = "WARN", Message = "warn", Severity = Severity.Warning };
        var orchestrator = new Mock<IValidationOrchestrator>();
        orchestrator.Setup(o => o.ValidateAsync(It.IsAny<ValidateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateResponse
            {
                Status = ValidationStatus.SuccessWithWarnings,
                Summary = new ValidationSummary
                {
                    FilesValidated = 1,
                    ErrorCount = 0,
                    WarningCount = 1,
                    InfoCount = 0,
                    TemplatesResolved = 0,
                    VariablesResolved = 0
                },
                Details = new ValidationDetails
                {
                    SyntaxValidation = null,
                    SchemaValidation = null,
                    TemplateResolution = null,
                    VariableProcessing = null,
                    AllErrors = new List<ValidationError>(),
                    AllWarnings = new List<ValidationError> { warning }
                },
                Metrics = new ProcessingMetrics
                {
                    TotalTimeMs = 0,
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow
                },
                ValidatedDocument = new PipelineDocument()
            });

        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<OutputFormatConfig>()))
            .Returns(new ReportOutput { Content = "report", Format = OutputFormatConfig.Text });

        var services = new ServiceCollection();
        services.AddSingleton(orchestrator.Object);
        services.AddSingleton(reporter.Object);
        var provider = services.BuildServiceProvider();

        var root = Program.BuildRootCommand(provider);

        var exitCode = await root.InvokeAsync(new[] { "validate", "--pipeline", "pipe.yml", "--strict" });

        exitCode.Should().Be(1);
        reporter.Verify(r => r.GenerateReport("pipe.yml", It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<OutputFormatConfig>()), Times.Once);
    }
}
