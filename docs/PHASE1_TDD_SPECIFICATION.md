# Phase 1 MVP - TDD Specification

**Version:** 1.0  
**Date:** December 12, 2025  
**Target Framework:** .NET 8.0  
**Testing Framework:** xUnit + Moq + FluentAssertions

---

## 1. Test Strategy Overview

### 1.1 Testing Pyramid for MVP

```
        ┌───────────────┐
        │  E2E Tests    │  ← 5% (CLI validation flow)
        │   (2-3)       │
        ├───────────────┤
        │ Integration   │  ← 15% (Component interactions)
        │  Tests (8-10) │
        ├───────────────┤
        │  Unit Tests   │  ← 80% (Component logic)
        │   (40-50)     │
        └───────────────┘
```

### 1.2 Coverage Targets

| Component           | Line Coverage | Branch Coverage | Priority |
|---------------------|--------------|-----------------|----------|
| YamlParser          | 85%          | 80%             | HIGH     |
| SyntaxValidator     | 90%          | 85%             | CRITICAL |
| SchemaManager       | 85%          | 80%             | HIGH     |
| TemplateResolver    | 85%          | 80%             | HIGH     |
| VariableProcessor   | 85%          | 80%             | HIGH     |
| CLI Handler         | 70%          | 65%             | MEDIUM   |
| **Overall MVP**     | **85%**      | **80%**         | -        |

### 1.3 Mock Strategy

**General Principles:**
- Mock external dependencies (file system, network, time)
- Use real implementations for value objects and DTOs
- Mock at architectural boundaries (IYamlParser, ISchemaManager, etc.)
- Prefer test doubles over mocking frameworks for simple scenarios

**Mock Hierarchy:**
```
Real Objects:
  ├─ PipelineDocument
  ├─ ValidationError
  ├─ ParseError
  └─ SourceLocation

Mocked Interfaces:
  ├─ IYamlParser
  ├─ ISyntaxValidator
  ├─ ISchemaManager
  ├─ ITemplateResolver
  ├─ IVariableProcessor
  ├─ IFileSystem
  └─ ILogger
```

---

## 2. Component Test Strategies

### 2.1 YamlParser Component

**Purpose:** Parse YAML files into strongly-typed PipelineDocument objects

**Key Test Scenarios:**

1. **Valid YAML Parsing**
   - Simple pipeline (steps only)
   - Multi-stage pipeline
   - Pipeline with variables and parameters
   - Pipeline with complex resources

2. **Error Handling**
   - Malformed YAML syntax
   - Invalid UTF-8 encoding
   - Empty files
   - Files exceeding size limits

3. **Source Mapping**
   - Line number preservation
   - Column tracking for errors
   - Multi-file source tracking (templates)

**Test Categories:**
- Happy path: 40%
- Error scenarios: 40%
- Edge cases: 20%

**Dependencies to Mock:**
- `IFileSystem` (file I/O)
- `ILogger` (diagnostics)

---

### 2.2 SyntaxValidator Component

**Purpose:** Validate ADO pipeline syntax rules without schema validation

**Key Test Scenarios:**

1. **Required Fields Validation**
   - Pipelines must have at least one of: steps, jobs, or stages
   - Jobs must have steps or template reference
   - Stages must have jobs array

2. **Structural Validation**
   - No circular template references
   - Valid variable name syntax
   - Parameter names follow conventions
   - Pool references are well-formed

3. **ADO-Specific Rules**
   - Task names follow `PublisherName.TaskName@Version` format
   - Step display names are unique within job
   - Condition expressions use valid syntax
   - DependsOn references exist

**Test Categories:**
- Required fields: 30%
- Structural rules: 40%
- ADO conventions: 30%

**Dependencies to Mock:**
- `ILogger` (diagnostics)

---

### 2.3 SchemaManager Component

**Purpose:** Validate pipeline documents against ADO YAML schema

**Key Test Scenarios:**

1. **Schema Loading**
   - Load embedded schema resources
   - Cache schema definitions
   - Handle schema versioning

