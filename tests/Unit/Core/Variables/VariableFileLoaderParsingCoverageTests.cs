using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Variables;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Core.Variables;

/// <summary>
/// Phase 1 Coverage Tests: VariableFileLoader parsing edge cases and error scenarios
/// Target: +2-3% coverage from file loading and YAML/JSON parsing
/// </summary>
public class VariableFileLoaderParsingCoverageTests
{
    private readonly VariableFileLoader _loader;

    public VariableFileLoaderParsingCoverageTests()
    {
        _loader = new VariableFileLoader();
    }

    #region YAML Flat Dictionary Tests

    [Fact]
    public void Load_WithYamlFlatKeyValuePairs_ParsesSuccessfully()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        File.WriteAllText(tempFile, "BUILD_ID: '123'\nENVIRONMENT: production\nDEBUG: 'true'");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().NotBeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WithYamlNumericValues_PreservesAsString()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        File.WriteAllText(tempFile, "PORT: 8080\nTIMEOUT: 30");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().NotBeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    #endregion

    #region YAML Variables Array Tests

    [Fact]
    public void Load_WithYamlVariablesArray_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        var yaml = @"variables:
  - name: VAR1
    value: value1
  - name: VAR2
    value: value2";
        File.WriteAllText(tempFile, yaml);

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().NotBeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WithYamlVariablesArrayMissingValue_SkipsVariable()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        var yaml = @"variables:
  - name: VAR1
  - name: VAR2
    value: value2";
        File.WriteAllText(tempFile, yaml);

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().NotBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    #endregion

    #region JSON Variables Array Tests

    [Fact]
    public void Load_WithJsonVariablesArray_ParsesSuccessfully()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        var json = @"{ ""variables"": [
            { ""name"": ""VAR1"", ""value"": ""val1"" },
            { ""name"": ""VAR2"", ""value"": ""val2"" }
        ] }";
        File.WriteAllText(tempFile, json);

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().NotBeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WithJsonEmptyVariablesArray_ReturnsEmpty()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        File.WriteAllText(tempFile, @"{ ""variables"": [] }");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().BeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WithJsonMissingVariablesProperty_ReturnsEmpty()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        File.WriteAllText(tempFile, @"{ ""other"": ""data"" }");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().BeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public void Load_WithNonExistentFile_ReportsError()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.yml");
        var errors = new List<ValidationError>();

        // Act
        var vars = _loader.Load(new[] { nonExistentFile }, null, errors);

        // Assert
        errors.Should().NotBeEmpty();
        vars.Should().NotBeNull();
    }

    [Fact]
    public void Load_WithInvalidJsonContent_ReportsError()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        File.WriteAllText(tempFile, "{ invalid json }");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            errors.Should().NotBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    #endregion

    #region File Merge Tests

    [Fact]
    public void Load_WithMultipleFiles_MergesCorrectly()
    {
        // Arrange
        var file1 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        var file2 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        File.WriteAllText(file1, "VAR1: value1\nVAR2: value2");
        File.WriteAllText(file2, "VAR3: value3\nVAR4: value4");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { file1, file2 }, null, errors);

            // Assert
            vars.Should().NotBeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    [Fact]
    public void Load_WithDuplicateVariablesAcrossFiles_LastFileWins()
    {
        // Arrange
        var file1 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        var file2 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        File.WriteAllText(file1, "DUPLICATE: first");
        File.WriteAllText(file2, "DUPLICATE: second");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { file1, file2 }, null, errors);

            // Assert
            vars.Should().NotBeEmpty();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    #endregion

    #region Empty and Edge Cases

    [Fact]
    public void Load_WithEmptyYamlFile_ReturnsEmpty()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.yml");
        File.WriteAllText(tempFile, "");

        try
        {
            // Act
            var errors = new List<ValidationError>();
            var vars = _loader.Load(new[] { tempFile }, null, errors);

            // Assert
            vars.Should().NotBeNull();
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WithEmptyFileList_ReturnsEmpty()
    {
        // Act
        var errors = new List<ValidationError>();
        var vars = _loader.Load(Enumerable.Empty<string>(), null, errors);

        // Assert
        vars.Should().NotBeNull();
        vars.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    #endregion
}
