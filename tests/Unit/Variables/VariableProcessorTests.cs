using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Variables;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Variables;

public class VariableProcessorTests
{
    private readonly VariableProcessor _processor;

    public VariableProcessorTests()
    {
        _processor = new VariableProcessor();
    }

    private VariableContext CreateContext()
    {
        return new VariableContext
        {
            SystemVariables = new Dictionary<string, object>()
        };
    }

    [Fact]
    public async Task ProcessAsync_WithNoVariables_ReturnsSuccess()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "trigger: main"
        };
        var context = CreateContext();

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ProcessedDocument);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ProcessAsync_WithSystemVariable_ResolvesCorrectly()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "name: $(Build.BuildNumber)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>
            {
                { "Build.BuildNumber", "12345" }
            }
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("12345", result.ProcessedDocument.RawContent!);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ProcessAsync_WithPipelineVariable_ResolvesCorrectly()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "script: echo $(myVar)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "myVar", "hello" }
            }
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("hello", result.ProcessedDocument.RawContent!);
        Assert.Single(result.ResolvedVariables);
        Assert.Equal("hello", result.ResolvedVariables["myVar"].Value);
    }

    [Fact]
    public async Task ProcessAsync_WithTemplateVariableSyntax_CurrentlyNotReplaced()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "name: ${{ variables.buildConfig }}"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "buildConfig", "Release" }
            }
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert: current implementation does not replace template syntax
        Assert.Contains("${{ variables.buildConfig }}", result.ProcessedDocument.RawContent!);
    }

    [Fact]
    public async Task ProcessAsync_WithUndefinedVariable_AndFailOnUnresolved_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "script: echo $(undefinedVar)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            FailOnUnresolved = true
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "UNDEFINED_VARIABLE");
    }

    [Fact]
    public async Task ProcessAsync_WithUndefinedVariable_AndAllowUnresolved_ReturnsWarning()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "script: echo $(undefinedVar)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            FailOnUnresolved = false
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ResolveExpression_WithSystemVariable_ReturnsValue()
    {
        // Arrange
        var expression = "$(Build.BuildId)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>
            {
                { "Build.BuildId", "999" }
            }
        };

        // Act
        var result = _processor.ResolveExpression(expression, context);

        // Assert
        Assert.Equal("999", result);
    }

    [Fact]
    public void ResolveExpression_WithUndefinedVariable_ThrowsWhenFailOnUnresolved()
    {
        // Arrange
        var expression = "$(undefinedVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            FailOnUnresolved = true
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _processor.ResolveExpression(expression, context));
    }

    [Fact]
    public void ResolveExpression_WithUndefinedVariable_ReturnsOriginalWhenAllowed()
    {
        // Arrange
        var expression = "$(undefinedVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            FailOnUnresolved = false
        };

        // Act
        var result = _processor.ResolveExpression(expression, context);

        // Assert
        Assert.Equal("$(undefinedVar)", result);
    }

    [Fact]
    public void ExpandVariables_WithMultipleVariables_ReplacesAll()
    {
        // Arrange
        var text = "Build $(buildId) for $(project) in $(environment)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "buildId", "123" },
                { "project", "MyApp" },
                { "environment", "Production" }
            }
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("Build 123 for MyApp in Production", result);
    }

    [Fact]
    public void ExpandVariables_WithNoVariables_ReturnsOriginal()
    {
        // Arrange
        var text = "This has no variables";
        var context = CreateContext();

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal(text, result);
    }

    [Fact]
    public void ValidateVariables_WithAllDefined_ReturnsValid()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "script: echo $(var1) $(var2)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "var1", "value1" },
                { "var2", "value2" }
            }
        };

        // Act
        var result = _processor.ValidateVariables(document, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateVariables_WithUndefinedVariable_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "script: echo $(undefinedVar)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            FailOnUnresolved = true
        };

        // Act
        var result = _processor.ValidateVariables(document, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "UNDEFINED_VARIABLE");
    }

    [Fact]
    public async Task ProcessAsync_WithVariablePrecedence_SystemTakesPriority()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "value: $(shared)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object> { { "shared", "system" } },
            PipelineVariables = new Dictionary<string, object> { { "shared", "pipeline" } },
            EnvironmentVariables = new Dictionary<string, object> { { "shared", "env" } }
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.Contains("system", result.ProcessedDocument.RawContent!);
    }

    [Fact]
    public async Task ProcessAsync_WithComplexDocument_ProcessesSuccessfully()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "complex.yml",
            RawContent = @"
trigger: $(trigger)
variables:
  - name: buildConfig
    value: $(config)
jobs:
  - job: Build
    displayName: Build $(project)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "trigger", "main" },
                { "config", "Release" },
                { "project", "MyProject" }
            }
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("main", result.ProcessedDocument.RawContent!);
        Assert.Contains("Release", result.ProcessedDocument.RawContent!);
        Assert.Contains("MyProject", result.ProcessedDocument.RawContent!);
    }
}