2. **Schema Validation**
   - Valid pipeline against schema
   - Missing required properties
   - Invalid property types
   - Additional properties not allowed
   - Enum value validation

3. **Error Reporting**
   - Precise error locations (line/column)
   - Clear error messages
   - Suggested fixes when applicable

**Test Categories:**
- Schema operations: 20%
- Validation logic: 60%
- Error reporting: 20%

**Dependencies to Mock:**
- `IFileSystem` (schema file access)
- `ILogger` (diagnostics)

---

### 2.4 TemplateResolver Component

**Purpose:** Resolve and expand template references in pipeline files

**Key Test Scenarios:**

1. **Template Resolution**
   - Local file templates (relative paths)
   - Repository templates (resources syntax)
   - Nested template references
   - Template parameter passing

2. **Parameter Substitution**
   - String parameters
   - Object parameters
   - Array parameters
   - Default parameter values

3. **Error Handling**
   - Template file not found
   - Circular template references
   - Missing required parameters
   - Type mismatch in parameters

**Test Categories:**
- Resolution logic: 40%
- Parameter handling: 35%
- Error scenarios: 25%

**Dependencies to Mock:**
- `IFileSystem` (template file access)
- `IYamlParser` (parse templates)
- `ILogger` (diagnostics)

---

### 2.5 VariableProcessor Component

**Purpose:** Process and expand variables in pipeline documents

**Key Test Scenarios:**

1. **Variable Expansion**
   - Simple variable references `$(variableName)`
   - Nested variable references `$(var1.$(var2))`
   - System variables (predefined)
   - Environment-specific variables

2. **Expression Evaluation**
   - Compile-time expressions `${{ }}`
   - Runtime expressions `$[ ]`
   - Conditional expressions
   - Function calls (length, format, etc.)

3. **Scoping**
   - Pipeline-level variables
   - Stage-level variable override
   - Job-level variable override
   - Variable group references

**Test Categories:**
- Variable expansion: 45%
- Expression evaluation: 35%
- Scope resolution: 20%

**Dependencies to Mock:**
- `ILogger` (diagnostics)

---

## 3. Concrete xUnit Test Examples

### 3.1 YamlParser - Valid Pipeline Parsing

```csharp
using Xunit;
using FluentAssertions;
using AdoPipelinesLocalRunner.Core;
using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Tests.Unit.Core;

public class YamlParserTests
{
    [Fact]
    public void Parse_ValidSimplePipeline_ReturnsParsedDocument()
    {
        // Arrange
        var yamlContent = @"
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - script: echo Hello World
    displayName: 'Run a one-line script'
  
  - script: |
      echo Add other tasks
      echo More commands
    displayName: 'Run multi-line script'
";
        var parser = new YamlParser();

        // Act
        var result = parser.Parse<PipelineDocument>(yamlContent);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Steps.Should().HaveCount(2);
        result.Errors.Should().BeEmpty();
        
        // Verify source mapping
        result.SourceMap.Should().NotBeNull();
        result.SourceMap.GetLineNumber("steps[0]").Should().Be(8);
    }

    [Fact]
    public void Parse_MalformedYaml_ReturnsErrorWithLocation()
    {
        // Arrange
        var yamlContent = @"
trigger:
  - main
pool:
  vmImage: 'ubuntu-latest'
steps:
  - script: echo test
    displayName: unmatched quote'
";
        var parser = new YamlParser();

        // Act
        var result = parser.Parse<PipelineDocument>(yamlContent);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Errors.Should().ContainSingle();
        
        var error = result.Errors[0];
        error.Code.Should().Be("YAML001");
        error.Severity.Should().Be(Severity.Error);
        error.Location.Should().NotBeNull();
        error.Location!.Line.Should().Be(8);
        error.Message.Should().Contain("unmatched");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n\n")]
    public void Parse_EmptyOrWhitespaceContent_ReturnsError(string content)
    {
        // Arrange
        var parser = new YamlParser();

        // Act
        var result = parser.Parse<PipelineDocument>(content);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("YAML002");
    }
}
```

