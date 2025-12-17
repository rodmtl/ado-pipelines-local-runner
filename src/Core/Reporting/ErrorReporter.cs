using System.Text;
using System.Text.Json;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Core.Reporting;

/// <summary>
/// Generates validation reports in multiple formats (text, JSON, SARIF, Markdown).
/// Implements IErrorReporter contract following single responsibility principle.
/// </summary>
public class ErrorReporter : IErrorReporter
{
    private readonly bool _useColors;

    public ErrorReporter()
    {
        _useColors = true;
    }
    /// <summary>
    /// Generates a formatted report containing validation errors and warnings.
    /// </summary>
    /// <param name="sourcePath">Path to the source file being validated.</param>
    /// <param name="errors">List of validation errors.</param>
    /// <param name="warnings">List of validation warnings.</param>
    /// <param name="format">Desired output format.</param>
    /// <returns>Report output with formatted content.</returns>
    public ReportOutput GenerateReport(
        string sourcePath,
        IReadOnlyList<ValidationError> errors,
        IReadOnlyList<ValidationError> warnings,
        OutputFormat format)
    {
        var categories = AggregateCategories(errors, warnings);
        return format switch
        {
            OutputFormat.Json => new ReportOutput
            {
                Content = BuildJson(sourcePath, errors, warnings, categories),
                Format = OutputFormat.Json,
                FileNameSuggestion = GenerateFileSuggestion(sourcePath, "json")
            },
            OutputFormat.Sarif => new ReportOutput
            {
                Content = BuildSarif(sourcePath, errors, warnings),
                Format = OutputFormat.Sarif,
                FileNameSuggestion = GenerateFileSuggestion(sourcePath, "sarif")
            },
            OutputFormat.Markdown => new ReportOutput
            {
                Content = BuildMarkdown(sourcePath, errors, warnings, categories),
                Format = OutputFormat.Markdown,
                FileNameSuggestion = GenerateFileSuggestion(sourcePath, "md")
            },
            _ => new ReportOutput
            {
                Content = BuildText(sourcePath, errors, warnings, categories, _useColors),
                Format = OutputFormat.Text,
                FileNameSuggestion = GenerateFileSuggestion(sourcePath, "txt")
            }
        };
    }

    /// <summary>
    /// Generates a suggested filename for the report.
    /// </summary>
    private static string GenerateFileSuggestion(string sourcePath, string extension)
    {
        var baseName = string.IsNullOrWhiteSpace(sourcePath)
            ? "validation"
            : Path.GetFileNameWithoutExtension(sourcePath);
        return $"{baseName}-report.{extension}";
    }

    /// <summary>
    /// Builds a human-readable text report with optional color support.
    /// </summary>
    private static string BuildText(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings, Dictionary<string, (int errorCount, int warningCount)> categories, bool useColors)
    {
        var sb = new StringBuilder();
        
        AppendTextHeader(sb, sourcePath, errors, warnings, useColors);
        AppendTextCategories(sb, categories);
        AppendTextErrors(sb, errors, useColors);
        AppendTextWarnings(sb, warnings, useColors);
        
        return sb.ToString();
    }

    /// <summary>
    /// Appends the header section to the text report.
    /// </summary>
    private static void AppendTextHeader(StringBuilder sb, string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings, bool useColors)
    {
        sb.AppendLine($"Source: {sourcePath ?? "<unknown>"}");
        
        if (useColors && errors.Count == 0 && warnings.Count == 0)
        {
            sb.Append("Status: ");
            AppendColored(sb, "✓ Success", AnsiColor.Green);
            sb.AppendLine($" - Errors: {errors.Count}, Warnings: {warnings.Count}");
        }
        else
        {
            sb.Append("Status: ");
            if (errors.Count > 0)
            {
                AppendColored(sb, "✗ Failed", AnsiColor.Red);
                sb.AppendLine($" - Errors: {errors.Count}, Warnings: {warnings.Count}");
            }
            else if (warnings.Count > 0)
            {
                AppendColored(sb, "⚠ Warning", AnsiColor.Yellow);
                sb.AppendLine($" - Errors: {errors.Count}, Warnings: {warnings.Count}");
            }
            else
            {
                sb.AppendLine($"Errors: {errors.Count}, Warnings: {warnings.Count}");
            }
        }
    }

