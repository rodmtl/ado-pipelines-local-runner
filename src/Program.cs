using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using AdoPipelinesLocalRunner.Core.Reporting;
using AdoPipelinesLocalRunner.Utils;
using ConfigOutputFormat = AdoPipelinesLocalRunner.Contracts.Configuration.OutputFormat;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;
using CommandOutputFormat = AdoPipelinesLocalRunner.Contracts.Commands.OutputFormat;

namespace AdoPipelinesLocalRunner;

/// <summary>
/// Main entry point for the ADO Pipelines Local Runner CLI application.
/// Handles command-line parsing, service configuration, and command execution.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        var rootCommand = BuildRootCommand(serviceProvider);
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Builds the root command with validate subcommand.
    /// </summary>
    public static RootCommand BuildRootCommand(IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("Azure DevOps Pipelines Local Runner - Validate and test pipelines locally");
        rootCommand.SetHandler(() => DisplayRootHelp());

        var validateCmd = CreateValidateCommand(serviceProvider);
        rootCommand.AddCommand(validateCmd);

        return rootCommand;
    }

    /// <summary>
    /// Displays root command help text.
    /// </summary>
    private static void DisplayRootHelp()
    {
        ConsoleHelper.WriteInfo("Azure DevOps Pipelines Local Runner");
        ConsoleHelper.WriteInfo("Usage: azp-local validate [OPTIONS]");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  azp-local validate --pipeline azure-pipelines.yml");
        Console.WriteLine("  azp-local validate --pipeline build.yml --base-path ./ --output json");
        Console.WriteLine("  azp-local validate --pipeline ci.yml --var buildConfig=Release --strict");
        Console.WriteLine("\nUse 'validate --help' to see all available options");
    }

    /// <summary>
    /// Creates the validate command with all options and handler.
    /// </summary>
    private static Command CreateValidateCommand(IServiceProvider serviceProvider)
    {
        var validateCmd = new Command("validate", "Validate an Azure DevOps pipeline YAML file");
        
        // Create and add all options
        var options = CreateValidateOptions();
        foreach (var option in options.Values)
        {
            validateCmd.AddOption(option);
        }

        // Set command handler
        validateCmd.SetHandler(async (ctx) => 
            await HandleValidateCommandAsync(ctx, serviceProvider, options));

        return validateCmd;
    }

    /// <summary>
    /// Creates all options for the validate command.
    /// </summary>
    private static Dictionary<string, Option> CreateValidateOptions()
    {
        return new()
        {
            ["pipeline"] = new Option<string>(
                name: "--pipeline", 
                description: "Path to the pipeline YAML file to validate") 
            { IsRequired = true },
            
            ["base-path"] = new Option<string?>(
                name: "--base-path", 
                description: "Base directory path for resolving local template references (default: current directory)"),
            
            ["schema-version"] = new Option<string?>(
                name: "--schema-version", 
                description: "Azure DevOps schema version to validate against (e.g., '2023-01'). If not specified, uses latest"),
            
            ["vars"] = new Option<string[]>(
                name: "--vars", 
                description: "Variable files to include in validation (YAML format)") 
            { Arity = ArgumentArity.ZeroOrMore },
            
            ["var"] = new Option<string[]>(
                name: "--var", 
                description: "Inline variable in key=value format (can be used multiple times)", 
                getDefaultValue: () => Array.Empty<string>()) 
            { Arity = ArgumentArity.ZeroOrMore },
            
            ["strict"] = new Option<bool>(
                name: "--strict", 
                description: "Treat all warnings as errors; exit with code 1 if warnings are found"),
            
            ["output"] = new Option<string>(
                name: "--output", 
                description: "Output format: text (default)|json|sarif|markdown", 
                getDefaultValue: () => "text"),
            
            ["allow-unresolved"] = new Option<bool>(
                name: "--allow-unresolved", 
                description: "Allow unresolved variables and report them as warnings instead of errors"),
            
            ["verbosity"] = new Option<string>(
                name: "--verbosity", 
                description: "Logging verbosity level: quiet|minimal|normal|detailed (default: normal)", 
                getDefaultValue: () => "normal"),
            
            ["log-file"] = new Option<string?>(
                name: "--log-file", 
                description: "Optional file path to save validation report")
        };
    }

    /// <summary>
    /// Handles the validate command execution.
    /// </summary>
    private static async Task HandleValidateCommandAsync(
        InvocationContext ctx, 
        IServiceProvider serviceProvider,
        Dictionary<string, Option> options)
    {
        var version = GetApplicationVersion();
        ConsoleHelper.WriteHeader("Azure DevOps Pipelines Local Runner", version);

        // Extract arguments from context
        var args = ExtractValidateArguments(ctx, options);
        
        // Parse inline variables
        if (!TryParseInlineVariables(args.InlineVarsRaw, args.InlineVars))
        {
            ConsoleHelper.WriteError("Error parsing inline variables");
            ctx.ExitCode = 3;
            return;
        }

        // Execute validation
        await ExecuteValidationAsync(ctx, serviceProvider, args);
    }

    /// <summary>
    /// Extracts and structures validate command arguments from context.
    /// </summary>
    private static ValidateCommandArgs ExtractValidateArguments(InvocationContext ctx, Dictionary<string, Option> options)
    {
        return new ValidateCommandArgs
        {
            Pipeline = ctx.ParseResult.GetValueForOption((Option<string>)options["pipeline"])!,
            BasePath = ctx.ParseResult.GetValueForOption((Option<string?>)options["base-path"]),
            SchemaVersion = ctx.ParseResult.GetValueForOption((Option<string?>)options["schema-version"]),
            VarFiles = ctx.ParseResult.GetValueForOption((Option<string[]>)options["vars"]) ?? Array.Empty<string>(),
            InlineVarsRaw = ctx.ParseResult.GetValueForOption((Option<string[]>)options["var"]) ?? Array.Empty<string>(),
            Strict = ctx.ParseResult.GetValueForOption((Option<bool>)options["strict"]),
            Output = ctx.ParseResult.GetValueForOption((Option<string>)options["output"]) ?? "text",
            AllowUnresolved = ctx.ParseResult.GetValueForOption((Option<bool>)options["allow-unresolved"]),
            Verbosity = ctx.ParseResult.GetValueForOption((Option<string>)options["verbosity"]) ?? "normal",
            LogFile = ctx.ParseResult.GetValueForOption((Option<string?>)options["log-file"]),
            InlineVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Attempts to parse inline variable arguments into key-value dictionary.
    /// </summary>
    private static bool TryParseInlineVariables(string[] rawVars, Dictionary<string, string> output)
    {
        foreach (var entry in rawVars)
        {
            var parts = entry.Split('=', 2);
            if (parts.Length != 2)
            {
                ConsoleHelper.WriteError($"Invalid --var entry: {entry}. Expected key=value.");
                return false;
            }
            output[parts[0]] = parts[1];
        }
        return true;
    }

    /// <summary>
    /// Executes the validation process and displays results.
    /// </summary>
    private static async Task ExecuteValidationAsync(
        InvocationContext ctx,
        IServiceProvider serviceProvider,
        ValidateCommandArgs args)
    {
        var orchestrator = serviceProvider.GetRequiredService<Core.Orchestration.IValidationOrchestrator>();
        var reporter = serviceProvider.GetRequiredService<IErrorReporter>();
        
        var outputFormat = ParseOutputFormat(args.Output);
        var request = BuildValidateRequest(args, outputFormat);
        
        var response = await orchestrator.ValidateAsync(request, CancellationToken.None);
        var report = reporter.GenerateReport(args.Pipeline, response.Details.AllErrors, response.Details.AllWarnings, outputFormat);
        
        Console.WriteLine(report.Content);
        Console.WriteLine();
        
        DisplayValidationResult(response.Status, args.Strict);
        
        if (!string.IsNullOrWhiteSpace(args.LogFile))
        {
            await TrySaveReportAsync(ctx, args.LogFile, report.Content);
        }
        
        ctx.ExitCode = DetermineExitCode(response.Status, args.Strict);
    }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    private static string GetApplicationVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "1.0.0";
    }

    /// <summary>
    /// Parses string output format to OutputFormat enum.
    /// </summary>
    private static ConfigOutputFormat ParseOutputFormat(string output)
    {
        return output.ToLowerInvariant() switch
        {
            "json" => ConfigOutputFormat.Json,
            "sarif" => ConfigOutputFormat.Sarif,
            "markdown" => ConfigOutputFormat.Markdown,
            _ => ConfigOutputFormat.Text
        };
    }

    /// <summary>
    /// Builds a ValidateRequest from command arguments.
    /// </summary>
    private static ValidateRequest BuildValidateRequest(ValidateCommandArgs args, ConfigOutputFormat outputFormat)
    {
        var requestOutputFormat = outputFormat switch
        {
            ConfigOutputFormat.Json => CommandOutputFormat.Json,
            _ => CommandOutputFormat.Text
        };

        return new ValidateRequest
        {
            Path = args.Pipeline,
            BaseDirectory = args.BasePath,
            SchemaVersion = args.SchemaVersion,
            VariableFiles = args.VarFiles,
            InlineVariables = args.InlineVars,
            FailOnWarnings = args.Strict,
            AllowUnresolvedVariables = args.AllowUnresolved,
            OutputFormat = requestOutputFormat
        };
    }

    /// <summary>
    /// Displays validation result message.
    /// </summary>
    private static void DisplayValidationResult(ValidationStatus status, bool strict)
    {
        switch (status)
        {
            case ValidationStatus.Success:
                ConsoleHelper.WriteSuccess("Pipeline validation completed successfully!");
                break;
                
            case ValidationStatus.SuccessWithWarnings:
                if (strict)
                    ConsoleHelper.WriteError("✗ Pipeline validation failed (warnings treated as errors in strict mode)");
                else
                    ConsoleHelper.WriteWarning("⚠ Pipeline validation completed with warnings");
                break;
                
            case ValidationStatus.Failed:
                ConsoleHelper.WriteError("✗ Pipeline validation failed");
                break;
        }
    }

    /// <summary>
    /// Attempts to save report to file.
    /// </summary>
    private static async Task TrySaveReportAsync(InvocationContext ctx, string logFile, string content)
    {
        try
        {
            await File.WriteAllTextAsync(logFile, content);
            ConsoleHelper.WriteInfo($"Report saved to: {logFile}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Failed to write log file: {ex.Message}");
            ctx.ExitCode = 3;
        }
    }

    /// <summary>
    /// Determines the exit code based on validation status.
    /// </summary>
    private static int DetermineExitCode(ValidationStatus status, bool strict)
    {
        return status switch
        {
            ValidationStatus.Success => 0,
            ValidationStatus.SuccessWithWarnings => strict ? 1 : 0,
            ValidationStatus.Failed => 1,
            _ => 3
        };
    }

    /// <summary>
    /// Configures dependency injection container with all required services.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            var verbosity = Environment.GetEnvironmentVariable("AZP_LOCAL_VERBOSITY") ?? "normal";
            var level = verbosity.ToLowerInvariant() switch
            {
                "quiet" => MsLogLevel.Error,
                "minimal" => MsLogLevel.Warning,
                "normal" => MsLogLevel.Information,
                "detailed" => MsLogLevel.Debug,
                _ => MsLogLevel.Information
            };
            builder.SetMinimumLevel(level);
        });

        services.AddSingleton<IErrorReporter, ErrorReporter>();
        services.AddSingleton<Core.Orchestration.IValidationOrchestrator, Core.Orchestration.ValidationOrchestrator>();
        services.AddSingleton<IYamlParser, Core.Parsing.YamlParser>();
        services.AddSingleton<ISyntaxValidator, Core.Validators.SyntaxValidator>();
        services.AddSingleton<ISchemaManager, Core.Schema.SchemaManager>();
        services.AddSingleton<ITemplateResolver, Core.Templates.TemplateResolver>();
        services.AddSingleton<IVariableProcessor, Core.Variables.VariableProcessor>();
    }
}

/// <summary>
/// Data structure for validate command arguments.
/// </summary>
internal class ValidateCommandArgs
{
    public required string Pipeline { get; set; }
    public string? BasePath { get; set; }
    public string? SchemaVersion { get; set; }
    public string[] VarFiles { get; set; } = Array.Empty<string>();
    public string[] InlineVarsRaw { get; set; } = Array.Empty<string>();
    public bool Strict { get; set; }
    public string Output { get; set; } = "text";
    public bool AllowUnresolved { get; set; }
    public string Verbosity { get; set; } = "normal";
    public string? LogFile { get; set; }
    public Dictionary<string, string> InlineVars { get; set; } = new();
}