---

### 3.2 SyntaxValidator - Required Fields Validation

```csharp
using Xunit;
using FluentAssertions;
using Moq;
using AdoPipelinesLocalRunner.Core;
using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Tests.Unit.Core;

public class SyntaxValidatorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly SyntaxValidator _validator;

    public SyntaxValidatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _validator = new SyntaxValidator(_loggerMock.Object);
    }

    [Fact]
    public void Validate_PipelineWithSteps_IsValid()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["script"] = "echo test",
                    ["displayName"] = "Test Step"
                }
            }
        };

        // Act
        var result = _validator.Validate(document);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Validate_PipelineWithoutStepsJobsOrStages_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { Branches = new[] { "main" } },
            Pool = new { VmImage = "ubuntu-latest" }
            // No steps, jobs, or stages
        };

        // Act
        var result = _validator.Validate(document);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        
        var error = result.Errors[0];
        error.Code.Should().Be("SYN001");
        error.Message.Should().Contain("must contain at least one of");
        error.Severity.Should().Be(Severity.Error);
        error.Suggestion.Should().Contain("Add steps, jobs, or stages");
    }

    [Fact]
    public void Validate_JobWithoutStepsOrTemplate_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Jobs = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["job"] = "BuildJob",
                    ["displayName"] = "Build",
                    ["pool"] = new { VmImage = "ubuntu-latest" }
                    // Missing steps or template
                }
            }
        };

        // Act
        var result = _validator.Validate(document);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("SYN002");
    }

    [Fact]
    public void Validate_InvalidVariableName_ReturnsWarning()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Variables = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["my-invalid-var!"] = "value" // Invalid characters
                }
            },
            Steps = new List<object>
            {
                new Dictionary<string, object> { ["script"] = "echo test" }
            }
        };

        // Act
        var result = _validator.Validate(document);

        // Assert
        result.IsValid.Should().BeTrue(); // Still valid, but with warning
        result.Warnings.Should().ContainSingle();
        
        var warning = result.Warnings[0];
        warning.Code.Should().Be("SYN010");
        warning.Severity.Should().Be(Severity.Warning);
        warning.Message.Should().Contain("variable name");
    }
}
```

---

### 3.3 SchemaManager - Schema Validation

```csharp
using Xunit;
using FluentAssertions;
using Moq;
using AdoPipelinesLocalRunner.Core;
using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Tests.Unit.Core;

public class SchemaManagerTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly SchemaManager _schemaManager;

    public SchemaManagerTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _loggerMock = new Mock<ILogger>();
        _schemaManager = new SchemaManager(_fileSystemMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ValidateAgainstSchema_ValidDocument_ReturnsSuccess()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new List<string> { "main", "develop" },
            Pool = new Dictionary<string, object>
            {
                ["vmImage"] = "ubuntu-latest"
            },
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["task"] = "DotNetCoreCLI@2",
                    ["inputs"] = new Dictionary<string, object>
                    {
                        ["command"] = "build"
                    }
                }
            }
        };

        // Act
        var result = _schemaManager.ValidateAgainstSchema(document);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAgainstSchema_InvalidPropertyType_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = "main", // Should be array or object, not string
            Steps = new List<object>
            {
                new Dictionary<string, object> { ["script"] = "echo test" }
            }
        };

        // Act
        var result = _schemaManager.ValidateAgainstSchema(document);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        
        var error = result.Errors[0];
        error.Code.Should().Be("SCH001");
        error.Message.Should().Contain("Invalid type");
        error.Message.Should().Contain("trigger");
        error.Location.Should().NotBeNull();
        error.Suggestion.Should().Contain("array or object");
    }

    [Fact]
    public void ValidateAgainstSchema_MissingRequiredProperty_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["task"] = "DotNetCoreCLI@2"
                    // Missing required 'inputs' property
                }
            }
        };

        // Act
        var result = _schemaManager.ValidateAgainstSchema(document);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        
        var error = result.Errors[0];
        error.Code.Should().Be("SCH002");
        error.Message.Should().Contain("required property");
        error.Message.Should().Contain("inputs");
    }

    [Fact]
    public void ValidateAgainstSchema_AdditionalPropertiesNotAllowed_ReturnsWarning()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["script"] = "echo test",
                    ["unknownProperty"] = "value"
                }
            }
        };

        // Act
        var result = _schemaManager.ValidateAgainstSchema(document);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().ContainSingle()
            .Which.Code.Should().Be("SCH010");
    }
}
```

