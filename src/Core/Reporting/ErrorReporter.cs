using System.Text;
using System.Text.Json;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Core.Reporting;

public class ErrorReporter : IErrorReporter
{
    public ReportOutput GenerateReport(
        string sourcePath,
        IReadOnlyList<ValidationError> errors,
        IReadOnlyList<ValidationError> warnings,
        OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Json => new ReportOutput
            {
                Content = BuildJson(sourcePath, errors, warnings),
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
                Content = BuildMarkdown(sourcePath, errors, warnings),
                Format = OutputFormat.Markdown,
                FileNameSuggestion = MakeSuggestion(sourcePath, "md")
            },
            _ => new ReportOutput
            {
                Content = BuildText(sourcePath, errors, warnings),
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

    private static string BuildText(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Source: {sourcePath ?? "<unknown>"}");
        sb.AppendLine($"Errors: {errors.Count}, Warnings: {warnings.Count}");
        if (errors.Count > 0)
        {
            sb.AppendLine("Errors:");
            foreach (var e in errors)
            {
                sb.AppendLine($"- [{e.Code}] {e.Message} ({e.Location?.FilePath}:{e.Location?.Line})");
            }
        }
        if (warnings.Count > 0)
        {
            sb.AppendLine("Warnings:");
            foreach (var w in warnings)
            {
                sb.AppendLine($"- [{w.Code}] {w.Message} ({w.Location?.FilePath}:{w.Location?.Line})");
            }
        }
        return sb.ToString();
    }

    private static string BuildMarkdown(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Rapport de validation\n");
        sb.AppendLine($"**Source**: {sourcePath ?? "<inconnu>"}");
        sb.AppendLine($"**Erreurs**: {errors.Count} • **Avertissements**: {warnings.Count}\n");
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

    private static string BuildJson(string sourcePath, IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationError> warnings)
    {
        var payload = new
        {
            source = sourcePath,
            summary = new { errors = errors.Count, warnings = warnings.Count },
            errors,
            warnings
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
}
