using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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


        /// <summary>
        /// Loads and merges variables from files and inline sources into a single dictionary.
        /// </summary>
        /// <param name="files">Variable file paths to load</param>
        /// <param name="inline">Inline variable key-value pairs to merge</param>
        /// <param name="baseDir">Base directory for relative file paths</param>
        /// <param name="errors">Error collection to append any loading errors to</param>
        /// <returns>Dictionary of all loaded variables (files merged with inline)</returns>
        private IReadOnlyDictionary<string, object> LoadVariables(IEnumerable<string> files, IReadOnlyDictionary<string, string>? inline, string? baseDir, List<ValidationError> errors)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Load variables from files
            LoadVariablesFromFiles(files, baseDir, result, errors);

            // Merge inline variables
            MergeInlineVariables(inline, result);

            return result;
        }

        /// <summary>
        /// Loads variables from YAML files and adds them to the result dictionary.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Handles only file loading logic, separated from inline variable handling.
        /// </remarks>
        private void LoadVariablesFromFiles(IEnumerable<string> files, string? baseDir, Dictionary<string, object> result, List<ValidationError> errors)
        {
            var deserializer = CreateYamlDeserializer();

            foreach (var file in files)
            {
                var path = ResolveVariableFilePath(file, baseDir);

                if (!TryLoadVariableFile(path, deserializer, result, errors))
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Creates a YAML deserializer configured for variable file parsing.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Isolates deserializer configuration for easy testing/modification.
        /// </remarks>
        private IDeserializer CreateYamlDeserializer() =>
            new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

        /// <summary>
        /// Resolves a variable file path, handling relative paths based on the base directory.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Pure path resolution logic, no side effects.
        /// </remarks>
        private string ResolveVariableFilePath(string file, string? baseDir) =>
            baseDir != null && !Path.IsPathRooted(file) ? Path.Combine(baseDir, file) : file;

        /// <summary>
        /// Attempts to load a single variable file and merge its contents into the result dictionary.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Handles loading logic for a single file with error handling.
        /// </remarks>
        private bool TryLoadVariableFile(string path, IDeserializer deserializer, Dictionary<string, object> result, List<ValidationError> errors)
        {
            if (!File.Exists(path))
            {
                errors.Add(CreateVariableFileNotFoundError(path));
                return false;
            }

            try
            {
                var content = File.ReadAllText(path);
                var parsed = deserializer.Deserialize<Dictionary<string, object>>(content) ?? new Dictionary<string, object>();
                MergeVariableDictionary(parsed, result);
                return true;
            }
            catch (Exception ex)
            {
                errors.Add(CreateVariableFileInvalidError(path, ex));
                return false;
            }
        }

        /// <summary>
        /// Merges variables from a source dictionary into a result dictionary.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Pure dictionary merging logic, reusable for multiple merge scenarios.
        /// </remarks>
        private void MergeVariableDictionary(Dictionary<string, object> source, Dictionary<string, object> result)
        {
            foreach (var kvp in source)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Merges inline variables into the result dictionary.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Handles only inline variable merging.
        /// </remarks>
        private void MergeInlineVariables(IReadOnlyDictionary<string, string>? inline, Dictionary<string, object> result)
        {
            if (inline == null)
            {
                return;
            }

            foreach (var kvp in inline)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Creates a validation error for a missing variable file.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Error object construction, isolated for consistency.
        /// </remarks>
        private ValidationError CreateVariableFileNotFoundError(string path) =>
            new ValidationError
            {
                Code = "VARIABLE_FILE_NOT_FOUND",
                Message = $"Variable file not found: {path}",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = path, Line = 0, Column = 0 }
            };

        /// <summary>
        /// Creates a validation error for an invalid variable file.
        /// </summary>
        /// <remarks>
        /// Single Responsibility: Error object construction with exception context.
        /// </remarks>
        private ValidationError CreateVariableFileInvalidError(string path, Exception ex) =>
            new ValidationError
            {
                Code = "VARIABLE_FILE_INVALID",
                Message = $"Failed to read variables from {path}: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = path, Line = 0, Column = 0 }
            };
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

    /// <summary>
    /// Validates an Azure Pipelines YAML file through multiple stages (syntax, schema, templates, variables).
    /// </summary>
    /// <param name="request">Validation request containing file path and validation options</param>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>Comprehensive validation response with errors, warnings, and metrics</returns>
    /// <remarks>
    /// Execution flow:
    /// 1. Parse YAML file
    /// 2. Run syntax validation
    /// 3. Optionally validate schema
    /// 4. Optionally resolve templates
    /// 5. Optionally process variables
    /// Each stage can add errors/warnings that contribute to final status determination.
    /// </remarks>
    public async Task<ValidateResponse> ValidateAsync(ValidateRequest request, CancellationToken ct)
    {
        var start = DateTimeOffset.UtcNow;
        var errorList = new List<ValidationError>();
        var warningList = new List<ValidationError>();
        PipelineDocument? doc = null;
        long parsingMs = 0, syntaxMs = 0, schemaMs = 0, tmplMs = 0, varMs = 0;
        ValidationResult? syntaxResult = null;
        SchemaValidationResult? schemaResult = null;
        TemplateExpansionResult? templateResult = null;
        VariableProcessingResult? variableResult = null;

        if (!File.Exists(request.Path))
        {
            errorList.Add(new ValidationError
            {
                Code = "PIPELINE_FILE_NOT_FOUND",
                Message = $"Pipeline file not found: {request.Path}",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = request.Path, Line = 0, Column = 0 },
                Suggestion = "Verify the --pipeline path or provide an absolute path."
            });

            var endMissing = DateTimeOffset.UtcNow;
            var metricsMissing = new ProcessingMetrics
            {
                StartTime = start,
                EndTime = endMissing,
                TotalTimeMs = (long)(endMissing - start).TotalMilliseconds,
                ParsingTimeMs = parsingMs,
                SyntaxValidationTimeMs = syntaxMs,
                SchemaValidationTimeMs = schemaMs,
                TemplateResolutionTimeMs = tmplMs,
                VariableProcessingTimeMs = varMs
            };

            return new ValidateResponse
            {
                Status = ValidationStatus.Failed,
                Summary = new ValidationSummary
                {
                    FilesValidated = 0,
                    ErrorCount = errorList.Count,
                    WarningCount = warningList.Count,
                    InfoCount = 0,
                    TemplatesResolved = 0,
                    VariablesResolved = 0
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
                Metrics = metricsMissing,
                ValidatedDocument = null
            };
        }

        try
        {
            _logger.LogInformation("Parsing YAML: {Path}", request.Path);
            var parseStart = DateTimeOffset.UtcNow;
            var parseResult = await _parser.ParseFileAsync<PipelineDocument>(request.Path, ct);
            parsingMs = (long)(DateTimeOffset.UtcNow - parseStart).TotalMilliseconds;
            doc = parseResult.Data ?? new PipelineDocument { SourcePath = request.Path, RawContent = null };

            if (parseResult.Errors?.Any() == true)
            {
                errorList.AddRange(parseResult.Errors.Select(e => new ValidationError
                {
                    Code = e.Code,
                    Message = e.Message,
                    Severity = e.Severity,
                    Location = e.Location,
                    RelatedLocations = e.RelatedLocations,
                    Suggestion = e.Suggestion
                }));

                if (!parseResult.Success)
                {
                    var endParse = DateTimeOffset.UtcNow;
                    var metricsParse = new ProcessingMetrics
                    {
                        StartTime = start,
                        EndTime = endParse,
                        TotalTimeMs = (long)(endParse - start).TotalMilliseconds,
                        ParsingTimeMs = parsingMs,
                        SyntaxValidationTimeMs = syntaxMs,
                        SchemaValidationTimeMs = schemaMs,
                        TemplateResolutionTimeMs = tmplMs,
                        VariableProcessingTimeMs = varMs
                    };

                    return new ValidateResponse
                    {
                        Status = ValidationStatus.Failed,
                        Summary = new ValidationSummary
                        {
                            FilesValidated = 1,
                            ErrorCount = errorList.Count,
                            WarningCount = warningList.Count,
                            InfoCount = 0,
                            TemplatesResolved = 0,
                            VariablesResolved = 0
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
                        Metrics = metricsParse,
                        ValidatedDocument = doc
                    };
                }
            }

            _logger.LogInformation("Running syntax validation");
            var syntaxStart = DateTimeOffset.UtcNow;
            syntaxResult = await _syntaxValidator.ValidateAsync(doc, ct);
            syntaxMs = (long)(DateTimeOffset.UtcNow - syntaxStart).TotalMilliseconds;
            errorList.AddRange(syntaxResult.Errors);
            warningList.AddRange(syntaxResult.Warnings);

            if (request.ValidateSchema)
            {
                _logger.LogInformation("Loading schema");
                var schemaStart = DateTimeOffset.UtcNow;
                schemaResult = await _schemaManager.ValidateAsync(doc, request.SchemaVersion, ct);
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
                templateResult = texp;
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
                variableResult = vresult;
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
                TemplatesResolved = templateResult?.Success == true ? 1 : 0,
                VariablesResolved = variableResult?.Success == true ? 1 : 0
            },
            Details = new ValidationDetails
            {
                SyntaxValidation = syntaxResult,
                SchemaValidation = schemaResult,
                TemplateResolution = templateResult,
                VariableProcessing = variableResult,
                AllErrors = errorList,
                AllWarnings = warningList
            },
            Metrics = metrics,
            ValidatedDocument = doc
        };
    }
}
