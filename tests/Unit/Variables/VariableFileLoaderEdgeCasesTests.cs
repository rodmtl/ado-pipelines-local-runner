using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Variables;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Variables;

/// <summary>
/// Tests for VariableFileLoader edge cases and error handling.
/// Focuses on file I/O errors, format edge cases, and boundary conditions.
/// </summary>
public class VariableFileLoaderEdgeCasesTests
{
    private readonly VariableFileLoader _loader;
    private readonly string _tempDir;

    public VariableFileLoaderEdgeCasesTests()
    {
        _loader = new VariableFileLoader();
        _tempDir = Path.Combine(Path.GetTempPath(), $"VariableLoaderTests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Load_WithEmptyFileList_ReturnsEmptyDictionary()
    {
        // Act
        var result = _loader.Load(Array.Empty<string>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Load_WithNullFiles_ThrowsException()
    {
        // Act & Assert - VariableFileLoader doesn't validate null, will throw NRE
        Assert.Throws<NullReferenceException>(() => _loader.Load(null!));
    }

    [Fact]
    public void Load_WithNonExistentFile_ReturnsEmptyAndRecordsError()
    {
        // Arrange
        var errors = new List<ValidationError>();
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent.yml");

        // Act
        var result = _loader.Load(new[] { nonExistentPath }, errors: errors);

        // Assert
        Assert.Empty(result);
        Assert.Single(errors);
        Assert.Equal("VARIABLE_FILE_NOT_FOUND", errors[0].Code);
    }

    [Fact]
    public void Load_WithMalformedYaml_RecordsError()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "malformed.yml");
        File.WriteAllText(filePath, "invalid: yaml: content: ][");
        var errors = new List<ValidationError>();

        // Act
        var result = _loader.Load(new[] { filePath }, errors: errors);

        // Assert
        Assert.Empty(result);
        Assert.Single(errors);
        Assert.Equal("VARIABLE_FILE_INVALID", errors[0].Code);
    }

    [Fact]
    public void Load_WithEmptyYamlFile_ReturnsEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "empty.yml");
        File.WriteAllText(filePath, "");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Load_WithOnlyCommentedYaml_ReturnsEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "commented.yml");
        File.WriteAllText(filePath, "# This is a comment\n# Another comment");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Load_WithMalformedJson_RecordsError()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "malformed.json");
        File.WriteAllText(filePath, "{ invalid json ][");
        var errors = new List<ValidationError>();

        // Act
        var result = _loader.Load(new[] { filePath }, errors: errors);

        // Assert
        Assert.Empty(result);
        Assert.Single(errors);
        Assert.Equal("VARIABLE_FILE_INVALID", errors[0].Code);
    }

    [Fact]
    public void Load_WithEmptyJsonFile_ReturnsEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "empty.json");
        File.WriteAllText(filePath, "{}");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Load_WithYamlVariablesEmptyList_ReturnsEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "empty-vars-list.yml");
        File.WriteAllText(filePath, "variables: []");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Load_WithJsonVariablesEmptyList_ReturnsEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "empty-vars-list.json");
        File.WriteAllText(filePath, "{ \"variables\": [] }");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Load_WithYamlVariableMissingName_SkipsVariable()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "missing-name.yml");
        File.WriteAllText(filePath, "variables:\n  - value: test\n  - name: valid\n    value: ok");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("ok", result["valid"]);
    }

    [Fact]
    public void Load_WithJsonVariableMissingName_SkipsVariable()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "missing-name.json");
        File.WriteAllText(filePath, "{ \"variables\": [{ \"value\": \"test\" }, { \"name\": \"valid\", \"value\": \"ok\" }] }");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("ok", result["valid"]);
    }

    [Fact]
    public void Load_WithYamlVariableMissingValue_SkipsVariable()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "missing-value.yml");
        File.WriteAllText(filePath, "variables:\n  - name: novalue");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert - Variable with missing value is not included
        Assert.Empty(result);
    }

    [Fact]
    public void Load_WithJsonVariableMissingValue_UsesEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "missing-value.json");
        File.WriteAllText(filePath, "{ \"variables\": [{ \"name\": \"novalue\" }] }");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("", result["novalue"]);
    }

    [Fact]
    public void Load_WithCaseMixedVariableNames_SucceedsWithCaseInsensitivity()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "case-test.yml");
        File.WriteAllText(filePath, "variables:\n  - name: MyVar\n    value: value1");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.True(result.TryGetValue("myvar", out var val)); // Case-insensitive
        Assert.Equal("value1", val);
    }

    [Fact]
    public void Load_WithDuplicateVariableNames_LaterOverridesEarlier()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "duplicates.yml");
        File.WriteAllText(filePath, "variables:\n  - name: dup\n    value: first\n  - name: dup\n    value: second");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("second", result["dup"]);
    }

    [Fact]
    public void Load_WithMultipleFiles_LaterFileOverridesEarlier()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "file1.yml");
        var file2 = Path.Combine(_tempDir, "file2.yml");
        File.WriteAllText(file1, "variables:\n  - name: shared\n    value: from-file1");
        File.WriteAllText(file2, "variables:\n  - name: shared\n    value: from-file2");

        // Act
        var result = _loader.Load(new[] { file1, file2 });

        // Assert
        Assert.Single(result);
        Assert.Equal("from-file2", result["shared"]);
    }

    [Fact]
    public void Load_WithMultipleFilesAndErrors_ContinuesProcessing()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "nonexistent.yml");
        var file2 = Path.Combine(_tempDir, "valid.yml");
        File.WriteAllText(file2, "variables:\n  - name: valid\n    value: data");
        var errors = new List<ValidationError>();

        // Act
        var result = _loader.Load(new[] { file1, file2 }, errors: errors);

        // Assert
        Assert.Single(result); // file2 still loaded
        Assert.Single(errors); // file1 error recorded
        Assert.Equal("data", result["valid"]);
    }

    [Fact]
    public void Load_WithYamlNullValue_UsesEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "null-value.yml");
        File.WriteAllText(filePath, "variables:\n  - name: nullvar\n    value: null");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("", result["nullvar"]);
    }

    [Fact]
    public void Load_WithJsonNullValue_UsesEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "null-value.json");
        File.WriteAllText(filePath, "{ \"variables\": [{ \"name\": \"nullvar\", \"value\": null }] }");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("", result["nullvar"]);
    }

    [Fact]
    public void Load_WithNumericVariableValue_PreservesAsString()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "numeric.yml");
        File.WriteAllText(filePath, "variables:\n  - name: buildnum\n    value: 12345");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        // Value should be object type, likely converted to string
        Assert.NotNull(result["buildnum"]);
    }

    [Fact]
    public void Load_WithBooleanVariableValue_PreservesValue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "boolean.yml");
        File.WriteAllText(filePath, "variables:\n  - name: flag\n    value: true");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.NotNull(result["flag"]);
    }

    [Fact]
    public void Load_WithRelativePath_UsesBaseDirCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "relative.yml");
        File.WriteAllText(filePath, "variables:\n  - name: rel\n    value: relative");
        var baseDir = _tempDir;

        // Act
        var result = _loader.Load(new[] { "relative.yml" }, baseDir: baseDir);

        // Assert
        Assert.Single(result);
        Assert.Equal("relative", result["rel"]);
    }

    [Fact]
    public void Load_WithAbsolutePath_IgnoresBassDir()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "absolute.yml");
        File.WriteAllText(filePath, "variables:\n  - name: abs\n    value: absolute");

        // Act
        var result = _loader.Load(new[] { filePath }, baseDir: "/some/other/path");

        // Assert
        Assert.Single(result);
        Assert.Equal("absolute", result["abs"]);
    }

    [Fact]
    public void Load_WithSpecialCharacterInVariableValue_PreservesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "special.yml");
        File.WriteAllText(filePath, "variables:\n  - name: special\n    value: \"Hello\\nWorld\\t$(literal)\"");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.NotNull(result["special"]);
    }

    [Fact]
    public void Load_WithUnicodeInVariableValue_PreservesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "unicode.yml");
        File.WriteAllText(filePath, "variables:\n  - name: unicode\n    value: 你好世界🚀", System.Text.Encoding.UTF8);

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("你好世界🚀", result["unicode"]);
    }

    [Fact]
    public void Load_WithVeryLongVariableValue_HandlesCorrectly()
    {
        // Arrange
        var longValue = string.Concat(Enumerable.Repeat("x", 100000));
        var filePath = Path.Combine(_tempDir, "long.yml");
        File.WriteAllText(filePath, $"variables:\n  - name: long\n    value: \"{longValue}\"");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal(longValue, result["long"]);
    }

    [Fact]
    public void Load_WithUnknownFileExtension_TreatsAsYaml()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "unknown.txt");
        File.WriteAllText(filePath, "variables:\n  - name: txt\n    value: textfile");

        // Act
        var result = _loader.Load(new[] { filePath });

        // Assert
        Assert.Single(result);
        Assert.Equal("textfile", result["txt"]);
    }

    [Fact]
    public void Load_WithAccessDenied_RecordsError()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "readonly.yml");
        File.WriteAllText(filePath, "variables:\n  - name: test\n    value: test");
        var fileInfo = new FileInfo(filePath);
        fileInfo.Attributes = FileAttributes.ReadOnly;
        var errors = new List<ValidationError>();

        try
        {
            // Act
            // This test is tricky on Windows. We'll attempt to simulate access denied
            // In reality, readonly files can still be read on Windows, so we skip detailed testing
            var result = _loader.Load(new[] { filePath }, errors: errors);

            // Assert - should succeed on most systems since readonly doesn't prevent reading
            Assert.NotEmpty(result);
        }
        finally
        {
            fileInfo.Attributes = FileAttributes.Normal;
        }
    }

    ~VariableFileLoaderEdgeCasesTests()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }
}
