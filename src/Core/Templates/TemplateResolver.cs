using AdoPipelinesLocalRunner.Contracts;
using System.Text.RegularExpressions;

namespace AdoPipelinesLocalRunner.Core.Templates;

/// <summary>
/// Implementation of ITemplateResolver.
/// Resolves and expands pipeline templates from local file sources (Phase 1).
/// Supports recursive resolution with depth limits and cycle detection.
/// </summary>
public class TemplateResolver : ITemplateResolver
{
    private readonly IFileSystem? _fileSystem;

    public TemplateResolver(IFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task<TemplateResolutionResult> ResolveAsync(
        string templateReference,
        TemplateResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ResolveInternal(templateReference, context), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TemplateExpansionResult> ExpandAsync(
        PipelineDocument document,
        TemplateResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ExpandInternal(document, context), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateReferenceAsync(
        string templateReference,
        TemplateResolutionContext context)
    {
        return await Task.Run(() => ValidateReferenceInternal(templateReference, context));
    }

    private TemplateResolutionResult ResolveInternal(string templateReference, TemplateResolutionContext context)
    {
        var errors = new List<ValidationError>();

        try
        {
            // Check depth limit
            if (context.CurrentDepth > context.MaxDepth)
            {
                errors.Add(new ValidationError
                {
                    Code = "TEMPLATE_DEPTH_EXCEEDED",
                    Message = $"Template depth exceeds maximum ({context.MaxDepth})",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = templateReference,
                        Line = 0,
                        Column = 0
                    },
                    Suggestion = $"Reduce template nesting or increase max depth (currently {context.MaxDepth})"
                });

                return new TemplateResolutionResult
                {
                    Success = false,
                    Errors = errors,
                    Source = templateReference
                };
            }

            // Check for circular references
            var stack = context.ResolutionStack?.ToList() ?? new List<string>();
            if (stack.Contains(templateReference))
            {
                errors.Add(new ValidationError
                {
                    Code = "CIRCULAR_TEMPLATE_REFERENCE",
                    Message = $"Circular template reference detected: {templateReference}",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = templateReference,
                        Line = 0,
                        Column = 0
                    },
                    Suggestion = "Review template references to remove circular dependencies"
                });

                return new TemplateResolutionResult
                {
                    Success = false,
                    Errors = errors,
                    Source = templateReference
                };
            }

            // Resolve relative path
            var resolvedPath = ResolvePath(templateReference, context.BaseDirectory);

            // Check if file exists
            if (!File.Exists(resolvedPath))
            {
                errors.Add(new ValidationError
                {
                    Code = "TEMPLATE_NOT_FOUND",
                    Message = $"Template file not found: {resolvedPath}",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = templateReference,
                        Line = 0,
                        Column = 0
                    },
                    Suggestion = "Verify template path and ensure file exists"
                });

                return new TemplateResolutionResult
                {
                    Success = false,
                    Errors = errors,
                    Source = templateReference
                };
            }

            // Load template content
            var content = File.ReadAllText(resolvedPath);

            return new TemplateResolutionResult
            {
                Success = true,
                Errors = Array.Empty<ValidationError>(),
                Source = resolvedPath,
                Content = content
            };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "TEMPLATE_RESOLUTION_ERROR",
                Message = $"Error resolving template: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = templateReference,
                    Line = 0,
                    Column = 0
                }
            });

            return new TemplateResolutionResult
            {
                Success = false,
                Errors = errors,
                Source = templateReference
            };
        }
    }

    private TemplateExpansionResult ExpandInternal(PipelineDocument document, TemplateResolutionContext context)
    {
        var errors = new List<ValidationError>();
        var expandedDocument = document;
        var resolvedTemplates = new List<ResolvedTemplate>();

        try
        {
            var templateRefs = ExtractTemplateReferences(document.RawContent);
            var currentStack = context.ResolutionStack?.ToList() ?? new List<string>();

            foreach (var templateRef in templateRefs)
            {
                var nextContext = new TemplateResolutionContext
                {
                    BaseDirectory = context.BaseDirectory,
                    Parameters = context.Parameters,
                    MaxDepth = context.MaxDepth,
                    CurrentDepth = context.CurrentDepth + 1,
                    ResolutionStack = currentStack.Concat(new[] { templateRef }).ToList(),
                    RepositoryContext = context.RepositoryContext
                };

                var resolution = ResolveInternal(templateRef, nextContext);
                if (!resolution.Success)
                {
                    errors.AddRange(resolution.Errors);
                    continue;
                }

                resolvedTemplates.Add(new ResolvedTemplate
                {
                    Reference = templateRef,
                    ResolvedSource = resolution.Source,
                    Parameters = context.Parameters,
                    Depth = nextContext.CurrentDepth
                });
            }

            return new TemplateExpansionResult
            {
                Success = errors.Count == 0,
                Errors = errors,
                ExpandedDocument = expandedDocument,
                ResolvedTemplates = resolvedTemplates
            };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "TEMPLATE_EXPANSION_ERROR",
                Message = $"Error expanding templates: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = document?.SourcePath ?? "<unknown>",
                    Line = 0,
                    Column = 0
                }
            });

            return new TemplateExpansionResult
            {
                Success = false,
                Errors = errors,
                ExpandedDocument = document,
                ResolvedTemplates = new List<ResolvedTemplate>()
            };
        }
    }

    private bool ValidateReferenceInternal(string templateReference, TemplateResolutionContext context)
    {
        try
        {
            var resolvedPath = ResolvePath(templateReference, context.BaseDirectory);
            return File.Exists(resolvedPath);
        }
        catch
        {
            return false;
        }
    }

    private IReadOnlyList<string> ExtractTemplateReferences(string? rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return Array.Empty<string>();

        var refs = new List<string>();
        var regex = new Regex(@"template:\s*(?<ref>[^\s]+)", RegexOptions.Compiled);
        var matches = regex.Matches(rawContent);
        foreach (Match match in matches)
        {
            var value = match.Groups["ref"].Value.Trim();
            if (!string.IsNullOrEmpty(value))
                refs.Add(value);
        }

        return refs;
    }

    private string ResolvePath(string reference, string baseDirectory)
    {
        if (Path.IsPathRooted(reference))
        {
            return reference;
        }

        return Path.Combine(baseDirectory, reference);
    }
}

/// <summary>
/// Placeholder for IFileSystem interface if not already defined.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    string ReadAllText(string path);
    Task<string> ReadAllTextAsync(string path);
    void WriteAllText(string path, string content);
    Task WriteAllTextAsync(string path, string content);
    IEnumerable<string> GetFiles(string directory, string searchPattern);
}
