using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Core.Validators;

/// <summary>
/// Implementation of ISyntaxValidator.
/// Validates Azure DevOps pipeline YAML structure against syntax rules.
/// Applies SOLID principles with strategy pattern for rule application.
/// </summary>
public class SyntaxValidator : ISyntaxValidator
{
    private readonly List<IValidationRule> _rules;

    public SyntaxValidator()
    {
        _rules = new List<IValidationRule>();
        RegisterBuiltInRules();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(PipelineDocument document, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ValidateInternal(document), cancellationToken);
    }

    /// <inheritdoc />
    public void AddRule(IValidationRule rule)
    {
        if (rule != null && !_rules.Any(r => r.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase)))
        {
            _rules.Add(rule);
        }
    }

    /// <inheritdoc />
    public bool RemoveRule(string ruleName)
    {
        var rule = _rules.FirstOrDefault(r => r.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));
        if (rule != null)
        {
            _rules.Remove(rule);
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<IValidationRule> GetRules()
    {
        return _rules.AsReadOnly();
    }

    private ValidationResult ValidateInternal(PipelineDocument document)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        if (document == null)
        {
            errors.Add(new ValidationError
            {
                Code = "NULL_DOCUMENT",
                Message = "Pipeline document is null",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = "<unknown>",
                    Line = 0,
                    Column = 0
                }
            });

            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = new List<ValidationError>()
            };
        }

        // Create validation context
        var context = new ValidationContext
        {
            Document = document,
            SourceMap = new DefaultSourceMap(),
            CurrentPath = string.Empty,
            Parent = null
        };

        // Apply all rules
        foreach (var rule in _rules)
        {
            try
            {
                var ruleErrors = rule.Validate(document, context).ToList();
                foreach (var error in ruleErrors)
                {
                    if (error.Severity == Severity.Error)
                        errors.Add(error);
                    else if (error.Severity == Severity.Warning)
                        warnings.Add(error);
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError
                {
                    Code = "RULE_EXECUTION_ERROR",
                    Message = $"Error executing rule {rule.Name}: {ex.Message}",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = document.SourcePath ?? "<unknown>",
                        Line = 0,
                        Column = 0
                    }
                });
            }
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private void RegisterBuiltInRules()
    {
        _rules.Add(new NoConflictingStructureRule());
        _rules.Add(new RequiredWorkDefinitionRule());
        _rules.Add(new MissingTriggerRule());
        _rules.Add(new StageStructureRule());
        _rules.Add(new JobStructureRule());
        _rules.Add(new StepStructureRule());
    }
}

/// <summary>
/// Rule: Validates that pipeline has no conflicting structure definitions.
/// A pipeline cannot have both stages and jobs/steps at root level.
/// </summary>
internal class NoConflictingStructureRule : IValidationRule
{
    public string Name => "NO_CONFLICTING_STRUCTURE";
    public Severity Severity => Severity.Error;
    public string Description => "Ensures pipeline doesn't mix stages with jobs/steps at root level";

    public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
    {
        var doc = node as PipelineDocument;
        if (doc == null)
            return Enumerable.Empty<ValidationError>();

        var hasStages = doc.Stages != null && doc.Stages.Count > 0;
        var hasJobs = doc.Jobs != null && doc.Jobs.Count > 0;
        var hasSteps = doc.Steps != null && doc.Steps.Count > 0;

        var issues = new List<ValidationError>();

        if (hasStages && hasJobs)
        {
            issues.Add(new ValidationError
            {
                Code = "CONFLICTING_STRUCTURE_STAGES_JOBS",
                Message = "Pipeline cannot have both 'stages' and 'jobs' at root level",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = doc.SourcePath ?? "<unknown>",
                    Line = 1,
                    Column = 1
                }
            });
        }

        if (hasStages && hasSteps)
        {
            issues.Add(new ValidationError
            {
                Code = "CONFLICTING_STRUCTURE_STAGES_STEPS",
                Message = "Pipeline cannot have both 'stages' and 'steps' at root level",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = doc.SourcePath ?? "<unknown>",
                    Line = 1,
                    Column = 1
                }
            });
        }

        if (hasJobs && hasSteps)
        {
            issues.Add(new ValidationError
            {
                Code = "CONFLICTING_STRUCTURE_JOBS_STEPS",
                Message = "Pipeline cannot have both 'jobs' and 'steps' at root level",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = doc.SourcePath ?? "<unknown>",
                    Line = 1,
                    Column = 1
                }
            });
        }

        return issues;
    }
}