    /// <summary>
    /// Appends the categories section to the text report.
    /// </summary>
    private static void AppendTextCategories(StringBuilder sb, Dictionary<string, (int errorCount, int warningCount)> categories)
    {
        if (categories.Count > 0)
        {
            sb.AppendLine("By Category:");
            foreach (var kvp in categories)
            {
                sb.AppendLine($"  - {kvp.Key}: errors={kvp.Value.errorCount}, warnings={kvp.Value.warningCount}");
            }
        }
    }

    /// <summary>
    /// Appends the errors section to the text report.
    /// </summary>
    private static void AppendTextErrors(StringBuilder sb, IReadOnlyList<ValidationError> errors, bool useColors)
    {
        if (errors.Count == 0)
            return;

        sb.AppendLine();
        if (useColors)
        {
            AppendColored(sb, "ERRORS:", AnsiColor.Red);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("ERRORS:");
        }
        
        foreach (var error in errors)
        {
            AppendTextIssue(sb, error, AnsiColor.Red, useColors);
        }
    }

    /// <summary>
    /// Appends the warnings section to the text report.
    /// </summary>
    private static void AppendTextWarnings(StringBuilder sb, IReadOnlyList<ValidationError> warnings, bool useColors)
    {
        if (warnings.Count == 0)
            return;

        sb.AppendLine();
        if (useColors)
        {
            AppendColored(sb, "WARNINGS:", AnsiColor.Yellow);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("WARNINGS:");
        }
        
        foreach (var warning in warnings)
        {
            AppendTextIssue(sb, warning, AnsiColor.Yellow, useColors);
        }
    }

    /// <summary>
    /// Appends a single issue (error or warning) to the text report.
    /// </summary>
    private static void AppendTextIssue(StringBuilder sb, ValidationError issue, string color, bool useColors)
    {
        if (useColors)
        {
            sb.Append("  ");
            AppendColored(sb, $"[{issue.Code}]", color);
            sb.AppendLine($" {issue.Message}");
        }
        else
        {
            sb.AppendLine($"  [{issue.Code}] {issue.Message}");
        }
        
        sb.AppendLine($"  Location: {issue.Location?.FilePath}:{issue.Location?.Line}:{issue.Location?.Column ?? 1}");
        if (!string.IsNullOrWhiteSpace(issue.Suggestion))
        {
            if (useColors)
            {
                sb.Append("  ");
                AppendColored(sb, "✓ Fix:", AnsiColor.Cyan);
                sb.AppendLine($" {issue.Suggestion}");
            }
            else
            {
                sb.AppendLine($"  ✓ Fix: {issue.Suggestion}");
            }
        }
    }

    /// <summary>
    /// Appends colored text to StringBuilder using ANSI escape codes.
    /// </summary>
    private static void AppendColored(StringBuilder sb, string text, string color)
    {
        var colorCode = color switch
        {
            AnsiColor.Red => "\u001b[91m",
            AnsiColor.Green => "\u001b[92m",
            AnsiColor.Yellow => "\u001b[93m",
            AnsiColor.Cyan => "\u001b[96m",
            _ => string.Empty
        };
        
        var resetCode = "\u001b[0m";
        sb.Append($"{colorCode}{text}{resetCode}");
    }

    /// <summary>
    /// Builds a Markdown-formatted report.
    /// </summary>
    private static string BuildMarkdown(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings, Dictionary<string, (int errorCount, int warningCount)> categories)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Rapport de validation\n");
        sb.AppendLine($"**Source**: {sourcePath ?? "<inconnu>"}");
        sb.AppendLine($"**Erreurs**: {errors.Count} • **Avertissements**: {warnings.Count}\n");
        
        if (categories.Count > 0)
        {
            sb.AppendLine("## Par catégorie");
            foreach (var kvp in categories)
            {
                sb.AppendLine($"- **{kvp.Key}**: erreurs={kvp.Value.errorCount}, avertissements={kvp.Value.warningCount}");
            }
        }
        