---

### 3.4 TemplateResolver - Template Expansion

```csharp
using Xunit;
using FluentAssertions;
using Moq;
using AdoPipelinesLocalRunner.Core;
using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Tests.Unit.Core;

public class TemplateResolverTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<IYamlParser> _parserMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TemplateResolver _resolver;

    public TemplateResolverTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _parserMock = new Mock<IYamlParser>();
        _loggerMock = new Mock<ILogger>();
        _resolver = new TemplateResolver(
            _fileSystemMock.Object,
            _parserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ResolveTemplates_SimpleTemplateReference_ExpandsSuccessfully()
    {
        // Arrange
        var mainPipeline = new PipelineDocument
        {
            Jobs = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["template"] = "templates/build-job.yml",
                    ["parameters"] = new Dictionary<string, object>
                    {
                        ["buildConfiguration"] = "Release"
                    }
                }
            }
        };

        var templateContent = @"
parameters:
  - name: buildConfiguration
    type: string
    default: Debug

jobs:
  - job: Build
    steps:
      - script: dotnet build -c ${{ parameters.buildConfiguration }}
";

        _fileSystemMock
            .Setup(fs => fs.FileExists("templates/build-job.yml"))
            .Returns(true);

        _fileSystemMock
            .Setup(fs => fs.ReadAllTextAsync("templates/build-job.yml", default))
            .ReturnsAsync(templateContent);

        _parserMock
            .Setup(p => p.Parse<object>(templateContent))
            .Returns(new ParserResult<object>
            {
                Success = true,
                Data = new Dictionary<string, object>
                {
                    ["parameters"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = "buildConfiguration",
                            ["type"] = "string",
                            ["default"] = "Debug"
                        }
                    },
                    ["jobs"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["job"] = "Build",
                            ["steps"] = new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    ["script"] = "dotnet build -c Release"
                                }
                            }
                        }
                    }
                }
            });

        // Act
        var result = await _resolver.ResolveTemplatesAsync(mainPipeline);

        // Assert
        result.Success.Should().BeTrue();
        result.ExpandedDocument.Should().NotBeNull();
        result.ExpandedDocument!.Jobs.Should().HaveCount(1);
        result.Errors.Should().BeEmpty();

        // Verify template was resolved and parameters substituted
        var job = result.ExpandedDocument.Jobs![0] as Dictionary<string, object>;
        job.Should().ContainKey("job");
        job!["job"].Should().Be("Build");
    }

    [Fact]
    public async Task ResolveTemplates_TemplateFileNotFound_ReturnsError()
    {
        // Arrange
        var mainPipeline = new PipelineDocument
        {
            Jobs = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["template"] = "templates/missing.yml"
                }
            }
        };

        _fileSystemMock
            .Setup(fs => fs.FileExists("templates/missing.yml"))
            .Returns(false);

        // Act
        var result = await _resolver.ResolveTemplatesAsync(mainPipeline);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        
        var error = result.Errors[0];
        error.Code.Should().Be("TPL001");
        error.Message.Should().Contain("Template file not found");
        error.Message.Should().Contain("templates/missing.yml");
        error.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public async Task ResolveTemplates_CircularReference_ReturnsError()
    {
        // Arrange
        var mainPipeline = new PipelineDocument
        {
            Jobs = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["template"] = "templates/template-a.yml"
                }
            }
        };

        // template-a references template-b, which references template-a
        _fileSystemMock
            .Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);

        _fileSystemMock
            .Setup(fs => fs.ReadAllTextAsync("templates/template-a.yml", default))
            .ReturnsAsync("jobs:\n  - template: templates/template-b.yml");

        _fileSystemMock
            .Setup(fs => fs.ReadAllTextAsync("templates/template-b.yml", default))
            .ReturnsAsync("jobs:\n  - template: templates/template-a.yml");

        _parserMock
            .Setup(p => p.Parse<object>(It.IsAny<string>()))
            .Returns((string content) => new ParserResult<object>
            {
                Success = true,
                Data = new Dictionary<string, object>()
            });

        // Act
        var result = await _resolver.ResolveTemplatesAsync(mainPipeline);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("TPL002");
        result.Errors[0].Message.Should().Contain("Circular template reference");
    }
}
```

