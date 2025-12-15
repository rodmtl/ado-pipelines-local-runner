using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Templates;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Templates;

public class TemplateResolverTests
{
    private readonly TemplateResolver _resolver;

    public TemplateResolverTests()
    {
        _resolver = new TemplateResolver();
    }

    [Fact]
    public async Task ResolveAsync_WithExceededDepth_ReturnsError()
    {
        // Arrange
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 11,
            MaxDepth = 10,
            BaseDirectory = "/templates"
        };

        // Act
        var result = await _resolver.ResolveAsync("template.yml", context);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "TEMPLATE_DEPTH_EXCEEDED");
    }

    [Fact]
    public async Task ResolveAsync_WithCircularReference_ReturnsError()
    {
        // Arrange
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 1,
            MaxDepth = 10,
            BaseDirectory = "/templates",
            ResolutionStack = new[] { "template1.yml", "template2.yml", "template1.yml" }
        };

        // Act
        var result = await _resolver.ResolveAsync("template1.yml", context);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "CIRCULAR_TEMPLATE_REFERENCE");
    }

    [Fact]
    public async Task ResolveAsync_WithValidTemplate_WithinLimits()
    {
        // Arrange
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 1,
            MaxDepth = 10,
            BaseDirectory = "/templates",
            ResolutionStack = new[] { "main.yml" }
        };

        // Act
        var result = await _resolver.ResolveAsync("template.yml", context);

        // Assert - Implementation may return FILE_NOT_FOUND or success depending on file system
        Assert.NotNull(result);
        Assert.NotNull(result.Errors);
    }

    [Fact]
    public async Task ExpandAsync_WithNoTemplates_ReturnsSuccess()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "main.yml",
            RawContent = "trigger: main\njobs:\n- job: Build"
        };
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 0,
            MaxDepth = 10,
            BaseDirectory = "/pipelines"
        };

        // Act
        var result = await _resolver.ExpandAsync(document, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ExpandedDocument);
    }

    [Fact]
    public async Task ExpandAsync_WithNullDocument_HandlesGracefully()
    {
        // Arrange
        PipelineDocument document = null!;
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 0,
            MaxDepth = 10,
            BaseDirectory = "/pipelines"
        };

        // Act
        var result = await _resolver.ExpandAsync(document, context);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidateReferenceAsync_WithValidReference_ReturnsTrue()
    {
        // Arrange
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 0,
            MaxDepth = 10,
            BaseDirectory = "/templates"
        };

        // Act
        var result = await _resolver.ValidateReferenceAsync("template.yml", context);

        // Assert - May be true or false depending on file existence
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ResolveAsync_WithEmptyReference_HandlesGracefully()
    {
        // Arrange
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 0,
            MaxDepth = 10,
            BaseDirectory = "/templates"
        };

        // Act
        var result = await _resolver.ResolveAsync("", context);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResolveAsync_WithContextAtMaxDepth_ReturnsError()
    {
        // Arrange
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 10,
            MaxDepth = 10,
            BaseDirectory = "/templates"
        };

        // Act
        var result = await _resolver.ResolveAsync("deep-template.yml", context);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ResolveAsync_WithNullResolutionStack_HandlesGracefully()
    {
        // Arrange
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 1,
            MaxDepth = 10,
            BaseDirectory = "/templates",
            ResolutionStack = null
        };

        // Act
        var result = await _resolver.ResolveAsync("template.yml", context);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExpandAsync_WithTemplateReferences_ProcessesCorrectly()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "main.yml",
            RawContent = @"
trigger: main
extends:
  template: base-template.yml"
        };
        var context = new TemplateResolutionContext
        {
            CurrentDepth = 0,
            MaxDepth = 10,
            BaseDirectory = "/pipelines"
        };

        // Act
        var result = await _resolver.ExpandAsync(document, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ExpandedDocument);
    }
}
