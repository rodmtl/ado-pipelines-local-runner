using System.Text;
using System.Text.Json;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Utils;

namespace AdoPipelinesLocalRunner.Core.Reporting;

public class ErrorReporter : IErrorReporter
{
    private readonly bool _useColors;

    public ErrorReporter()
    {
        // Enable colors by default for console output
        _useColors = true;
    }
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
                FileNameSuggestion = MakeSuggestion(sourcePath, "json")
            },
            OutputFormat.Sarif => new ReportOutput
            {
                Content = BuildSarif(sourcePath, errors, warnings),
                Format = OutputFormat.Sarif,
                FileNameSuggestion = MakeSuggestion(sourcePath, "sarif")
            },
            OutputFormat.Markdown => new ReportOutput
            {
                Content = BuildMarkdown(sourcePath, errors, warnings, categories),
                Format = OutputFormat.Markdown,
                FileNameSuggestion = MakeSuggestion(sourcePath, "md")
            },
            _ => new ReportOutput
            {
                Content = BuildText(sourcePath, errors, warnings, categories, _useColors),
                Format = OutputFormat.Text,
                FileNameSuggestion = MakeSuggestion(sourcePath, "txt")
            }
        };
    }

    private static string MakeSuggestion(string sourcePath, string ext)
    {
        var baseName = string.IsNullOrWhiteSpace(sourcePath)
            ? "validation"
            : System.IO.Path.GetFileNameWithoutExtension(sourcePath);
        return $"{baseName}-report.{ext}";
    }

    private static string BuildText(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings, Dictionary<string, (int errors, int warnings)> categories, bool useColors)
    {
        var sb = new StringBuilder();
        
        // Write source and counts
        sb.AppendLine($"Source: {sourcePath ?? "<unknown>"}");
        
        if (useColors && errors.Count == 0 && warnings.Count == 0)
        {
            sb.Append("Status: ");
            AppendColored(sb, "✓ Success", "Green");
            sb.AppendLine($" - Errors: {errors.Count}, Warnings: {warnings.Count}");
        }
        else
        {
            sb.Append("Status: ");
            if (errors.Count > 0)
            {
                AppendColored(sb, $"✗ Failed", "Red");
                sb.AppendLine($" - Errors: {errors.Count}, Warnings: {warnings.Count}");
            }
            else if (warnings.Count > 0)
            {
                AppendColored(sb, "⚠ Warning", "Yellow");
                sb.AppendLine($" - Errors: {errors.Count}, Warnings: {warnings.Count}");
            }
            else
            {
                sb.AppendLine($"Errors: {errors.Count}, Warnings: {warnings.Count}");
            }
        }
        
        if (categories.Count > 0)
        {
            sb.AppendLine("By Category:");
            foreach (var kvp in categories)
            {
                sb.AppendLine($"  - {kvp.Key}: errors={kvp.Value.errors}, warnings={kvp.Value.warnings}");
            }
        }
        
        if (errors.Count > 0)
        {
            sb.AppendLine();
            if (useColors)
            {
                AppendColored(sb, "ERRORS:", "Red");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("ERRORS:");
            }
            
            foreach (var e in errors)
            {
                if (useColors)
                {
                    sb.Append("  ");
                    AppendColored(sb, $"[{e.Code}]", "Red");
                    sb.AppendLine($" {e.Message}");
                }
                else
                {
                    sb.AppendLine($"  [{e.Code}] {e.Message}");
                }
                
                sb.AppendLine($"  Location: {e.Location?.FilePath}:{e.Location?.Line}:{e.Location?.Column ?? 1}");
                if (!string.IsNullOrWhiteSpace(e.Suggestion))
                {
                    if (useColors)
                    {
                        sb.Append("  ");
                        AppendColored(sb, "✓ Fix:", "Cyan");
                        sb.AppendLine($" {e.Suggestion}");
                    }
                    else
                    {
                        sb.AppendLine($"  ✓ Fix: {e.Suggestion}");
                    }
                }
            }
        }
        
        if (warnings.Count > 0)
        {
            sb.AppendLine();
            if (useColors)
            {
                AppendColored(sb, "WARNINGS:", "Yellow");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("WARNINGS:");
            }
            
            foreach (var w in warnings)
            {
                if (useColors)
                {
                    sb.Append("  ");
                    AppendColored(sb, $"[{w.Code}]", "Yellow");
                    sb.AppendLine($" {w.Message}");
                }
                else
                {
                    sb.AppendLine($"  [{w.Code}] {w.Message}");
                }
                
                sb.AppendLine($"  Location: {w.Location?.FilePath}:{w.Location?.Line}:{w.Location?.Column ?? 1}");
                if (!string.IsNullOrWhiteSpace(w.Suggestion))
                {
                    if (useColors)
                    {
                        sb.Append("  ");
                        AppendColored(sb, "✓ Fix:", "Cyan");
                        sb.AppendLine($" {w.Suggestion}");
                    }
                    else
                    {
                        sb.AppendLine($"  ✓ Fix: {w.Suggestion}");
                    }
                }
            }
        }
        
        return sb.ToString();
    }

    private static void AppendColored(StringBuilder sb, string text, string color)
    {
        // Use ANSI color codes for colored text in console
        var colorCode = color.ToLowerInvariant() switch
        {
            "red" => "\u001b[91m",
            "green" => "\u001b[92m",
            "yellow" => "\u001b[93m",
            "cyan" => "\u001b[96m",
            _ => ""
        };
        var resetCode = "\u001b[0m";
        sb.Append($"{colorCode}{text}{resetCode}");
    }

    private static string BuildMarkdown(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings, Dictionary<string, (int errors, int warnings)> categories)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Rapport de validation\n");
        sb.AppendLine($"**Source**: {sourcePath ?? "<inconnu>"}");
        sb.AppendLine($"**Erreurs**: {errors.Count} • **Avertissements**: {warnings.Count}\n");
        if (categories.Count > 0)
        {
            sb.AppendLine("## Par catégorie");
            foreach (var kvp in categories)
            {
                sb.AppendLine($"- **{kvp.Key}**: erreurs={kvp.Value.errors}, avertissements={kvp.Value.warnings}");
            }
        }
        if (errors.Count > 0)
        {
            sb.AppendLine("## Erreurs");
            foreach (var e in errors)
            {
                sb.AppendLine($"- **Code**: {e.Code} — {e.Message} (" +
                              $"{e.Location?.FilePath}:{e.Location?.Line})");
            }
        }
        if (warnings.Count > 0)
        {
            sb.AppendLine("## Avertissements");
            foreach (var w in warnings)
            {
                sb.AppendLine($"- **Code**: {w.Code} — {w.Message} (" +
                              $"{w.Location?.FilePath}:{w.Location?.Line})");
            }
        }
        return sb.ToString();
    }

    private static string BuildJson(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings, Dictionary<string, (int errors, int warnings)> categories)
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

    private static string BuildSarif(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings)
    {
        // Minimal SARIF v2.1.0 payload (not full schema-compliant, suitable for Phase 1 MVP)
        var runs = new[]
        {
            new
            {
                tool = new { driver = new { name = "azp-local-validator" } },
                results = errors.Select(e => new
                {
                    ruleId = e.Code,
                    level = "error",
                    message = new { text = e.Message },
                    locations = new[]
                    {
                        new
                        {
                            physicalLocation = new
                            {
                                artifactLocation = new { uri = e.Location?.FilePath ?? sourcePath },
                                region = new { startLine = e.Location?.Line ?? 0 }
                            }
                        }
                    }
                }).Concat(warnings.Select(w => new
                {
                    ruleId = w.Code,
                    level = "warning",
                    message = new { text = w.Message },
                    locations = new[]
                    {
                        new
                        {
                            physicalLocation = new
                            {
                                artifactLocation = new { uri = w.Location?.FilePath ?? sourcePath },
                                region = new { startLine = w.Location?.Line ?? 0 }
                            }
                        }
                    }
                }))
            }
        };

        var sarif = new Dictionary<string, object>
        {
            ["version"] = "2.1.0",
            ["$schema"] = "https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0.json",
            ["runs"] = runs
        };
        return JsonSerializer.Serialize(sarif, new JsonSerializerOptions { WriteIndented = true });
    }

    private static Dictionary<string, (int errors, int warnings)> AggregateCategories(IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings)
    {
        var dict = new Dictionary<string, (int errors, int warnings)>(StringComparer.OrdinalIgnoreCase);
        void Add(string category, bool isError)
        {
            var current = dict.TryGetValue(category, out var v) ? v : (0, 0);
            current = isError ? (current.Item1 + 1, current.Item2) : (current.Item1, current.Item2 + 1);
            dict[category] = current;
        }

        foreach (var e in errors)
        {
            Add(DeduceCategory(e.Code), true);
        }
        foreach (var w in warnings)
        {
            Add(DeduceCategory(w.Code), false);
        }
        return dict;
    }

    private static string DeduceCategory(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "Unknown";
        code = code.ToUpperInvariant();
        if (code.StartsWith("SYNTAX")) return "Syntax";
        if (code.StartsWith("SCHEMA")) return "Schema";
        if (code.StartsWith("TEMPLATE")) return "Template";
        if (code.StartsWith("VARIABLE")) return "Variable";
        return "General";
    }
}
