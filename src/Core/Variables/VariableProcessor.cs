using AdoPipelinesLocalRunner.Contracts;
using System.Text.RegularExpressions;

namespace AdoPipelinesLocalRunner.Core.Variables;

/// <summary>
/// Implementation of IVariableProcessor.
/// Processes and resolves pipeline variables and expressions.
/// Supports $(var) and ${{ variables.var }} syntax.
/// Follows Single Responsibility with separate validation, resolution, and extraction concerns.
/// </summary>
public class VariableProcessor : IVariableProcessor
{
    private static readonly Regex VariableRegex = new(@"\$\(([^)]+)\)|\$\{\{\s*variables\.([^}]+)\s*\}\}", RegexOptions.Compiled);

    /// <inheritdoc />
    public async Task<VariableProcessingResult> ProcessAsync(
        PipelineDocument document,
        VariableContext context,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ProcessInternal(document, context), cancellationToken);
    }

    /// <inheritdoc />
    public string ResolveExpression(string expression, VariableContext context)
    {
        if (string.IsNullOrEmpty(expression))
            return expression;

        var match = VariableRegex.Match(expression);
        if (!match.Success)
            return expression;

        var varName = match.Groups[1].Value ?? match.Groups[2].Value;
        return TryResolveVariable(varName, context, expression);
    }

    /// <summary>
    /// Attempts to resolve a variable from any available context.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Variable resolution logic with precedence handling.
    /// </remarks>
    private string TryResolveVariable(string varName, VariableContext context, string fallback)
    {
        if (TryGetFromContext(varName, context, out var value))
            return value?.ToString() ?? fallback;

        if (context.FailOnUnresolved)
            throw new InvalidOperationException($"Variable '{varName}' is not defined");

        return fallback;
    }

    /// <inheritdoc />
    public string ExpandVariables(string text, VariableContext context)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return VariableRegex.Replace(text, match => ResolveMatch(match, context));
    }

    /// <summary>
    /// Resolves a single regex match to a variable value.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Match resolution logic.
    /// </remarks>
    private string ResolveMatch(Match match, VariableContext context)
    {
        var varName = match.Groups[1].Value ?? match.Groups[2].Value;
        if (TryGetFromContext(varName, context, out var value))
            return value?.ToString() ?? match.Value;

        if (context.FailOnUnresolved)
            throw new InvalidOperationException($"Variable '{varName}' is not defined");

        return match.Value;
    }

    /// <summary>
    /// Attempts to retrieve a variable from any available context source.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Context searching with defined precedence.
    /// </remarks>
    private bool TryGetFromContext(string varName, VariableContext context, out object? value)
    {
        value = null;

        // Preserve existing precedence where SystemVariables take priority
        if (context.SystemVariables?.TryGetValue(varName, out value) == true)
            return true;

        // Scope-aware precedence: innermost to outermost within pipeline-defined variables
        switch (context.Scope)
        {
            case VariableScope.Step:
                if (context.StepVariables?.TryGetValue(varName, out value) == true) return true;
                if (context.JobVariables?.TryGetValue(varName, out value) == true) return true;
                if (context.StageVariables?.TryGetValue(varName, out value) == true) return true;
                if (context.PipelineVariables?.TryGetValue(varName, out value) == true) return true;
                break;
            case VariableScope.Job:
                if (context.JobVariables?.TryGetValue(varName, out value) == true) return true;
                if (context.StageVariables?.TryGetValue(varName, out value) == true) return true;
                if (context.PipelineVariables?.TryGetValue(varName, out value) == true) return true;
                break;
            case VariableScope.Stage:
                if (context.StageVariables?.TryGetValue(varName, out value) == true) return true;
                if (context.PipelineVariables?.TryGetValue(varName, out value) == true) return true;
                break;
            case VariableScope.Pipeline:
            default:
                if (context.PipelineVariables?.TryGetValue(varName, out value) == true) return true;
                break;
        }

        // Other sources
        if (context.EnvironmentVariables?.TryGetValue(varName, out value) == true) return true;
        if (context.Parameters?.TryGetValue(varName, out value) == true) return true;

        return false;
    }

    /// <inheritdoc />
    public ValidationResult ValidateVariables(PipelineDocument document, VariableContext context)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        try
        {
            var varReferences = ExtractVariableReferences(document);
            ValidateVariableReferences(varReferences, document, context, errors, warnings);

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors, Warnings = warnings };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "VARIABLE_VALIDATION_ERROR",
                Message = $"Error validating variables: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = document?.SourcePath ?? "<unknown>", Line = 0, Column = 0 }
            });
            return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
        }
    }

    /// <summary>
    /// Validates that all variable references are defined in the context.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Variable reference validation.
    /// </remarks>
    private void ValidateVariableReferences(HashSet<string> varReferences, PipelineDocument document, 
        VariableContext context, List<ValidationError> errors, List<ValidationError> warnings)
    {
        foreach (var varRef in varReferences)
        {
            if (!IsVariableDefined(varRef, context))
            {
                var issue = new ValidationError
                {
                    Code = "UNDEFINED_VARIABLE",
                    Message = $"Variable '{varRef}' is not defined",
                    Severity = context.FailOnUnresolved ? Severity.Error : Severity.Warning,
                    Location = new SourceLocation { FilePath = document?.SourcePath ?? "<unknown>", Line = 0, Column = 0 },
                    Suggestion = $"Define variable '{varRef}' in variables section or via --var parameter"
                };

                if (context.FailOnUnresolved)
                    errors.Add(issue);
                else
                    warnings.Add(issue);
            }
        }
    }

    /// <summary>
    /// Processes pipeline variables through multiple validation and expansion stages.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Variable processing orchestration.
    /// Coordinates extraction, validation, and expansion steps.
    /// </remarks>
    private VariableProcessingResult ProcessInternal(PipelineDocument document, VariableContext context)
    {
        var errors = new List<ValidationError>();
        var resolvedVars = new List<ResolvedVariable>();

        try
        {
            var documentDefinedVars = ExtractDefinedVariables(document);
            var contextWithDocumentVars = MergeContexts(context, documentDefinedVars);

            var varValidation = ValidateVariables(document, contextWithDocumentVars);
            errors.AddRange(varValidation.Errors);

            CollectResolvedVariables(contextWithDocumentVars, resolvedVars);
            var newContent = TryExpandVariables(document, contextWithDocumentVars, errors);

            return new VariableProcessingResult
            {
                Success = errors.Count == 0,
                ProcessedDocument = string.IsNullOrEmpty(document.RawContent) ? document : document with { RawContent = newContent },
                Errors = errors,
                ResolvedVariables = resolvedVars.ToDictionary(v => v.Name)
            };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "VARIABLE_PROCESSING_ERROR",
                Message = $"Error processing variables: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = document?.SourcePath ?? "<unknown>", Line = 0, Column = 0 }
            });
            return new VariableProcessingResult
            {
                Success = false,
                ProcessedDocument = document,
                Errors = errors,
                ResolvedVariables = resolvedVars.ToDictionary(v => v.Name)
            };
        }
    }

    /// <summary>
    /// Merges document-defined variables with the provided context.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Context merging.
    /// </remarks>
    private VariableContext MergeContexts(VariableContext context, Dictionary<string, object> documentDefinedVars)
    {
        return new VariableContext
        {
            SystemVariables = context.SystemVariables,
            PipelineVariables = MergeDictionaries(context.PipelineVariables, documentDefinedVars),
            EnvironmentVariables = context.EnvironmentVariables,
            Parameters = context.Parameters,
            VariableGroups = context.VariableGroups,
            FailOnUnresolved = context.FailOnUnresolved,
            Scope = context.Scope
        };
    }

    /// <summary>
    /// Collects resolved variables for reporting.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Variable collection for output.
    /// </remarks>
    private void CollectResolvedVariables(VariableContext context, List<ResolvedVariable> resolvedVars)
    {
        if (context.PipelineVariables == null)
            return;

        foreach (var kvp in context.PipelineVariables)
        {
            resolvedVars.Add(new ResolvedVariable
            {
                Name = kvp.Key,
                Value = kvp.Value,
                Source = "pipeline",
                Scope = context.Scope,
                IsSecret = false
            });
        }
    }

    /// <summary>
    /// Attempts to expand variables in document content.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Variable expansion with error handling.
    /// </remarks>
    private string TryExpandVariables(PipelineDocument document, VariableContext context, List<ValidationError> errors)
    {
        if (string.IsNullOrEmpty(document.RawContent))
            return string.Empty;

        try
        {
            return ExpandVariables(document.RawContent, context);
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "VARIABLE_EXPANSION_ERROR",
                Message = ex.Message,
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = document.SourcePath ?? "<unknown>", Line = 0, Column = 0 }
            });
            return document.RawContent;
        }
    }

    /// <summary>
    /// Extracts all variable references found in the document.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Variable reference extraction.
    /// </remarks>
    private HashSet<string> ExtractVariableReferences(PipelineDocument document)
    {
        var refs = new HashSet<string>();
        if (document?.RawContent == null)
            return refs;

        var matches = VariableRegex.Matches(document.RawContent);
        foreach (Match match in matches)
        {
            var varName = match.Groups[1].Value ?? match.Groups[2].Value;
            if (!string.IsNullOrEmpty(varName))
                refs.Add(varName);
        }

        return refs;
    }

    /// <summary>
    /// Checks if a variable is defined in the provided context.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Variable definition checking.
    /// </remarks>
    private bool IsVariableDefined(string varName, VariableContext context)
    {
        return context.SystemVariables?.ContainsKey(varName) == true ||
               context.PipelineVariables?.ContainsKey(varName) == true ||
               context.EnvironmentVariables?.ContainsKey(varName) == true ||
               context.Parameters?.ContainsKey(varName) == true;
    }

    /// <summary>
    /// Extracts variable definitions from the YAML document itself.
    /// Parses pipeline-level and job-level variable sections.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: YAML variable extraction.
    /// </remarks>
    private Dictionary<string, object> ExtractDefinedVariables(PipelineDocument document)
    {
        var vars = new Dictionary<string, object>();
        if (string.IsNullOrEmpty(document?.RawContent))
            return vars;

        var lines = document.RawContent.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var variablesMatch = Regex.Match(line, @"^(\s*)variables\s*:\s*$");
            
            if (variablesMatch.Success)
            {
                ExtractVariablesFromSection(lines, i, variablesMatch.Groups[1].Value.Length, vars);
            }
        }

        return vars;
    }

    /// <summary>
    /// Extracts variables from a variables: section in YAML.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Single variables section extraction.
    /// </remarks>
    private void ExtractVariablesFromSection(string[] lines, int startIndex, int baseIndent, Dictionary<string, object> vars)
    {
        var expectedIndent = baseIndent + 2;

        for (int j = startIndex + 1; j < lines.Length; j++)
        {
            var varLine = lines[j];
            if (string.IsNullOrWhiteSpace(varLine))
                continue;

            var varMatch = Regex.Match(varLine, @"^(\s*)(\w[\w-]*)\s*:\s*(.*)$");
            if (!varMatch.Success)
                break;

            var indent = varMatch.Groups[1].Value.Length;
            if (indent < expectedIndent)
                break;

            if (indent == expectedIndent)
            {
                var varName = varMatch.Groups[2].Value.Trim();
                var varValue = varMatch.Groups[3].Value.Trim();
                if (!string.IsNullOrEmpty(varName))
                    vars[varName] = varValue;
            }
        }
    }

    /// <summary>
    /// Merges two dictionaries with dict2 values taking precedence.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Dictionary merging utility.
    /// </remarks>
    private Dictionary<string, object> MergeDictionaries(IReadOnlyDictionary<string, object>? dict1, Dictionary<string, object> dict2)
    {
        var result = new Dictionary<string, object>(dict1 ?? new Dictionary<string, object>());
        foreach (var kvp in dict2)
        {
            result[kvp.Key] = kvp.Value;
        }
        return result;
    }
}
