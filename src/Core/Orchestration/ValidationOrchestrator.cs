using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using System.Linq;

namespace AdoPipelinesLocalRunner.Core.Orchestration;

public interface IValidationOrchestrator
{
    Task<ValidateResponse> ValidateAsync(ValidateRequest request, CancellationToken ct);
}

public class ValidationOrchestrator : IValidationOrchestrator
{
    private readonly IYamlParser _parser;
    private readonly ISyntaxValidator _syntaxValidator;
    private readonly ISchemaManager _schemaManager;
    private readonly ITemplateResolver _templateResolver;
    private readonly IVariableProcessor _variableProcessor;
    private readonly IErrorReporter _errorReporter;
    private readonly ILogger<ValidationOrchestrator> _logger;


        private IReadOnlyDictionary<string, object> LoadVariables(IEnumerable<string> files, IReadOnlyDictionary<string, string>? inline, string? baseDir, List<ValidationError> errors)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                var path = baseDir != null && !Path.IsPathRooted(file) ? Path.Combine(baseDir, file) : file;
                if (!File.Exists(path))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "VARIABLE_FILE_NOT_FOUND",
                        Message = $"Variable file not found: {path}",
                        Severity = Severity.Error,
                        Location = new SourceLocation { FilePath = path, Line = 0, Column = 0 }
                    });
                    continue;
                }

                try
                {
                    var content = File.ReadAllText(path);
                    var parsed = deserializer.Deserialize<Dictionary<string, object>>(content) ?? new Dictionary<string, object>();
                    foreach (var kvp in parsed)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "VARIABLE_FILE_INVALID",
                        Message = $"Failed to read variables from {path}: {ex.Message}",
                        Severity = Severity.Error,
                        Location = new SourceLocation { FilePath = path, Line = 0, Column = 0 }
                    });
                }
            }

            if (inline != null)
            {
                foreach (var kvp in inline)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    public ValidationOrchestrator(
        IYamlParser parser,
        ISyntaxValidator syntaxValidator,
        ISchemaManager schemaManager,
        ITemplateResolver templateResolver,
        IVariableProcessor variableProcessor,
        IErrorReporter errorReporter,
        ILogger<ValidationOrchestrator> logger)
    {
        _parser = parser;
        _syntaxValidator = syntaxValidator;
        _schemaManager = schemaManager;
        _templateResolver = templateResolver;
        _variableProcessor = variableProcessor;
        _errorReporter = errorReporter;
        _logger = logger;
    }

    public async Task<ValidateResponse> ValidateAsync(ValidateRequest request, CancellationToken ct)
    {
        var start = DateTimeOffset.UtcNow;
        var errorList = new List<ValidationError>();
        var warningList = new List<ValidationError>();
        PipelineDocument? doc = null;
        long parsingMs = 0, syntaxMs = 0, schemaMs = 0, tmplMs = 0, varMs = 0;

        try
        {
            _logger.LogInformation("Parsing YAML: {Path}", request.Path);
            var parseStart = DateTimeOffset.UtcNow;
            var parseResult = await _parser.ParseFileAsync<PipelineDocument>(request.Path, ct);
            parsingMs = (long)(DateTimeOffset.UtcNow - parseStart).TotalMilliseconds;
            doc = parseResult.Data ?? new PipelineDocument { SourcePath = request.Path, RawContent = null };

            _logger.LogInformation("Running syntax validation");
            var syntaxStart = DateTimeOffset.UtcNow;
            var syntaxResult = await _syntaxValidator.ValidateAsync(doc, ct);
            syntaxMs = (long)(DateTimeOffset.UtcNow - syntaxStart).TotalMilliseconds;
            errorList.AddRange(syntaxResult.Errors);
            warningList.AddRange(syntaxResult.Warnings);

            if (request.ValidateSchema)
            {
                _logger.LogInformation("Loading schema");
                var schemaStart = DateTimeOffset.UtcNow;
                var schemaResult = await _schemaManager.ValidateAsync(doc, request.SchemaVersion, ct);
                schemaMs = (long)(DateTimeOffset.UtcNow - schemaStart).TotalMilliseconds;
                foreach (var si in schemaResult.Errors)
                {
                    if (si.Severity == Severity.Error || si.Severity == Severity.Critical)
                        errorList.Add(si);
                    else if (si.Severity == Severity.Warning)
                        warningList.Add(si);
                }
            }

            if (request.ValidateTemplates && request.BaseDirectory is not null)
            {
                _logger.LogInformation("Resolving templates from {Base}", request.BaseDirectory);
                var tmplStart = DateTimeOffset.UtcNow;
                var tctx = new TemplateResolutionContext { BaseDirectory = request.BaseDirectory!, MaxDepth = request.MaxTemplateDepth };
                var texp = await _templateResolver.ExpandAsync(doc, tctx, ct);
                tmplMs = (long)(DateTimeOffset.UtcNow - tmplStart).TotalMilliseconds;
                doc = texp.ExpandedDocument ?? doc;
                errorList.AddRange(texp.Errors);
            }

            if (request.ValidateVariables)
            {
                _logger.LogInformation("Processing variables");
                var varStart = DateTimeOffset.UtcNow;
                var pipelineVars = LoadVariables(request.VariableFiles ?? Array.Empty<string>(), request.InlineVariables, request.BaseDirectory, errorList);
                var vctx = new VariableContext
                {
                    SystemVariables = new Dictionary<string, object>(),
                    PipelineVariables = pipelineVars,
                    VariableGroups = request.MockVariableGroups,
                    EnvironmentVariables = null,
                    Parameters = null,
                    FailOnUnresolved = !request.AllowUnresolvedVariables,
                    Scope = VariableScope.Pipeline
                };
                var vresult = await _variableProcessor.ProcessAsync(doc, vctx, ct);
                varMs = (long)(DateTimeOffset.UtcNow - varStart).TotalMilliseconds;
                doc = vresult.ProcessedDocument ?? doc;
                errorList.AddRange(vresult.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation encountered an error");
            errorList.Add(new ValidationError
            {
                Code = "VALIDATION_ERROR",
                Message = ex.Message,
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = request.Path, Line = 0, Column = 0 }
            });
        }

        var end = DateTimeOffset.UtcNow;
        var metrics = new ProcessingMetrics
        {
            StartTime = start,
            EndTime = end,
            TotalTimeMs = (long)(end - start).TotalMilliseconds,
            ParsingTimeMs = parsingMs,
            SyntaxValidationTimeMs = syntaxMs,
            SchemaValidationTimeMs = schemaMs,
            TemplateResolutionTimeMs = tmplMs,
            VariableProcessingTimeMs = varMs
        };

        var report = _errorReporter.GenerateReport(
            request.Path,
            errorList,
            warningList,
            Contracts.Configuration.OutputFormat.Text);

        var status = (errorList.Count > 0)
            || (request.FailOnWarnings && warningList.Count > 0)
            ? ValidationStatus.Failed
            : (warningList.Count > 0 ? ValidationStatus.SuccessWithWarnings : ValidationStatus.Success);

        return new ValidateResponse
        {
            Status = status,
            Summary = new ValidationSummary
            {
                FilesValidated = 1,
                ErrorCount = errorList.Count,
                WarningCount = warningList.Count,
                InfoCount = 0,
                TemplatesResolved = metrics.TemplateResolutionTimeMs > 0 ? 1 : 0,
                VariablesResolved = metrics.VariableProcessingTimeMs > 0 ? 1 : 0
            },
            Details = new ValidationDetails
            {
                SyntaxValidation = null,
                SchemaValidation = null,
                TemplateResolution = null,
                VariableProcessing = null,
                AllErrors = errorList,
                AllWarnings = warningList
            },
            Metrics = metrics,
            ValidatedDocument = doc
        };
    }
}
