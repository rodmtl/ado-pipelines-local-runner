using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Templates;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Templates;

public class TemplateHTTPTests
{
    [Fact]
    public async Task ResolveAsync_WithHttpUrl_FetchesAndReturnsContent()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            "https://example.com/templates/build.yml",
            "steps:\n  - script: echo Building"
        );
        var resolver = new HttpTemplateResolver(handler);
        var context = new TemplateResolutionContext { BaseDirectory = "/base" };

        // Act
        var result = await resolver.ResolveAsync("https://example.com/templates/build.yml", context);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("echo Building");
        result.Source.Should().Be("https://example.com/templates/build.yml");
    }

    [Fact]
    public async Task ResolveAsync_WithHttpsUrl_RespectsTimeout()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            "https://example.com/slow.yml",
            "content",
            delayMs: 100
        );
        var resolver = new HttpTemplateResolver(handler, timeoutSeconds: 1);
        var context = new TemplateResolutionContext { BaseDirectory = "/base" };

        // Act - should complete within timeout
        var result = await resolver.ResolveAsync("https://example.com/slow.yml", context);

        // Assert - completes without timeout exception
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_With404_ReturnsError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            "https://example.com/notfound.yml",
            null,
            statusCode: System.Net.HttpStatusCode.NotFound
        );
        var resolver = new HttpTemplateResolver(handler);
        var context = new TemplateResolutionContext { BaseDirectory = "/base" };

        // Act
        var result = await resolver.ResolveAsync("https://example.com/notfound.yml", context);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}

public class TemplateCachingTests
{
    [Fact]
    public async Task ResolveAsync_SecondCall_UsesCacheWithinTTL()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            "https://example.com/template.yml",
            "content1"
        );
        var resolver = new HttpTemplateResolver(handler, cacheTtlSeconds: 60);
        var context = new TemplateResolutionContext { BaseDirectory = "/base" };
        var url = "https://example.com/template.yml";

        // Act - first call
        var result1 = await resolver.ResolveAsync(url, context);

        // Update handler to return different content (should not be called due to cache)
        handler.SetResponse(url, "content2");
        var result2 = await resolver.ResolveAsync(url, context);

        // Assert - both return same cached content
        result1.Content.Should().Be("content1");
        result2.Content.Should().Be("content1"); // from cache
    }

    [Fact]
    public async Task ResolveAsync_AfterCacheTTLExpiry_RefetchesContent()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            "https://example.com/template.yml",
            "content1"
        );
        var resolver = new HttpTemplateResolver(handler, cacheTtlSeconds: 1); // 1 second TTL
        var context = new TemplateResolutionContext { BaseDirectory = "/base" };
        var url = "https://example.com/template.yml";

        // Act - first call
        var result1 = await resolver.ResolveAsync(url, context);

        // Wait for cache to expire
        await Task.Delay(1100);

        // Update handler to return different content
        handler.SetResponse(url, "content2");
        var result2 = await resolver.ResolveAsync(url, context);

        // Assert - second call fetches new content
        result1.Content.Should().Be("content1");
        result2.Content.Should().Be("content2"); // refetched
    }
}

public class TemplateRetryTests
{
    [Fact]
    public async Task ResolveAsync_With500Error_RetriesExponentialBackoff()
    {
        // Arrange - fail twice, then succeed
        var handler = new MockHttpMessageHandler(
            "https://example.com/template.yml",
            "success"
        );
        var attemptsTracked = new List<int>();
        handler.OnAttempt = () => attemptsTracked.Add(1);
        
        // First two calls fail with 500
        handler.FailCount = 2;
        
        var resolver = new HttpTemplateResolver(handler);
        var context = new TemplateResolutionContext { BaseDirectory = "/base" };

        // Act
        var result = await resolver.ResolveAsync("https://example.com/template.yml", context);

        // Assert - succeeded after retries
        result.Success.Should().BeTrue();
        result.Content.Should().Be("success");
        attemptsTracked.Count.Should().BeGreaterThan(1); // multiple attempts
    }

    [Fact]
    public async Task ResolveAsync_WithPersistentFailure_FailsAfterMaxRetries()
    {
        // Arrange - always fail
        var handler = new MockHttpMessageHandler(
            "https://example.com/template.yml",
            null,
            statusCode: System.Net.HttpStatusCode.ServiceUnavailable
        );
        handler.FailCount = int.MaxValue; // always fail
        
        var resolver = new HttpTemplateResolver(handler, maxRetries: 3);
        var context = new TemplateResolutionContext { BaseDirectory = "/base" };

        // Act
        var result = await resolver.ResolveAsync("https://example.com/template.yml", context);

        // Assert - failed after exhausting retries
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}

/// <summary>
/// Mock HTTP handler for testing that simulates HTTP responses without network.
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private Dictionary<string, (string? content, System.Net.HttpStatusCode status)> _responses = new();
    public int FailCount { get; set; } = 0;
    public int AttemptCount { get; private set; } = 0;
    public Action? OnAttempt { get; set; }
    private int _delayMs;

    public MockHttpMessageHandler(string url, string? content, int delayMs = 0, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        _delayMs = delayMs;
        _responses[url] = (content, statusCode);
    }

    public void SetResponse(string url, string? content, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK)
    {
        _responses[url] = (content, status);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        OnAttempt?.Invoke();
        AttemptCount++;

        if (_delayMs > 0)
            await Task.Delay(_delayMs, cancellationToken);

        if (FailCount > 0)
        {
            FailCount--;
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Temporary failure")
            };
        }

        var url = request.RequestUri?.ToString() ?? "";
        if (_responses.TryGetValue(url, out var response))
        {
            return new HttpResponseMessage(response.status)
            {
                Content = response.content != null ? new StringContent(response.content) : null
            };
        }

        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        };
    }
}