        if (errors.Count > 0)
        {
            sb.AppendLine("## Erreurs");
            foreach (var error in errors)
            {
                sb.AppendLine($"- **Code**: {error.Code} — {error.Message} (" +
                              $"{error.Location?.FilePath}:{error.Location?.Line})");
            }
        }
        
        if (warnings.Count > 0)
        {
            sb.AppendLine("## Avertissements");
            foreach (var warning in warnings)
            {
                sb.AppendLine($"- **Code**: {warning.Code} — {warning.Message} (" +
                              $"{warning.Location?.FilePath}:{warning.Location?.Line})");
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds a JSON-formatted report.
    /// </summary>
    private static string BuildJson(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings, Dictionary<string, (int errorCount, int warningCount)> categories)
    {
        var errorDetails = errors.Select(e => new
        {
            code = e.Code,
            message = e.Message,
            severity = e.Severity.ToString(),
            location = new
            {
                file = e.Location?.FilePath,
                line = e.Location?.Line,
                column = e.Location?.Column
            },
            suggestion = e.Suggestion
        }).ToList();

        var warningDetails = warnings.Select(w => new
        {
            code = w.Code,
            message = w.Message,
            severity = w.Severity.ToString(),
            location = new
            {
                file = w.Location?.FilePath,
                line = w.Location?.Line,
                column = w.Location?.Column
            },
            suggestion = w.Suggestion
        }).ToList();

        var payload = new
        {
            source = sourcePath,
            summary = new { errors = errors.Count, warnings = warnings.Count, categories = categories },
            errors = errorDetails,
            warnings = warningDetails
        };
        
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Builds a SARIF v2.1.0 formatted report for integration with analysis tools.
    /// </summary>
    private static string BuildSarif(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings)
    {
        var results = errors
            .Concat(warnings)
            .Select(issue => new
            {
                ruleId = issue.Code,
                level = issue.Severity == Severity.Error ? "error" : "warning",
                message = new { text = issue.Message },
                locations = new[]
                {
                    new
                    {
                        physicalLocation = new
                        {
                            artifactLocation = new { uri = issue.Location?.FilePath ?? sourcePath },
                            region = new { startLine = issue.Location?.Line ?? 0 }
                        }
                    }
                }
            })
            .ToList();

        var sarif = new Dictionary<string, object>
        {
            ["version"] = "2.1.0",
            ["$schema"] = "https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0.json",
            ["runs"] = new[]
            {
                new
                {
                    tool = new { driver = new { name = "azp-local-validator" } },
                    results = results
                }
            }
        };
        
        return JsonSerializer.Serialize(sarif, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Aggregates error and warning counts by category.
    /// </summary>
    private static Dictionary<string, (int errorCount, int warningCount)> AggregateCategories(IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings)
    {
        var categories = new Dictionary<string, (int errorCount, int warningCount)>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var error in errors)
        {
            var category = DeriveCategory(error.Code);
            var current = categories.TryGetValue(category, out var value) ? value : (errorCount: 0, warningCount: 0);
            categories[category] = (errorCount: current.errorCount + 1, warningCount: current.warningCount);
        }
        
        foreach (var warning in warnings)
        {
            var category = DeriveCategory(warning.Code);
            var current = categories.TryGetValue(category, out var value) ? value : (errorCount: 0, warningCount: 0);
            categories[category] = (errorCount: current.errorCount, warningCount: current.warningCount + 1);
        }
        
        return categories;
    }

    /// <summary>
    /// Derives a category name from an error code.
    /// </summary>
    private static string DeriveCategory(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "Unknown";

        code = code.ToUpperInvariant();
        
        return code switch
        {
            _ when code.StartsWith("SYNTAX") => "Syntax",
            _ when code.StartsWith("SCHEMA") => "Schema",
            _ when code.StartsWith("TEMPLATE") => "Template",
            _ when code.StartsWith("VARIABLE") => "Variable",
            _ => "General"
        };
    }
}

/// <summary>
/// ANSI color code constants for console output.
/// </summary>
internal static class AnsiColor
{
    public const string Red = "Red";
    public const string Green = "Green";
    public const string Yellow = "Yellow";
    public const string Cyan = "Cyan";
}