/// <summary>
/// Rule: Validates that pipeline has at least one work definition (stages, jobs, or steps).
/// </summary>
internal class RequiredWorkDefinitionRule : IValidationRule
{
    public string Name => "REQUIRED_WORK_DEFINITION";
    public Severity Severity => Severity.Error;
    public string Description => "Ensures pipeline has work to perform (stages, jobs, or steps)";

    public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
    {
        var doc = node as PipelineDocument;
        if (doc == null)
            return Enumerable.Empty<ValidationError>();

        var hasStages = doc.Stages != null && doc.Stages.Count > 0;
        var hasJobs = doc.Jobs != null && doc.Jobs.Count > 0;
        var hasSteps = doc.Steps != null && doc.Steps.Count > 0;

        if (!hasStages && !hasJobs && !hasSteps)
        {
            return new[]
            {
                new ValidationError
                {
                    Code = "NO_WORK_DEFINITION",
                    Message = "Pipeline must define at least one stage, job, or step",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = 1,
                        Column = 1
                    },
                    Suggestion = "Add 'stages:', 'jobs:', or 'steps:' section to the pipeline"
                }
            };
        }

        return Enumerable.Empty<ValidationError>();
    }
}

/// <summary>
/// Rule: Validates that pipeline has a trigger definition.
/// </summary>
internal class MissingTriggerRule : IValidationRule
{
    public string Name => "MISSING_TRIGGER_RULE";
    public Severity Severity => Severity.Warning;
    public string Description => "Warns if pipeline doesn't have a trigger definition";

    public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
    {
        var doc = node as PipelineDocument;
        if (doc == null)
            return Enumerable.Empty<ValidationError>();

        if (doc.Trigger == null)
        {
            return new[]
            {
                new ValidationError
                {
                    Code = "MISSING_TRIGGER",
                    Message = "Pipeline should define a 'trigger' for continuous integration",
                    Severity = Severity.Warning,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = 1,
                        Column = 1
                    },
                    Suggestion = "Add 'trigger:' section or use 'trigger: none' to disable automatic triggers"
                }
            };
        }

        return Enumerable.Empty<ValidationError>();
    }
}

/// <summary>
/// Helper static methods for validation.
/// </summary>
internal static class ValidationHelpers
{
    /// <summary>
    /// Check if an object has a property with a given name.
    /// </summary>
    public static bool HasProperty(object obj, string propertyName)
    {
        try
        {
            var type = obj?.GetType();
            if (type == null) return false;
            
            var property = type.GetProperty(propertyName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property != null)
            {
                var value = property.GetValue(obj);
                return value != null;
            }
            
            // Try as dictionary
            if (obj is System.Collections.IDictionary dict)
            {
                return dict.Keys.Cast<object>().Any(k => k?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) == true);
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Rule: Validates stage structure when stages are used.
/// </summary>
internal class StageStructureRule : IValidationRule
{
    public string Name => "STAGE_STRUCTURE_RULE";
    public Severity Severity => Severity.Error;
    public string Description => "Validates structure of pipeline stages";

    public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
    {
        var doc = node as PipelineDocument;
        if (doc?.Stages == null || doc.Stages.Count == 0)
            return Enumerable.Empty<ValidationError>();

        var issues = new List<ValidationError>();
        int lineNumber = 1;

        foreach (var stage in doc.Stages)
        {
            if (stage == null)
            {
                issues.Add(new ValidationError
                {
                    Code = "NULL_STAGE",
                    Message = "Stage cannot be null",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = lineNumber,
                        Column = 1
                    }
                });
                lineNumber++;
                continue;
            }

            bool hasDisplayName = ValidationHelpers.HasProperty(stage, "displayName");
            bool hasJobs = ValidationHelpers.HasProperty(stage, "jobs");

            // Stages require at least one piece of metadata or structure
            if (!hasDisplayName && !hasJobs)
            {
                issues.Add(new ValidationError
                {
                    Code = "INVALID_STAGE_STRUCTURE",
                    Message = "Stage must have at least a displayName or jobs defined",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = lineNumber,
                        Column = 1
                    },
                    Suggestion = "Add 'displayName' or 'jobs' to the stage definition"
                });
            }

            lineNumber++;
        }

        return issues;
    }
}

/// <summary>
/// Rule: Validates job structure when jobs are used.
/// </summary>
internal class JobStructureRule : IValidationRule
{
    public string Name => "JOB_STRUCTURE_RULE";
    public Severity Severity => Severity.Error;
    public string Description => "Validates structure of pipeline jobs";

