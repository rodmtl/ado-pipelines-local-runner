using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Contracts.Errors;
using AdoPipelinesLocalRunner.Core.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using OutputFormatConfig = AdoPipelinesLocalRunner.Contracts.Configuration.OutputFormat;

namespace AdoPipelinesLocalRunner.Tests.Unit.Orchestration;

public class ValidationOrchestratorTests
{
    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenNoIssues()
    {
        var doc = new PipelineDocument { SourcePath = "pipeline.yml" };
        var parser = new Mock<IYamlParser>();
        parser.Setup(p => p.ParseFileAsync<PipelineDocument>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResult<PipelineDocument>
            {
                Success = true,
                Data = doc,
                Errors = Array.Empty<ParseError>(),
                SourceMap = Mock.Of<ISourceMap>()
            });

        var syntax = new Mock<ISyntaxValidator>();
        syntax.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                IsValid = true,
                Errors = Array.Empty<ValidationError>(),
                Warnings = Array.Empty<ValidationError>()
            });

        var schema = new Mock<ISchemaManager>();
        schema.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaValidationResult
            {
                IsValid = true,
                SchemaVersion = "latest",
                Errors = Array.Empty<ValidationError>()
            });

        var templates = new Mock<ITemplateResolver>();
        templates.Setup(t => t.ExpandAsync(It.IsAny<PipelineDocument>(), It.IsAny<TemplateResolutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateExpansionResult
            {
                Success = true,
                ExpandedDocument = doc,
                ResolvedTemplates = Array.Empty<ResolvedTemplate>(),
                Errors = Array.Empty<ValidationError>()
            });

        var variables = new Mock<IVariableProcessor>();
        variables.Setup(v => v.ProcessAsync(It.IsAny<PipelineDocument>(), It.IsAny<VariableContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VariableProcessingResult
            {
                Success = true,
                ProcessedDocument = doc,
                ResolvedVariables = new Dictionary<string, ResolvedVariable>(),
                UnresolvedReferences = Array.Empty<string>(),
                Errors = Array.Empty<ValidationError>()
            });

        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), OutputFormatConfig.Text))
            .Returns(new ReportOutput { Content = "ok", Format = OutputFormatConfig.Text });

        var logger = new Mock<ILogger<ValidationOrchestrator>>();

        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = "pipeline.yml",
            BaseDirectory = ".",
            SchemaVersion = null,
            VariableFiles = Array.Empty<string>(),
            FailOnWarnings = false
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Success);
        response.Summary.ErrorCount.Should().Be(0);
        response.Summary.WarningCount.Should().Be(0);
        response.Details.AllErrors.Should().BeEmpty();
        response.Details.AllWarnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenWarningsAndStrict()
    {
        var doc = new PipelineDocument { SourcePath = "pipeline.yml" };
        var parser = new Mock<IYamlParser>();
        parser.Setup(p => p.ParseFileAsync<PipelineDocument>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResult<PipelineDocument>
            {
                Success = true,
                Data = doc,
                Errors = Array.Empty<ParseError>(),
                SourceMap = Mock.Of<ISourceMap>()
            });

        var warning = new ValidationError { Code = "WARN", Message = "warning", Severity = Severity.Warning };
        var syntax = new Mock<ISyntaxValidator>();
        syntax.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                IsValid = false,
                Errors = Array.Empty<ValidationError>(),
                Warnings = new[] { warning }
            });

        var schema = new Mock<ISchemaManager>();
        schema.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaValidationResult
            {
                IsValid = true,
                SchemaVersion = "latest",
                Errors = Array.Empty<ValidationError>()
            });

        var templates = new Mock<ITemplateResolver>();
        templates.Setup(t => t.ExpandAsync(It.IsAny<PipelineDocument>(), It.IsAny<TemplateResolutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateExpansionResult
            {
                Success = true,
                ExpandedDocument = doc,
                ResolvedTemplates = Array.Empty<ResolvedTemplate>(),
                Errors = Array.Empty<ValidationError>()
            });

        var variables = new Mock<IVariableProcessor>();
        variables.Setup(v => v.ProcessAsync(It.IsAny<PipelineDocument>(), It.IsAny<VariableContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VariableProcessingResult
            {
                Success = true,
                ProcessedDocument = doc,
                ResolvedVariables = new Dictionary<string, ResolvedVariable>(),
                UnresolvedReferences = Array.Empty<string>(),
                Errors = Array.Empty<ValidationError>()
            });

        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), OutputFormatConfig.Text))
            .Returns(new ReportOutput { Content = "warn", Format = OutputFormatConfig.Text });

        var logger = new Mock<ILogger<ValidationOrchestrator>>();

        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = "pipeline.yml",
            BaseDirectory = ".",
            SchemaVersion = null,
            VariableFiles = Array.Empty<string>(),
            FailOnWarnings = true
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Failed);
        response.Details.AllWarnings.Should().ContainSingle(w => w.Code == "WARN");
    }

    [Fact]
    public async Task ValidateAsync_ShouldCaptureExceptions_AsValidationError()
    {
        var parser = new Mock<IYamlParser>();
        parser.Setup(p => p.ParseFileAsync<PipelineDocument>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new YamlParseException("broken"));

        var syntax = new Mock<ISyntaxValidator>();
        var schema = new Mock<ISchemaManager>();
        var templates = new Mock<ITemplateResolver>();
        var variables = new Mock<IVariableProcessor>();
        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), OutputFormatConfig.Text))
            .Returns(new ReportOutput { Content = "err", Format = OutputFormatConfig.Text });
        var logger = new Mock<ILogger<ValidationOrchestrator>>();

        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = "pipeline.yml",
            BaseDirectory = null,
            SchemaVersion = null,
            VariableFiles = Array.Empty<string>(),
            FailOnWarnings = false
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Failed);
        response.Details.AllErrors.Should().ContainSingle(e => e.Code == "VALIDATION_ERROR");
    }
}