---

### 3.5 VariableProcessor - Variable Expansion

```csharp
using Xunit;
using FluentAssertions;
using Moq;
using AdoPipelinesLocalRunner.Core;
using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Tests.Unit.Core;

public class VariableProcessorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly VariableProcessor _processor;

    public VariableProcessorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _processor = new VariableProcessor(_loggerMock.Object);
    }

    [Fact]
    public void ProcessVariables_SimpleVariableReference_ExpandsCorrectly()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Variables = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["buildConfiguration"] = "Release",
                    ["artifactName"] = "drop"
                }
            },
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["script"] = "dotnet build -c $(buildConfiguration)",
                    ["displayName"] = "Build $(buildConfiguration)"
                },
                new Dictionary<string, object>
                {
                    ["task"] = "PublishBuildArtifacts@1",
                    ["inputs"] = new Dictionary<string, object>
                    {
                        ["artifactName"] = "$(artifactName)"
                    }
                }
            }
        };

        // Act
        var result = _processor.ProcessVariables(document);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedDocument.Should().NotBeNull();
        
        var steps = result.ProcessedDocument!.Steps!;
        var step1 = steps[0] as Dictionary<string, object>;
        step1!["script"].Should().Be("dotnet build -c Release");
        step1["displayName"].Should().Be("Build Release");
        
        var step2 = steps[1] as Dictionary<string, object>;
        var inputs = step2!["inputs"] as Dictionary<string, object>;
        inputs!["artifactName"].Should().Be("drop");
    }

    [Fact]
    public void ProcessVariables_CompileTimeExpression_EvaluatesCorrectly()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Variables = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["majorVersion"] = "1",
                    ["minorVersion"] = "2"
                }
            },
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["script"] = "echo Version: ${{ variables.majorVersion }}.${{ variables.minorVersion }}"
                }
            }
        };

        // Act
        var result = _processor.ProcessVariables(document);

        // Assert
        result.Success.Should().BeTrue();
        var steps = result.ProcessedDocument!.Steps!;
        var step = steps[0] as Dictionary<string, object>;
        step!["script"].Should().Be("echo Version: 1.2");
    }

    [Fact]
    public void ProcessVariables_UndefinedVariable_ReturnsWarning()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Variables = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["knownVar"] = "value"
                }
            },
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["script"] = "echo $(unknownVar)"
                }
            }
        };

        // Act
        var result = _processor.ProcessVariables(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().ContainSingle();
        
        var warning = result.Warnings[0];
        warning.Code.Should().Be("VAR001");
        warning.Message.Should().Contain("Undefined variable");
        warning.Message.Should().Contain("unknownVar");
        warning.Severity.Should().Be(Severity.Warning);
    }

    [Theory]
    [InlineData("$(Build.SourceBranch)", "refs/heads/main")]
    [InlineData("$(Build.BuildId)", "12345")]
    [InlineData("$(Agent.OS)", "Linux")]
    public void ProcessVariables_PredefinedSystemVariable_ExpandsToExpectedValue(
        string variableRef, 
        string expectedValue)
    {
        // Arrange
        var systemVariables = new Dictionary<string, string>
        {
            ["Build.SourceBranch"] = "refs/heads/main",
            ["Build.BuildId"] = "12345",
            ["Agent.OS"] = "Linux"
        };

        var processor = new VariableProcessor(_loggerMock.Object, systemVariables);
        
        var document = new PipelineDocument
        {
            Steps = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["script"] = $"echo {variableRef}"
                }
            }
        };

        // Act
        var result = processor.ProcessVariables(document);

        // Assert
        result.Success.Should().BeTrue();
        var steps = result.ProcessedDocument!.Steps!;
        var step = steps[0] as Dictionary<string, object>;
        step!["script"].Should().Be($"echo {expectedValue}");
    }

    [Fact]
    public void ProcessVariables_VariableScopeOverride_UsesCorrectScope()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Variables = new List<object>
            {
                new Dictionary<string, object> { ["environment"] = "dev" }
            },
            Stages = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["stage"] = "Production",
                    ["variables"] = new List<object>
                    {
                        new Dictionary<string, object> { ["environment"] = "prod" }
                    },
                    ["jobs"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["job"] = "Deploy",
                            ["steps"] = new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    ["script"] = "deploy to $(environment)"
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = _processor.ProcessVariables(document);

        // Assert
        result.Success.Should().BeTrue();
        
        var stages = result.ProcessedDocument!.Stages!;
        var stage = stages[0] as Dictionary<string, object>;
        var jobs = stage!["jobs"] as List<object>;
        var job = jobs![0] as Dictionary<string, object>;
        var steps = job!["steps"] as List<object>;
        var step = steps![0] as Dictionary<string, object>;
        
        // Stage-level variable should override pipeline-level
        step!["script"].Should().Be("deploy to prod");
    }
}
```

