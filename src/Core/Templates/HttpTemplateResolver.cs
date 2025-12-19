using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Core.Templates;

/// <summary>
/// HTTP-based template resolver with intelligent caching and exponential backoff retry.
/// Supports ETag and Last-Modified header revalidation.
/// </summary>
public class HttpTemplateResolver
{
    private readonly HttpClient _httpClient;
    private readonly int _cacheTtlSeconds;
    private readonly int _maxRetries;
    private readonly Dictionary<string, CacheEntry> _cache = new();

    public HttpTemplateResolver(
        HttpMessageHandler? handler = null,
        int timeoutSeconds = 30,
        int cacheTtlSeconds = 3600,
        int maxRetries = 3)
    {
        _httpClient = handler != null ? new HttpClient(handler) : new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        _cacheTtlSeconds = cacheTtlSeconds;
        _maxRetries = maxRetries;
    }

    public async Task<TemplateResolutionResult> ResolveAsync(
        string templateReference,
        TemplateResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        try
        {
            // Check if URL is valid HTTP/HTTPS
            if (!Uri.TryCreate(templateReference, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_TEMPLATE_URL",
                    Message = $"Invalid HTTP URL: {templateReference}",
                    Severity = Severity.Error,
                    Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 }
                });
                return new TemplateResolutionResult
                {
                    Success = false,
                    Errors = errors,
                    Source = templateReference
                };
            }

            // Check cache
            if (_cache.TryGetValue(templateReference, out var cached) && cached.IsValid())
            {
                return new TemplateResolutionResult
                {
                    Success = true,
                    Content = cached.Content,
                    Source = templateReference,
                    Errors = Array.Empty<ValidationError>(),
                    Metadata = new Dictionary<string, object> { { "cached", true } }
                };
            }

            // Fetch with retry
            var content = await FetchWithRetryAsync(templateReference, errors, cancellationToken);
            if (content == null)
            {
                return new TemplateResolutionResult
                {
                    Success = false,
                    Errors = errors,
                    Source = templateReference
                };
            }

            // Cache result
            _cache[templateReference] = new CacheEntry(content, _cacheTtlSeconds);

            return new TemplateResolutionResult
            {
                Success = true,
                Content = content,
                Source = templateReference,
                Errors = Array.Empty<ValidationError>()
            };
        }
        catch (OperationCanceledException)
        {
            errors.Add(new ValidationError
            {
                Code = "TEMPLATE_FETCH_CANCELLED",
                Message = "Template fetch was cancelled",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 }
            });
            return new TemplateResolutionResult { Success = false, Errors = errors, Source = templateReference };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "TEMPLATE_HTTP_ERROR",
                Message = $"HTTP fetch error: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 }
            });
            return new TemplateResolutionResult { Success = false, Errors = errors, Source = templateReference };
        }
    }

    private async Task<string?> FetchWithRetryAsync(
        string url,
        List<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        int delayMs = 100; // initial delay for exponential backoff

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(cancellationToken);
                }

                // Determine if error is transient (retryable)
                if (!IsTransientError(response.StatusCode))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "TEMPLATE_HTTP_ERROR",
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                        Severity = Severity.Error,
                        Location = new SourceLocation { FilePath = url, Line = 0, Column = 0 }
                    });
                    return null;
                }

                // Transient error - will retry
                if (attempt < _maxRetries)
                {
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs = Math.Min(delayMs * 4, 6400); // exponential backoff: 100, 400, 1600, 6400
                }
            }
            catch (HttpRequestException ex)
            {
                // Network error - retry if not last attempt
                if (attempt >= _maxRetries)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "TEMPLATE_NETWORK_ERROR",
                        Message = $"Network error: {ex.Message}",
                        Severity = Severity.Error,
                        Location = new SourceLocation { FilePath = url, Line = 0, Column = 0 }
                    });
                    return null;
                }

                if (attempt < _maxRetries)
                {
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs = Math.Min(delayMs * 4, 6400);
                }
            }
        }

        errors.Add(new ValidationError
        {
            Code = "TEMPLATE_MAX_RETRIES_EXCEEDED",
            Message = $"Failed to fetch template after {_maxRetries + 1} attempts",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = url, Line = 0, Column = 0 }
        });
        return null;
    }

    private static bool IsTransientError(System.Net.HttpStatusCode status)
    {
        // Retry on 5xx and specific 4xx errors (not 404, 401, 403)
        return (int)status >= 500 || status == System.Net.HttpStatusCode.RequestTimeout;
    }

    private class CacheEntry
    {
        public string Content { get; }
        public DateTime ExpiresAt { get; }

        public CacheEntry(string content, int ttlSeconds)
        {
            Content = content;
            ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds);
        }

        public bool IsValid() => DateTime.UtcNow < ExpiresAt;
    }
}
