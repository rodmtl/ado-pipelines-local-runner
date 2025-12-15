using AdoPipelinesLocalRunner.Contracts;
using System.Text.RegularExpressions;

namespace AdoPipelinesLocalRunner.Core.Variables;

/// <summary>
/// Implementation of IVariableProcessor.
/// Processes and resolves pipeline variables and expressions.
/// Supports $(var) and ${{ variables.var }} syntax.
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

        // Try to resolve from various contexts
        if (context.SystemVariables?.TryGetValue(varName, out var sysValue) == true)
            return sysValue?.ToString() ?? expression;

        if (context.PipelineVariables?.TryGetValue(varName, out var pipeValue) == true)
            return pipeValue?.ToString() ?? expression;

        if (context.EnvironmentVariables?.TryGetValue(varName, out var envValue) == true)
            return envValue?.ToString() ?? expression;

        if (context.Parameters?.TryGetValue(varName, out var paramValue) == true)
            return paramValue?.ToString() ?? expression;

        // Variable not found
        if (context.FailOnUnresolved)
            throw new InvalidOperationException($"Variable '{varName}' is not defined");

        return expression; // Return original if unresolved and allowed
    }

    /// <inheritdoc />
    public string ExpandVariables(string text, VariableContext context)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return VariableRegex.Replace(text, match =>
        {
            var varName = match.Groups[1].Value ?? match.Groups[2].Value;

            // Try each context in order of precedence
            if (context.SystemVariables?.TryGetValue(varName, out var sysValue) == true)
                return sysValue?.ToString() ?? match.Value;

            if (context.PipelineVariables?.TryGetValue(varName, out var pipeValue) == true)
                return pipeValue?.ToString() ?? match.Value;

            if (context.EnvironmentVariables?.TryGetValue(varName, out var envValue) == true)
                return envValue?.ToString() ?? match.Value;

            if (context.Parameters?.TryGetValue(varName, out var paramValue) == true)
                return paramValue?.ToString() ?? match.Value;

            // Not found - return original or throw based on context
            if (context.FailOnUnresolved)
                throw new InvalidOperationException($"Variable '{varName}' is not defined");

            return match.Value; // Return original expression
        });
    }

    /// <inheritdoc />
    public ValidationResult ValidateVariables(PipelineDocument document, VariableContext context)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        try
        {
            // Scan document for variable references
            var varReferences = ExtractVariableReferences(document);

            foreach (var varRef in varReferences)
            {
                var found = IsVariableDefined(varRef, context);
                if (!found)
                {
                    var issue = new ValidationError
                    {
                        Code = "UNDEFINED_VARIABLE",
                        Message = $"Variable '{varRef}' is not defined",
                        Severity = context.FailOnUnresolved ? Severity.Error : Severity.Warning,
                        Location = new SourceLocation
                        {
                            FilePath = document?.SourcePath ?? "<unknown>",
                            Line = 0,
                            Column = 0
                        },
                        Suggestion = $"Define variable '{varRef}' in variables section or via --var parameter"
                    };

                    if (context.FailOnUnresolved)
                        errors.Add(issue);
                    else
                        warnings.Add(issue);
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "VARIABLE_VALIDATION_ERROR",
                Message = $"Error validating variables: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = document?.SourcePath ?? "<unknown>",
                    Line = 0,
                    Column = 0
                }
            });

            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }
    }

    private VariableProcessingResult ProcessInternal(PipelineDocument document, VariableContext context)
    {
        var errors = new List<ValidationError>();
        var resolvedVars = new List<ResolvedVariable>();

        try
        {
            // In Phase 1, basic variable validation
            var varValidation = ValidateVariables(document, context);
            errors.AddRange(varValidation.Errors);

            var resolvedVarsDict = resolvedVars.ToDictionary(v => v.Name);
            return new VariableProcessingResult
            {
                Success = errors.Count == 0,
                ProcessedDocument = document,
                Errors = errors,
                ResolvedVariables = resolvedVarsDict
            };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "VARIABLE_PROCESSING_ERROR",
                Message = $"Error processing variables: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = document?.SourcePath ?? "<unknown>",
                    Line = 0,
                    Column = 0
                }
            });

            var resolvedVarsDict = resolvedVars.ToDictionary(v => v.Name);
            return new VariableProcessingResult
            {
                Success = false,
                ProcessedDocument = document,
                Errors = errors,
                ResolvedVariables = resolvedVarsDict
            };
        }
    }

    private HashSet<string> ExtractVariableReferences(PipelineDocument document)
    {
        var refs = new HashSet<string>();

        if (document?.RawContent != null)
        {
            var matches = VariableRegex.Matches(document.RawContent);
            foreach (Match match in matches)
            {
                var varName = match.Groups[1].Value ?? match.Groups[2].Value;
                if (!string.IsNullOrEmpty(varName))
                    refs.Add(varName);
            }
        }

        return refs;
    }

    private bool IsVariableDefined(string varName, VariableContext context)
    {
        return context.SystemVariables?.ContainsKey(varName) == true ||
               context.PipelineVariables?.ContainsKey(varName) == true ||
               context.EnvironmentVariables?.ContainsKey(varName) == true ||
               context.Parameters?.ContainsKey(varName) == true;
    }
}