    public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
    {
        var doc = node as PipelineDocument;
        if (doc?.Jobs == null || doc.Jobs.Count == 0)
            return Enumerable.Empty<ValidationError>();

        var issues = new List<ValidationError>();
        int lineNumber = 1;

        foreach (var job in doc.Jobs)
        {
            if (job == null)
            {
                issues.Add(new ValidationError
                {
                    Code = "NULL_JOB",
                    Message = "Job cannot be null",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = lineNumber,
                        Column = 1
                    }
                });
                lineNumber++;
                continue;
            }

            bool hasDisplayName = ValidationHelpers.HasProperty(job, "displayName");
            bool hasSteps = ValidationHelpers.HasProperty(job, "steps");

            // Jobs require at least displayName or steps
            if (!hasDisplayName && !hasSteps)
            {
                issues.Add(new ValidationError
                {
                    Code = "INVALID_JOB_STRUCTURE",
                    Message = "Job must have at least a displayName or steps defined",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = lineNumber,
                        Column = 1
                    },
                    Suggestion = "Add 'displayName' or 'steps' to the job definition"
                });
            }

            lineNumber++;
        }

        return issues;
    }
}

/// <summary>
/// Rule: Validates step structure when steps are used.
/// </summary>
internal class StepStructureRule : IValidationRule
{
    public string Name => "STEP_STRUCTURE_RULE";
    public Severity Severity => Severity.Error;
    public string Description => "Validates structure of pipeline steps";

    public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
    {
        var doc = node as PipelineDocument;
        if (doc?.Steps == null || doc.Steps.Count == 0)
            return Enumerable.Empty<ValidationError>();

        var issues = new List<ValidationError>();
        int lineNumber = 1;

        foreach (var step in doc.Steps)
        {
            if (step == null)
            {
                issues.Add(new ValidationError
                {
                    Code = "NULL_STEP",
                    Message = "Step cannot be null",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = lineNumber,
                        Column = 1
                    }
                });
                lineNumber++;
                continue;
            }

            // Steps should have at least script, task, checkout, download, or publish
            var hasScript = ValidationHelpers.HasProperty(step, "script");
            var hasTask = ValidationHelpers.HasProperty(step, "task");
            var hasCheckout = ValidationHelpers.HasProperty(step, "checkout");
            var hasDownload = ValidationHelpers.HasProperty(step, "download");
            var hasPublish = ValidationHelpers.HasProperty(step, "publish");

            if (!hasScript && !hasTask && !hasCheckout && !hasDownload && !hasPublish)
            {
                issues.Add(new ValidationError
                {
                    Code = "STEP_NO_ACTION",
                    Message = "Step must define an action (script, task, checkout, download, or publish)",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = doc.SourcePath ?? "<unknown>",
                        Line = lineNumber,
                        Column = 1
                    }
                });
            }

            lineNumber++;
        }

        return issues;
    }
}

/// <summary>
/// Default implementation of ISourceMap for context creation.
/// </summary>
internal class DefaultSourceMap : ISourceMap
{
    public int GetLineNumber(string path)
    {
        return -1; // Default: not found
    }

    public SourceLocation GetOriginalLocation(int line)
    {
        return new SourceLocation
        {
            FilePath = "<unknown>",
            Line = line,
            Column = 1
        };
    }

    public IEnumerable<string> GetAllPaths()
    {
        return Enumerable.Empty<string>();
    }
}
