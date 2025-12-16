using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Core.Orchestration;
using AdoPipelinesLocalRunner.Core.Parsing;
using AdoPipelinesLocalRunner.Core.Reporting;
using AdoPipelinesLocalRunner.Core.Schema;
using AdoPipelinesLocalRunner.Core.Templates;
using AdoPipelinesLocalRunner.Core.Validators;
using AdoPipelinesLocalRunner.Core.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Integration;

/// <summary>
/// Integration tests that validate all demo pipeline files work correctly
/// with the real implementations (not mocked).
/// </summary>
public class DemoPipelinesTests
{
    private readonly IValidationOrchestrator _orchestrator;
    private readonly string _demosBasePath;

    public DemoPipelinesTests()
    {
        // Set up the demos base path
        var solutionRoot = GetSolutionRoot();
        _demosBasePath = Path.Combine(solutionRoot, "demos");

        // Create real implementations with parameterless constructors
        var parser = new YamlParser();
        var syntaxValidator = new SyntaxValidator();
        var schemaManager = new SchemaManager();
        var templateResolver = new TemplateResolver();
        var variableProcessor = new VariableProcessor();
        var errorReporter = new ErrorReporter();

        var orchestratorLogger = new Mock<ILogger<ValidationOrchestrator>>().Object;
        _orchestrator = new ValidationOrchestrator(
            parser,
            syntaxValidator,
            schemaManager,
            templateResolver,
            variableProcessor,
            errorReporter,
            orchestratorLogger
        );
    }

    private string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "AdoPipelinesLocalRunner.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        if (currentDir == null)
        {
            throw new InvalidOperationException("Could not find solution root directory");
        }

        return currentDir;
    }

    private async Task<ValidateResponse> ValidateDemo(
        string demoFileName,
        Dictionary<string, string>? inlineVariables = null,
        string? variableFilePath = null)
    {
        var pipelinePath = Path.Combine(_demosBasePath, demoFileName);
        
        if (!File.Exists(pipelinePath))
        {
            throw new FileNotFoundException($"Demo file not found: {pipelinePath}");
        }

        var request = new ValidateRequest
        {
            Path = pipelinePath,
            BaseDirectory = GetSolutionRoot(),
            ValidateSchema = true,
            ValidateTemplates = true,
            ValidateVariables = true,
            InlineVariables = inlineVariables ?? new Dictionary<string, string>(),
            VariableFiles = variableFilePath != null ? new[] { variableFilePath } : null,
            FailOnWarnings = false
        };

        return await _orchestrator.ValidateAsync(request, CancellationToken.None);
    }

    [Fact]
    public async Task SyntaxValidationDemo_ShouldPass()
    {
        // Arrange & Act
        var response = await ValidateDemo("01-syntax-validation.yml");

        // Assert
                if (response.Status != ValidationStatus.Success)
                {
                    var errors = string.Join(Environment.NewLine, response.Details.AllErrors.Select(e => $"{e.Code}: {e.Message}"));
                    throw new Exception($"Validation failed with errors:{Environment.NewLine}{errors}");
                }

        response.Should().NotBeNull();
        response.Status.Should().Be(ValidationStatus.Success);
        response.Summary.ErrorCount.Should().Be(0);
        response.Summary.WarningCount.Should().Be(0);
    }

    [Fact]
    public async Task SchemaValidationDemo_ShouldPass()
    {
        // Arrange & Act
        var response = await ValidateDemo("02-schema-validation.yml");

        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be(ValidationStatus.Success);
        response.Summary.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task TemplateReferenceDemo_ShouldPass()
    {
        // Arrange & Act
        var response = await ValidateDemo("03-template-reference.yml");
        if (response.Status != ValidationStatus.Success)
        {
            var errors = string.Join(Environment.NewLine, response.Details.AllErrors.Select(e => $"{e.Code}: {e.Message}"));
            throw new Exception($"Validation failed with errors:{Environment.NewLine}{errors}");
        }


        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be(ValidationStatus.Success);
        response.Summary.ErrorCount.Should().Be(0);
        response.Details.Should().NotBeNull();
        response.Details.TemplateResolution.Should().NotBeNull();
    }

    [Fact]
    public async Task VariablesDemo_WithInline_ShouldSubstitute()
    {
        // Arrange
        var inlineVars = new Dictionary<string, string>
        {
            { "BuildConfiguration", "Release" },
            { "BuildVersion", "1.0.0" },
            { "Environment", "Production" }
        };

        // Act
        var response = await ValidateDemo("04-variables.yml", inlineVariables: inlineVars);
        if (response.Status != ValidationStatus.Success)
        {
            var errors = string.Join(Environment.NewLine, response.Details.AllErrors.Select(e => $"{e.Code}: {e.Message}"));
            throw new Exception($"Validation failed with errors:{Environment.NewLine}{errors}");
        }


        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be(ValidationStatus.Success);
        response.Summary.ErrorCount.Should().Be(0);
        response.Details.Should().NotBeNull();
        response.Details.VariableProcessing.Should().NotBeNull();
        response.Details.VariableProcessing!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task VariablesDemo_WithFile_ShouldSubstitute()
    {
        // Arrange
        var variableFile = Path.Combine(_demosBasePath, "04-variables-input.yml");

        // Act
        var response = await ValidateDemo("04-variables.yml", variableFilePath: variableFile);

        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be(ValidationStatus.Success);
        response.Summary.ErrorCount.Should().Be(0);
        response.Details.Should().NotBeNull();
        response.Details.VariableProcessing.Should().NotBeNull();
    }

    [Fact]
    public async Task CliDemo_ShouldPass()
    {
        // Arrange & Act
        var response = await ValidateDemo("05-cli-demo.yml");

        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be(ValidationStatus.Success);
        response.Summary.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void AllDemoFiles_ShouldExist()
    {
        // Arrange
        var expectedDemos = new[]
        {
            "01-syntax-validation.yml",
            "02-schema-validation.yml",
            "03-template-reference.yml",
            "03-template.yml",
            "04-variables.yml",
            "04-variables-input.yml",
            "05-cli-demo.yml"
        };

        // Act & Assert
        foreach (var demo in expectedDemos)
        {
            var path = Path.Combine(_demosBasePath, demo);
            File.Exists(path).Should().BeTrue($"Demo file {demo} should exist at {path}");
        }
    }
}
