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
            if (!ValidateDepthLimit(templateReference, context, errors))
                return new TemplateResolutionResult { Success = false, Errors = errors, Source = templateReference };

            // Resolve path and check for circular references
            var resolvedPath = ResolvePath(templateReference, context.BaseDirectory);
            if (!ValidateNoCircularReference(templateReference, resolvedPath, context, errors))
                return new TemplateResolutionResult { Success = false, Errors = errors, Source = templateReference };

            // Check file existence
            if (!ValidateFileExists(templateReference, resolvedPath, errors))
                return new TemplateResolutionResult { Success = false, Errors = errors, Source = templateReference };

            // Load and return template
            return LoadTemplateContent(resolvedPath, templateReference, errors);
        }
        catch (Exception ex)
        {
            errors.Add(CreateTemplateResolutionException(templateReference, ex));
            return new TemplateResolutionResult { Success = false, Errors = errors, Source = templateReference };
        }
    }

    /// <summary>
    /// Validates that the template depth does not exceed maximum.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Depth validation.
    /// </remarks>
    private bool ValidateDepthLimit(string templateReference, TemplateResolutionContext context, List<ValidationError> errors)
    {
        if (context.CurrentDepth <= context.MaxDepth)
            return true;

        errors.Add(new ValidationError
        {
            Code = "TEMPLATE_DEPTH_EXCEEDED",
            Message = $"Template depth exceeds maximum ({context.MaxDepth})",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 },
            Suggestion = $"Reduce template nesting or increase max depth (currently {context.MaxDepth})"
        });
        return false;
    }

    /// <summary>
    /// Validates that no circular template reference exists.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Circular reference detection.
    /// Checks both full paths and filenames for flexibility.
    /// </remarks>
    private bool ValidateNoCircularReference(string templateReference, string resolvedPath, 
        TemplateResolutionContext context, List<ValidationError> errors)
    {
        var stack = context.ResolutionStack ?? Array.Empty<string>();
        if (!IsCircularReference(resolvedPath, templateReference, stack))
            return true;

        errors.Add(new ValidationError
        {
            Code = "CIRCULAR_TEMPLATE_REFERENCE",
            Message = $"Circular template reference detected: {resolvedPath}",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 },
            Suggestion = $"Template chain: {string.Join(" -> ", stack)} -> {resolvedPath}"
        });
        return false;
    }

    /// <summary>
    /// Detects if a template is already in the resolution stack.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Circular reference checking.
    /// </remarks>
    private bool IsCircularReference(string resolvedPath, string templateReference, IEnumerable<string> stack)
    {
        var resolvedFileName = Path.GetFileName(resolvedPath);
        var referenceFileName = Path.GetFileName(templateReference);

        return stack.Any(item =>
            string.Equals(item, resolvedPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item, templateReference, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(item), resolvedFileName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(item), referenceFileName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates that the template file exists.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: File existence validation.
    /// </remarks>
    private bool ValidateFileExists(string templateReference, string resolvedPath, List<ValidationError> errors)
    {
        if (File.Exists(resolvedPath))
            return true;

        errors.Add(new ValidationError
        {
            Code = "TEMPLATE_NOT_FOUND",
            Message = $"Template file not found: {resolvedPath}",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 },
            Suggestion = "Verify template path and ensure file exists"
        });
        return false;
    }

    /// <summary>
    /// Loads the content of a template file.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Template content loading and result creation.
    /// </remarks>
    private TemplateResolutionResult LoadTemplateContent(string resolvedPath, string templateReference, List<ValidationError> errors)
    {
        try
        {
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
                Code = "TEMPLATE_READ_ERROR",
                Message = $"Error reading template file: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 }
            });
            return new TemplateResolutionResult { Success = false, Errors = errors, Source = resolvedPath };
        }
    }

    /// <summary>
    /// Creates an error result for template resolution exceptions.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Exception error creation.
    /// </remarks>
    private ValidationError CreateTemplateResolutionException(string templateReference, Exception ex) =>
        new()
        {
            Code = "TEMPLATE_RESOLUTION_ERROR",
            Message = $"Error resolving template: {ex.Message}",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = templateReference, Line = 0, Column = 0 }
        };

    private TemplateExpansionResult ExpandInternal(PipelineDocument document, TemplateResolutionContext context)
    {
        var errors = new List<ValidationError>();
        var expandedDocument = document;
        var resolvedTemplates = new List<ResolvedTemplate>();

        try
        {
            var templateRefs = ExtractTemplateReferences(document.RawContent);
            var currentStack = context.ResolutionStack?.ToList() ?? new List<string>();

            ProcessTemplateReferences(templateRefs, context, currentStack, errors, resolvedTemplates);

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
            errors.Add(CreateTemplateExpansionException(document, ex));
            return new TemplateExpansionResult
            {
                Success = false,
                Errors = errors,
                ExpandedDocument = document,
                ResolvedTemplates = new List<ResolvedTemplate>()
            };
        }
    }

    /// <summary>
    /// Processes all template references and resolves them.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Template reference processing orchestration.
    /// </remarks>
    private void ProcessTemplateReferences(IReadOnlyList<string> templateRefs, TemplateResolutionContext context,
        List<string> currentStack, List<ValidationError> errors, List<ResolvedTemplate> resolvedTemplates)
    {
        foreach (var templateRef in templateRefs)
        {
            var nextContext = CreateChildResolutionContext(context, currentStack);
            var resolution = ResolveInternal(templateRef, nextContext);

            if (!resolution.Success)
            {
                errors.AddRange(resolution.Errors);
                continue;
            }

            resolvedTemplates.Add(CreateResolvedTemplate(templateRef, resolution, nextContext));
        }
    }

    /// <summary>
    /// Creates a child resolution context with increased depth.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Context creation for recursive resolution.
    /// </remarks>
    private TemplateResolutionContext CreateChildResolutionContext(TemplateResolutionContext parent, List<string> stack) =>
        new()
        {
            BaseDirectory = parent.BaseDirectory,
            Parameters = parent.Parameters,
            MaxDepth = parent.MaxDepth,
            CurrentDepth = parent.CurrentDepth + 1,
            ResolutionStack = stack,
            RepositoryContext = parent.RepositoryContext
        };

    /// <summary>
    /// Creates a resolved template record from a resolution result.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: ResolvedTemplate creation.
    /// </remarks>
    private ResolvedTemplate CreateResolvedTemplate(string templateRef, TemplateResolutionResult resolution, 
        TemplateResolutionContext context) =>
        new()
        {
            Reference = templateRef,
            ResolvedSource = resolution.Source,
            Parameters = context.Parameters,
            Depth = context.CurrentDepth
        };

    /// <summary>
    /// Creates an error result for template expansion exceptions.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Exception error creation for expansion.
    /// </remarks>
    private ValidationError CreateTemplateExpansionException(PipelineDocument? document, Exception ex) =>
        new()
        {
            Code = "TEMPLATE_EXPANSION_ERROR",
            Message = $"Error expanding templates: {ex.Message}",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = document?.SourcePath ?? "<unknown>", Line = 0, Column = 0 }
        };

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