---

## 4. Integration Test Strategy

### 4.1 Component Interaction Tests

**Key Scenarios:**

1. **Parse → Validate Flow**
   ```
   YamlParser → SyntaxValidator → SchemaManager
   ```
   - Parse valid pipeline → validate syntax → validate schema
   - Parse with errors → early exit with parse errors
   - Parse valid but invalid syntax → syntax errors
   - Valid syntax but invalid schema → schema errors

2. **Template Resolution Flow**
   ```
   YamlParser → TemplateResolver → YamlParser (recursive)
   ```
   - Parse main file → resolve template → parse template → merge
   - Nested template resolution (3 levels deep)
   - Template parameter validation

3. **Variable Processing Flow**
   ```
   VariableProcessor → entire document traversal
   ```
   - Process after template resolution
   - Respect scope hierarchy
   - Handle expressions in templates

### 4.2 Sample Integration Test

```csharp
public class ValidationPipelineIntegrationTests
{
    [Fact]
    public async Task ValidatePipeline_EndToEnd_ProcessesSuccessfully()
    {
        // Arrange
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile("azure-pipelines.yml", @"
trigger:
  - main

variables:
  buildConfiguration: Release

stages:
  - template: templates/build-stage.yml
    parameters:
      configuration: $(buildConfiguration)
");

        fileSystem.AddFile("templates/build-stage.yml", @"
parameters:
  - name: configuration
    type: string

stages:
  - stage: Build
    jobs:
      - job: BuildJob
        steps:
          - script: dotnet build -c ${{ parameters.configuration }}
");

        var parser = new YamlParser();
        var syntaxValidator = new SyntaxValidator(new ConsoleLogger());
        var schemaManager = new SchemaManager(fileSystem, new ConsoleLogger());
        var templateResolver = new TemplateResolver(fileSystem, parser, new ConsoleLogger());
        var variableProcessor = new VariableProcessor(new ConsoleLogger());

        // Act
        // Step 1: Parse
        var parseResult = await parser.ParseFileAsync<PipelineDocument>("azure-pipelines.yml");
        parseResult.Success.Should().BeTrue();

        // Step 2: Resolve templates
        var resolveResult = await templateResolver.ResolveTemplatesAsync(parseResult.Data!);
        resolveResult.Success.Should().BeTrue();

        // Step 3: Process variables
        var variableResult = variableProcessor.ProcessVariables(resolveResult.ExpandedDocument!);
        variableResult.Success.Should().BeTrue();

        // Step 4: Validate syntax
        var syntaxResult = syntaxValidator.Validate(variableResult.ProcessedDocument!);
        syntaxResult.IsValid.Should().BeTrue();

        // Step 5: Validate schema
        var schemaResult = schemaManager.ValidateAgainstSchema(variableResult.ProcessedDocument!);
        schemaResult.IsValid.Should().BeTrue();

        // Assert
        parseResult.Data.Should().NotBeNull();
        resolveResult.ExpandedDocument.Should().NotBeNull();
        variableResult.ProcessedDocument.Should().NotBeNull();
        
        var finalDocument = variableResult.ProcessedDocument!;
        finalDocument.Stages.Should().HaveCount(1);
        
        // Verify template was resolved and variables expanded
        var stage = finalDocument.Stages![0] as Dictionary<string, object>;
        stage.Should().ContainKey("stage");
        stage!["stage"].Should().Be("Build");
    }
}
```

