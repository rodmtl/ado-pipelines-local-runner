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
        Assert.NotNull(result.ProcessedDocument);
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
        Assert.NotNull(result.ProcessedDocument);
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
        Assert.NotNull(result.ProcessedDocument);
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
        Assert.NotNull(result.ProcessedDocument);
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
        Assert.NotNull(result.ProcessedDocument);
        Assert.Contains("main", result.ProcessedDocument.RawContent!);
        Assert.Contains("Release", result.ProcessedDocument.RawContent!);
        Assert.Contains("MyProject", result.ProcessedDocument.RawContent!);
    }

    [Fact]
    public void ResolveExpression_WithEmptyExpression_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = _processor.ResolveExpression("", context);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ResolveExpression_WithNullExpression_ReturnsNull()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = _processor.ResolveExpression(null!, context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExpandVariables_WithEmptyText_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = _processor.ExpandVariables("", context);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ExpandVariables_WithNullText_ReturnsNull()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = _processor.ExpandVariables(null!, context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExpandVariables_WithEnvironmentVariable_ResolvesCorrectly()
    {
        // Arrange
        var text = "Build $(envVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            EnvironmentVariables = new Dictionary<string, object>
            {
                { "envVar", "envValue" }
            }
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("Build envValue", result);
    }

    [Fact]
    public void ExpandVariables_WithParameterVariable_ResolvesCorrectly()
    {
        // Arrange
        var text = "Value: $(paramVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            Parameters = new Dictionary<string, object>
            {
                { "paramVar", "paramValue" }
            }
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("Value: paramValue", result);
    }

    [Fact]
    public void ExpandVariables_WithUndefinedVariable_ThrowsWhenFailOnUnresolved()
    {
        // Arrange
        var text = "Build $(undefinedVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            FailOnUnresolved = true
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _processor.ExpandVariables(text, context));
    }

    [Fact]
    public void ExpandVariables_WithUndefinedVariable_ReturnsOriginalWhenAllowed()
    {
        // Arrange
        var text = "Build $(undefinedVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            FailOnUnresolved = false
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("Build $(undefinedVar)", result);
    }

    [Fact]
    public void ValidateVariables_WithNullDocument_HandlesGracefully()
    {
        // Arrange
        PipelineDocument? document = new PipelineDocument { SourcePath = "test.yml", RawContent = null };
        var context = CreateContext();

        // Act
        var result = _processor.ValidateVariables(document, context);

        // Assert - null RawContent should be handled gracefully
        Assert.NotNull(result);
        // Result should be valid since there are no variable references in null content
    }

    [Fact]
    public void ValidateVariables_WithMixedVariables_ValidatesAllSources()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "script: $(sys) $(pipe) $(env) $(param)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object> { { "sys", "sysVal" } },
            PipelineVariables = new Dictionary<string, object> { { "pipe", "pipeVal" } },
            EnvironmentVariables = new Dictionary<string, object> { { "env", "envVal" } },
            Parameters = new Dictionary<string, object> { { "param", "paramVal" } }
        };

        // Act
        var result = _processor.ValidateVariables(document, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ProcessAsync_WithNullRawContent_ReturnsSuccess()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = null
        };
        var context = CreateContext();

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ProcessAsync_WithExceptionDuringExpansion_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "script: $(testVar)"
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
        Assert.Contains(result.Errors, e => e.Code == "VARIABLE_EXPANSION_ERROR");
    }

    [Fact]
    public void ResolveExpression_WithNonVariableExpression_ReturnsOriginal()
    {
        // Arrange
        var expression = "This is plain text";
        var context = CreateContext();

        // Act
        var result = _processor.ResolveExpression(expression, context);

        // Assert
        Assert.Equal(expression, result);
    }

    [Fact]
    public async Task ProcessAsync_ExtractsDocumentDefinedVariables_Successfully()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = @"
variables:
  myVar: myValue
script: echo $(myVar)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>()
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.ResolvedVariables);
        Assert.Contains("myVar", result.ResolvedVariables.Keys);
    }

    [Fact]
    public void ExpandVariables_WithVariableHavingNullValue_HandlesGracefully()
    {
        // Arrange
        var text = "Value: $(nullVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>
            {
                { "nullVar", null! }
            }
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("Value: $(nullVar)", result);
    }

    [Fact]
    public void ExpandVariables_WithSpecialCharactersInValue_PreservesCharacters()
    {
        // Arrange
        var text = "Path: $(pathVar)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>
            {
                { "pathVar", "C:\\Users\\test\\file.txt" }
            }
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("Path: C:\\Users\\test\\file.txt", result);
    }

    [Fact]
    public async Task ProcessAsync_WithDocumentDefinedVariables_MergesCorrectly()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = @"variables:
  myVar: myValue
  anotherVar: anotherValue
script: echo $(myVar) $(anotherVar)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>()
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ProcessedDocument);
        Assert.Contains("myValue", result.ProcessedDocument.RawContent!);
        Assert.Contains("anotherValue", result.ProcessedDocument.RawContent!);
        Assert.NotEmpty(result.ResolvedVariables);
    }

    [Fact]
    public async Task ProcessAsync_WithNestedVariableDefinitions_OnlyProcessesTopLevel()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = @"variables:
  topLevel: value1
  nested:
    subVar: value2
script: echo $(topLevel)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>()
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void ValidateVariables_WithEmptyDocument_RawContent_ReturnsValid()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = ""
        };
        var context = CreateContext();

        // Act
        var result = _processor.ValidateVariables(document, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ExpandVariables_WithMultipleOccurrencesOfSameVariable_ReplacesAll()
    {
        // Arrange
        var text = "Start $(var) middle $(var) end $(var)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>
            {
                { "var", "VALUE" }
            }
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("Start VALUE middle VALUE end VALUE", result);
    }

    [Fact]
    public void ValidateVariables_WithComplexVariableNames_ValidatesCorrectly()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "value: $(Build.BuildId) $(System.PullRequestId) $(Custom_Var_123)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>
            {
                { "Build.BuildId", "123" },
                { "System.PullRequestId", "456" },
                { "Custom_Var_123", "789" }
            }
        };

        // Act
        var result = _processor.ValidateVariables(document, context);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ProcessAsync_WithVariableGroupScope_PreservesScope()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "value: $(myVar)"
        };
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "myVar", "myValue" }
            },
            Scope = VariableScope.Pipeline
        };

        // Act
        var result = await _processor.ProcessAsync(document, context);

        // Assert
        Assert.True(result.Success);
        if (result.ResolvedVariables.TryGetValue("myVar", out var resolvedVar))
        {
            Assert.Equal(VariableScope.Pipeline, resolvedVar.Scope);
        }
    }

    [Fact]
    public void ExpandVariables_WithConsecutiveVariables_ReplacesCorrectly()
    {
        // Arrange
        var text = "$(var1)$(var2)$(var3)";
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>
            {
                { "var1", "a" },
                { "var2", "b" },
                { "var3", "c" }
            }
        };

        // Act
        var result = _processor.ExpandVariables(text, context);

        // Assert
        Assert.Equal("abc", result);
    }
}
