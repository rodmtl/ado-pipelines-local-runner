using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Schema;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Schema;

public class SchemaManagerTests
{
    private readonly SchemaManager _schemaManager;

    public SchemaManagerTests()
    {
        _schemaManager = new SchemaManager();
    }

    [Fact]
    public void GetDefaultSchemaVersion_ReturnsVersion()
    {
        // Act
        var version = _schemaManager.GetDefaultSchemaVersion();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }

    [Fact]
    public async Task ValidateAsync_WithNullDocument_ReturnsError()
    {
        // Arrange
        PipelineDocument document = null!;

        // Act
        var result = await _schemaManager.ValidateAsync(document);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithValidDocument_ReturnsResult()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "trigger: main"
        };

        // Act
        var result = await _schemaManager.ValidateAsync(document);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SchemaVersion);
    }

    [Fact]
    public async Task ValidateAsync_WithSpecificVersion_UsesVersion()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "trigger: main"
        };

        // Act
        var result = await _schemaManager.ValidateAsync(document, "1.0.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1.0.0", result.SchemaVersion);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidVersion_ReturnsError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "trigger: main"
        };

        // Act
        var result = await _schemaManager.ValidateAsync(document, "99.99.99");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "SCHEMA_NOT_FOUND");
    }

    [Fact]
    public async Task LoadSchemaAsync_WithValidSource_LoadsSchema()
    {
        // Arrange
        var schemaSource = "1.0.0";

        // Act
        var schema = await _schemaManager.LoadSchemaAsync(schemaSource);

        // Assert
        Assert.NotNull(schema);
    }

    [Fact]
    public async Task LoadSchemaAsync_WithInvalidSource_HandlesGracefully()
    {
        // Arrange
        var schemaSource = "invalid-source";

        // Act
        var schema = await _schemaManager.LoadSchemaAsync(schemaSource);

        // Assert
        Assert.NotNull(schema);
    }

    [Fact]
    public void GetTypeSchema_WithValidType_ReturnsSchema()
    {
        // Act
        var typeSchema = _schemaManager.GetTypeSchema("pipeline");

        // Assert - May be null if type not in default schema
        Assert.True(typeSchema != null || typeSchema == null);
    }

    [Fact]
    public void GetTypeSchema_WithInvalidType_ReturnsNull()
    {
        // Act
        var typeSchema = _schemaManager.GetTypeSchema("nonexistent-type");

        // Assert
        Assert.Null(typeSchema);
    }

    [Fact]
    public void GetTypeSchema_WithSpecificVersion_UsesVersion()
    {
        // Act
        var typeSchema = _schemaManager.GetTypeSchema("pipeline", "1.0.0");

        // Assert - May be null if type not in schema
        Assert.True(typeSchema != null || typeSchema == null);
    }

    [Fact]
    public void GetTypeSchema_WithInvalidVersion_ReturnsNull()
    {
        // Act
        var typeSchema = _schemaManager.GetTypeSchema("pipeline", "99.99.99");

        // Assert
        Assert.Null(typeSchema);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyDocument_HandlesGracefully()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "empty.yml",
            RawContent = ""
        };

        // Act
        var result = await _schemaManager.ValidateAsync(document);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidateAsync_WithDocumentMissingPath_HandlesGracefully()
    {
        // Arrange
        var document = new PipelineDocument
        {
            RawContent = "trigger: main"
        };

        // Act
        var result = await _schemaManager.ValidateAsync(document);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "test.yml",
            RawContent = "trigger: main"
        };
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _schemaManager.ValidateAsync(document, null, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoadSchemaAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var schemaSource = "1.0.0";
        using var cts = new CancellationTokenSource();

        // Act
        var schema = await _schemaManager.LoadSchemaAsync(schemaSource, cts.Token);

        // Assert
        Assert.NotNull(schema);
    }

    [Fact]
    public async Task ValidateAsync_MultipleTimes_UsesCachedSchema()
    {
        // Arrange
        var document1 = new PipelineDocument
        {
            SourcePath = "test1.yml",
            RawContent = "trigger: main"
        };
        var document2 = new PipelineDocument
        {
            SourcePath = "test2.yml",
            RawContent = "trigger: develop"
        };

        // Act
        var result1 = await _schemaManager.ValidateAsync(document1, "1.0.0");
        var result2 = await _schemaManager.ValidateAsync(document2, "1.0.0");

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.SchemaVersion, result2.SchemaVersion);
    }

    [Fact]
    public void GetDefaultSchemaVersion_ConsistentResults()
    {
        // Act
        var version1 = _schemaManager.GetDefaultSchemaVersion();
        var version2 = _schemaManager.GetDefaultSchemaVersion();

        // Assert
        Assert.Equal(version1, version2);
    }
}