---

## 5. Test Organization

### 5.1 Project Structure

```
AdoPipelinesLocalRunner.Tests/
├── Unit/
│   ├── Core/
│   │   ├── YamlParserTests.cs
│   │   ├── SyntaxValidatorTests.cs
│   │   ├── SchemaManagerTests.cs
│   │   ├── TemplateResolverTests.cs
│   │   └── VariableProcessorTests.cs
│   └── Commands/
│       └── ValidateCommandTests.cs
├── Integration/
│   ├── ValidationPipelineTests.cs
│   ├── TemplateResolutionTests.cs
│   └── VariableProcessingTests.cs
├── E2E/
│   └── CliValidationTests.cs
├── Fixtures/
│   ├── PipelineDocumentFixtures.cs
│   ├── YamlContentFixtures.cs
│   └── TestDataBuilder.cs
└── TestHelpers/
    ├── FakeFileSystem.cs
    ├── TestLogger.cs
    └── AssertionExtensions.cs
```

### 5.2 Test Naming Convention

**Pattern:** `MethodName_Scenario_ExpectedBehavior`

**Examples:**
- `Parse_ValidYaml_ReturnsParsedDocument`
- `Validate_MissingRequiredField_ReturnsError`
- `ResolveTemplates_CircularReference_ReturnsError`
- `ProcessVariables_UndefinedVariable_ReturnsWarning`

---

## 6. Test Data Management

### 6.1 Test Fixtures

Create reusable test data builders:

```csharp
public class PipelineDocumentBuilder
{
    private readonly PipelineDocument _document = new();

    public PipelineDocumentBuilder WithSteps(params object[] steps)
    {
        return this with { _document = _document with { Steps = steps.ToList() } };
    }

    public PipelineDocumentBuilder WithVariables(Dictionary<string, object> variables)
    {
        return this with { _document = _document with { Variables = new List<object> { variables } } };
    }

    public PipelineDocument Build() => _document;
}
```

### 6.2 Sample YAML Files

Store common test YAML files in `TestData/` directory:

```
TestData/
├── Valid/
│   ├── simple-pipeline.yml
│   ├── multi-stage-pipeline.yml
│   ├── pipeline-with-templates.yml
│   └── pipeline-with-variables.yml
├── Invalid/
│   ├── malformed-yaml.yml
│   ├── missing-required-fields.yml
│   ├── invalid-syntax.yml
│   └── circular-templates.yml
└── Templates/
    ├── build-job.yml
    ├── deploy-job.yml
    └── test-stage.yml
```

---

## 7. Continuous Testing

### 7.1 Test Execution Priorities

**Priority 1 - Fast Feedback (< 5 seconds):**
- Unit tests for all components
- Run on every file save (watch mode)

