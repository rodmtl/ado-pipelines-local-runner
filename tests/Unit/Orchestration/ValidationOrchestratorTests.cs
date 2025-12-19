using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Contracts.Errors;
using AdoPipelinesLocalRunner.Core.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
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

        var pipelinePath = CreateTempPipelineFile();

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = pipelinePath,
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

        var pipelinePath = CreateTempPipelineFile();

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = pipelinePath,
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

        var pipelinePath = CreateTempPipelineFile();

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = pipelinePath,
            BaseDirectory = null,
            SchemaVersion = null,
            VariableFiles = Array.Empty<string>(),
            FailOnWarnings = false
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Failed);
        response.Details.AllErrors.Should().ContainSingle(e => e.Code == "VALIDATION_ERROR");
    }

    [Fact]
    public async Task ValidateAsync_ShouldFailFast_WhenPipelineFileMissing()
    {
        var parser = new Mock<IYamlParser>(MockBehavior.Strict);
        var syntax = new Mock<ISyntaxValidator>(MockBehavior.Strict);
        var schema = new Mock<ISchemaManager>(MockBehavior.Strict);
        var templates = new Mock<ITemplateResolver>(MockBehavior.Strict);
        var variables = new Mock<IVariableProcessor>(MockBehavior.Strict);

        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), OutputFormatConfig.Text))
            .Returns(new ReportOutput { Content = "missing", Format = OutputFormatConfig.Text });

        var logger = new Mock<ILogger<ValidationOrchestrator>>();

        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.yml");

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = missingPath,
            BaseDirectory = ".",
            SchemaVersion = null,
            VariableFiles = Array.Empty<string>(),
            FailOnWarnings = false
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Failed);
        response.Details.AllErrors.Should().ContainSingle(e => e.Code == "PIPELINE_FILE_NOT_FOUND");

        parser.Verify(p => p.ParseFileAsync<PipelineDocument>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        syntax.VerifyNoOtherCalls();
        schema.VerifyNoOtherCalls();
        templates.VerifyNoOtherCalls();
        variables.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldHandleParsingFailure()
    {
        var doc = new PipelineDocument { SourcePath = "pipeline.yml" };
        var parser = new Mock<IYamlParser>();
        parser.Setup(p => p.ParseFileAsync<PipelineDocument>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResult<PipelineDocument>
            {
                Success = false,
                Data = null,
                Errors = new[] { new ParseError { Code = "YAML_PARSE_ERROR", Message = "Invalid YAML", Severity = Severity.Error } },
                SourceMap = Mock.Of<ISourceMap>()
            });

        var syntax = new Mock<ISyntaxValidator>();
        var schema = new Mock<ISchemaManager>();
        var templates = new Mock<ITemplateResolver>();
        var variables = new Mock<IVariableProcessor>();
        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), OutputFormatConfig.Text))
            .Returns(new ReportOutput { Content = "parse error", Format = OutputFormatConfig.Text });

        var logger = new Mock<ILogger<ValidationOrchestrator>>();
        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var pipelinePath = CreateTempPipelineFile();
        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = pipelinePath,
            BaseDirectory = ".",
            SchemaVersion = null,
            VariableFiles = Array.Empty<string>(),
            FailOnWarnings = false
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Failed);
    }

    [Fact]
    public async Task ValidateAsync_WithSchemaValidationError_ShouldFail()
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
            .ReturnsAsync(new ValidationResult { IsValid = true, Errors = Array.Empty<ValidationError>(), Warnings = Array.Empty<ValidationError>() });

        var schema = new Mock<ISchemaManager>();
        schema.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaValidationResult
            {
                IsValid = false,
                SchemaVersion = "latest",
                Errors = new[] { new ValidationError { Code = "SCHEMA_INVALID", Message = "Schema validation failed", Severity = Severity.Error } }
            });

        var templates = new Mock<ITemplateResolver>();
        var variables = new Mock<IVariableProcessor>();
        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), OutputFormatConfig.Text))
            .Returns(new ReportOutput { Content = "schema error", Format = OutputFormatConfig.Text });

        var logger = new Mock<ILogger<ValidationOrchestrator>>();
        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var pipelinePath = CreateTempPipelineFile();
        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = pipelinePath,
            BaseDirectory = ".",
            SchemaVersion = null,
            VariableFiles = Array.Empty<string>(),
            FailOnWarnings = false
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Failed);
        response.Summary.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateAsync_ShouldLoadPipelineVariables_FromYamlFile()
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
            .ReturnsAsync(new ValidationResult { IsValid = true, Errors = Array.Empty<ValidationError>(), Warnings = Array.Empty<ValidationError>() });

        var schema = new Mock<ISchemaManager>();
        schema.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaValidationResult { IsValid = true, SchemaVersion = "latest", Errors = Array.Empty<ValidationError>() });

        var templates = new Mock<ITemplateResolver>();

        VariableContext? capturedCtx = null;
        var variables = new Mock<IVariableProcessor>();
        variables.Setup(v => v.ProcessAsync(It.IsAny<PipelineDocument>(), It.IsAny<VariableContext>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineDocument, VariableContext, CancellationToken>((_, ctx, _) => capturedCtx = ctx)
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

        var pipelinePath = CreateTempPipelineFile();
        var variableFile = Path.Combine(Path.GetTempPath(), $"vars-{Guid.NewGuid():N}.yml");
        File.WriteAllText(variableFile, "variables:\n  - name: VAR1\n    value: one\n  - name: VAR2\n    value: two\n");

        try
        {
            var response = await orchestrator.ValidateAsync(new ValidateRequest
            {
                Path = pipelinePath,
                BaseDirectory = Path.GetDirectoryName(variableFile),
                SchemaVersion = null,
                VariableFiles = new[] { variableFile },
                ValidateTemplates = false,
                FailOnWarnings = false
            }, CancellationToken.None);

            response.Status.Should().Be(ValidationStatus.Success);
            capturedCtx.Should().NotBeNull();
            capturedCtx!.PipelineVariables.Should().ContainKey("VAR1").WhoseValue.Should().Be("one");
            capturedCtx.PipelineVariables.Should().ContainKey("VAR2").WhoseValue.Should().Be("two");
            response.Details.AllErrors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(variableFile)) File.Delete(variableFile);
            if (File.Exists(pipelinePath)) File.Delete(pipelinePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldReportError_ForInvalidJsonVariableFile()
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
            .ReturnsAsync(new ValidationResult { IsValid = true, Errors = Array.Empty<ValidationError>(), Warnings = Array.Empty<ValidationError>() });

        var schema = new Mock<ISchemaManager>();
        schema.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaValidationResult { IsValid = true, SchemaVersion = "latest", Errors = Array.Empty<ValidationError>() });

        var templates = new Mock<ITemplateResolver>();

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
            .Returns(new ReportOutput { Content = "invalid", Format = OutputFormatConfig.Text });

        var logger = new Mock<ILogger<ValidationOrchestrator>>();

        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var pipelinePath = CreateTempPipelineFile();
        var variableFile = Path.Combine(Path.GetTempPath(), $"vars-{Guid.NewGuid():N}.json");
        File.WriteAllText(variableFile, "{ not: valid json");

        try
        {
            var response = await orchestrator.ValidateAsync(new ValidateRequest
            {
                Path = pipelinePath,
                BaseDirectory = Path.GetDirectoryName(variableFile),
                SchemaVersion = null,
                VariableFiles = new[] { variableFile },
                ValidateTemplates = false,
                FailOnWarnings = false
            }, CancellationToken.None);

            response.Status.Should().Be(ValidationStatus.Failed);
            response.Details.AllErrors.Should().ContainSingle(e => e.Code == "VARIABLE_FILE_INVALID");
        }
        finally
        {
            if (File.Exists(variableFile)) File.Delete(variableFile);
            if (File.Exists(pipelinePath)) File.Delete(pipelinePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldReportError_ForMissingVariableFile()
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
            .ReturnsAsync(new ValidationResult { IsValid = true, Errors = Array.Empty<ValidationError>(), Warnings = Array.Empty<ValidationError>() });

        var schema = new Mock<ISchemaManager>();
        schema.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaValidationResult { IsValid = true, SchemaVersion = "latest", Errors = Array.Empty<ValidationError>() });

        var templates = new Mock<ITemplateResolver>();

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
            .Returns(new ReportOutput { Content = "missing", Format = OutputFormatConfig.Text });

        var logger = new Mock<ILogger<ValidationOrchestrator>>();

        var orchestrator = new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, variables.Object, reporter.Object, logger.Object);

        var pipelinePath = CreateTempPipelineFile();
        var missingVariableFile = Path.Combine(Path.GetTempPath(), $"vars-missing-{Guid.NewGuid():N}.yml");

        var response = await orchestrator.ValidateAsync(new ValidateRequest
        {
            Path = pipelinePath,
            BaseDirectory = Path.GetDirectoryName(missingVariableFile),
            SchemaVersion = null,
            VariableFiles = new[] { missingVariableFile },
            ValidateTemplates = false,
            FailOnWarnings = false
        }, CancellationToken.None);

        response.Status.Should().Be(ValidationStatus.Failed);
        response.Details.AllErrors.Should().ContainSingle(e => e.Code == "VARIABLE_FILE_NOT_FOUND");

        if (File.Exists(pipelinePath)) File.Delete(pipelinePath);
    }

    private static string CreateTempPipelineFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pipeline-{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, "trigger: none\n");
        return path;
    }
}