**Priority 2 - Pre-commit (< 30 seconds):**
- All unit tests
- Critical integration tests
- Run before git commit

**Priority 3 - CI Pipeline (< 2 minutes):**
- All tests (unit + integration + E2E)
- Code coverage analysis
- Run on every push

### 7.2 Code Coverage Reports

**Tools:**
- Coverlet for .NET code coverage
- ReportGenerator for HTML reports
- SonarQube for quality gates

**Minimum Thresholds:**
- Line coverage: 85%
- Branch coverage: 80%
- Fail build if below thresholds

---

## 8. Testing Best Practices

### 8.1 Principles

1. **Fast**: Unit tests should run in < 100ms each
2. **Isolated**: No dependencies between tests
3. **Repeatable**: Same input → same output
4. **Self-validating**: Clear pass/fail
5. **Timely**: Written alongside production code

### 8.2 AAA Pattern Compliance

Every test must follow:

```csharp
[Fact]
public void TestName()
{
    // Arrange - Set up test data and dependencies
    var input = CreateTestInput();
    var sut = new SystemUnderTest();

    // Act - Execute the method being tested
    var result = sut.Method(input);

    // Assert - Verify the expected outcome
    result.Should().Be(expectedValue);
}
```

### 8.3 Common Pitfalls to Avoid

❌ **Don't:**
- Test implementation details
- Use magic numbers/strings without context
- Create interdependent tests
- Mock everything (use real objects when simple)
- Write tests that require manual verification

✅ **Do:**
- Test behavior and contracts
- Use constants or builders for test data
- Ensure tests can run in any order
- Mock only external dependencies
- Make assertions explicit and clear

---

## 9. Success Metrics

### 9.1 Quantitative Targets

| Metric                        | Target | Critical |
|-------------------------------|--------|----------|
| Unit test count               | 45+    | 40+      |
| Integration test count        | 10+    | 8+       |
| E2E test count                | 3+     | 2+       |
| Average test execution time   | < 100ms| < 200ms  |
| Total test suite time         | < 2min | < 3min   |
| Line coverage                 | 85%    | 80%      |
| Branch coverage               | 80%    | 75%      |
| Mutation score                | 75%+   | 70%+     |

### 9.2 Qualitative Goals

- All critical paths have tests
- Error scenarios are covered
- Edge cases are identified and tested
- Test failures provide clear actionable messages
- Tests serve as documentation for component behavior

---

## 10. Next Steps

1. **Week 1:** Implement unit tests for YamlParser and SyntaxValidator
2. **Week 2:** Implement unit tests for SchemaManager, TemplateResolver, VariableProcessor
3. **Week 3:** Implement integration tests for validation pipeline
4. **Week 4:** Implement E2E tests and achieve coverage targets
5. **Week 5:** Set up CI/CD with automated testing and coverage reporting

---

## Appendix A: Test Helper Classes

### A.1 FakeFileSystem

```csharp
public class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new();

    public void AddFile(string path, string content)
    {
        _files[NormalizePath(path)] = content;
    }

    public bool FileExists(string path)
    {
        return _files.ContainsKey(NormalizePath(path));
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
    {
        var normalizedPath = NormalizePath(path);
        if (!_files.ContainsKey(normalizedPath))
            throw new FileNotFoundException($"File not found: {path}");
        
        return Task.FromResult(_files[normalizedPath]);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
```

### A.2 TestLogger

```csharp
public class TestLogger : ILogger
{
    public List<string> Messages { get; } = new();

    public void LogDebug(string message) => Messages.Add($"DEBUG: {message}");
    public void LogInfo(string message) => Messages.Add($"INFO: {message}");
    public void LogWarning(string message) => Messages.Add($"WARN: {message}");
    public void LogError(string message) => Messages.Add($"ERROR: {message}");

    public bool HasError(string containing)
    {
        return Messages.Any(m => m.StartsWith("ERROR:") && m.Contains(containing));
    }
}
```

---

**Document End**
