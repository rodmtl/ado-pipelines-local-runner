using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Reporting;
using AdoPipelinesLocalRunner.Utils;

namespace AdoPipelinesLocalRunner;

/// <summary>
/// Main entry point for the ADO Pipelines Local Runner CLI application.
/// </summary>
public class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        var rootCommand = BuildRootCommand(serviceProvider);
        return await rootCommand.InvokeAsync(args);
    }

    public static RootCommand BuildRootCommand(IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("Azure DevOps Pipelines Local Runner - Validate and test pipelines locally");
        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Azure DevOps Pipelines Local Runner");
            Console.WriteLine("Usage: azp-local validate [OPTIONS]");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  azp-local validate --pipeline azure-pipelines.yml");
            Console.WriteLine("  azp-local validate --pipeline build.yml --base-path ./ --output json");
            Console.WriteLine("  azp-local validate --pipeline ci.yml --var buildConfig=Release --strict");
            Console.WriteLine("\nUse 'validate --help' to see all available options");
        });

        var validateCmd = new Command("validate", "Validate an Azure DevOps pipeline YAML file");
        var pipelineOpt = new Option<string>(
            name: "--pipeline", 
            description: "Path to the pipeline YAML file to validate") 
        { IsRequired = true };
        var basePathOpt = new Option<string?>(
            name: "--base-path", 
            description: "Base directory path for resolving local template references (default: current directory)");
        var schemaVerOpt = new Option<string?>(
            name: "--schema-version", 
            description: "Azure DevOps schema version to validate against (e.g., '2023-01'). If not specified, uses latest");
        var varsOpt = new Option<string[]>(
            name: "--vars", 
            description: "Variable files to include in validation (YAML format)") 
        { Arity = ArgumentArity.ZeroOrMore };
        var inlineVarOpt = new Option<string[]>(
            name: "--var", 
            description: "Inline variable in key=value format (can be used multiple times)", 
            getDefaultValue: () => Array.Empty<string>()) 
        { Arity = ArgumentArity.ZeroOrMore };
        var strictOpt = new Option<bool>(
            name: "--strict", 
            description: "Treat all warnings as errors; exit with code 1 if warnings are found");
        var outputOpt = new Option<string>(
            name: "--output", 
            description: "Output format: text (default)|json|sarif|markdown", 
            getDefaultValue: () => "text");
        var allowUnresolvedOpt = new Option<bool>(
            name: "--allow-unresolved", 
            description: "Allow unresolved variables and report them as warnings instead of errors");
        var verbosityOpt = new Option<string>(
            name: "--verbosity", 
            description: "Logging verbosity level: quiet|minimal|normal|detailed (default: normal)", 
            getDefaultValue: () => "normal");
        var logFileOpt = new Option<string?>(
            name: "--log-file", 
            description: "Optional file path to save validation report");

        validateCmd.AddOption(pipelineOpt);
        validateCmd.AddOption(basePathOpt);
        validateCmd.AddOption(schemaVerOpt);
        validateCmd.AddOption(varsOpt);
        validateCmd.AddOption(inlineVarOpt);
        validateCmd.AddOption(strictOpt);
        validateCmd.AddOption(outputOpt);
        validateCmd.AddOption(allowUnresolvedOpt);
        validateCmd.AddOption(verbosityOpt);
        validateCmd.AddOption(logFileOpt);

        validateCmd.SetHandler(async (InvocationContext ctx) =>
        {
            // Get version info
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "1.0.0";

            // Show header
            ConsoleHelper.WriteHeader("Azure DevOps Pipelines Local Runner", version);

            var pipeline = ctx.ParseResult.GetValueForOption(pipelineOpt)!;
            var basePath = ctx.ParseResult.GetValueForOption(basePathOpt);
            var schemaVersion = ctx.ParseResult.GetValueForOption(schemaVerOpt);
            var vars = ctx.ParseResult.GetValueForOption(varsOpt) ?? Array.Empty<string>();
            var inlineVarsRaw = ctx.ParseResult.GetValueForOption(inlineVarOpt) ?? Array.Empty<string>();
            var strict = ctx.ParseResult.GetValueForOption(strictOpt);
            var output = ctx.ParseResult.GetValueForOption(outputOpt) ?? "text";
            var allowUnresolved = ctx.ParseResult.GetValueForOption(allowUnresolvedOpt);
            var verbosity = ctx.ParseResult.GetValueForOption(verbosityOpt) ?? "normal";
            var logFile = ctx.ParseResult.GetValueForOption(logFileOpt);

            var inlineVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in inlineVarsRaw)
            {
                var parts = entry.Split('=', 2);
                if (parts.Length != 2)
                {
                    ConsoleHelper.WriteError($"Invalid --var entry: {entry}. Expected key=value.");
                    ctx.ExitCode = 3;
                    return;
                }
                inlineVars[parts[0]] = parts[1];
            }

            var orchestrator = serviceProvider.GetRequiredService<Core.Orchestration.IValidationOrchestrator>();
            var reporter = serviceProvider.GetRequiredService<Contracts.IErrorReporter>();
            var format = output.ToLowerInvariant() switch
            {
                "json" => Contracts.Configuration.OutputFormat.Json,
                "sarif" => Contracts.Configuration.OutputFormat.Sarif,
                "markdown" => Contracts.Configuration.OutputFormat.Markdown,
                "text" => Contracts.Configuration.OutputFormat.Text,
                _ => Contracts.Configuration.OutputFormat.Text
            };

            var req = new Contracts.Commands.ValidateRequest
            {
                Path = pipeline,
                BaseDirectory = basePath,
                SchemaVersion = schemaVersion,
                VariableFiles = vars,
                InlineVariables = inlineVars,
                FailOnWarnings = strict,
                AllowUnresolvedVariables = allowUnresolved,
                OutputFormat = format switch
                {
                    Contracts.Configuration.OutputFormat.Json => Contracts.Commands.OutputFormat.Json,
                    // Map unsupported request formats to Text for orchestrator
                    Contracts.Configuration.OutputFormat.Sarif => Contracts.Commands.OutputFormat.Text,
                    Contracts.Configuration.OutputFormat.Markdown => Contracts.Commands.OutputFormat.Text,
                    _ => Contracts.Commands.OutputFormat.Text
                }
            };

            var resp = await orchestrator.ValidateAsync(req, CancellationToken.None);
            var report = reporter.GenerateReport(pipeline, resp.Details.AllErrors, resp.Details.AllWarnings, format);
            Console.WriteLine(report.Content);
            
            // Add success/failure message at the end
            Console.WriteLine();
            if (resp.Status == Contracts.Commands.ValidationStatus.Success)
            {
                ConsoleHelper.WriteSuccess("Pipeline validation completed successfully!");
            }
            else if (resp.Status == Contracts.Commands.ValidationStatus.SuccessWithWarnings)
            {
                if (strict)
                {
                    ConsoleHelper.WriteError("✗ Pipeline validation failed (warnings treated as errors in strict mode)");
                }
                else
                {
                    ConsoleHelper.WriteWarning("⚠ Pipeline validation completed with warnings");
                }
            }
            else if (resp.Status == Contracts.Commands.ValidationStatus.Failed)
            {
                ConsoleHelper.WriteError("✗ Pipeline validation failed");
            }
            
            if (!string.IsNullOrWhiteSpace(logFile))
            {
                try
                {
                    await File.WriteAllTextAsync(logFile!, report.Content);
                    ConsoleHelper.WriteInfo($"Report saved to: {logFile}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to write log file: {ex.Message}");
                    ctx.ExitCode = 3;
                    return;
                }
            }
            var exitCode = resp.Status switch
            {
                Contracts.Commands.ValidationStatus.Success => 0,
                Contracts.Commands.ValidationStatus.SuccessWithWarnings => strict ? 1 : 0,
                Contracts.Commands.ValidationStatus.Failed => 1,
                _ => 3
            };
            ctx.ExitCode = exitCode;
        });

        rootCommand.AddCommand(validateCmd);

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("ADO Pipelines Local Runner");
            Console.WriteLine("Use --help to see available commands");
        });

        return rootCommand;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            // Map verbosity from environment variable set by handler, default to Information
            var verbosity = Environment.GetEnvironmentVariable("AZP_LOCAL_VERBOSITY") ?? "normal";
            var level = verbosity.ToLowerInvariant() switch
            {
                "quiet" => LogLevel.Error,
                "minimal" => LogLevel.Warning,
                "normal" => LogLevel.Information,
                "detailed" => LogLevel.Debug,
                _ => LogLevel.Information
            };
            builder.SetMinimumLevel(level);
            // File logging not configured via logger; handler writes reports when requested.
        });

        // Register services
        services.AddSingleton<IErrorReporter, ErrorReporter>();
        services.AddSingleton<Core.Orchestration.IValidationOrchestrator, Core.Orchestration.ValidationOrchestrator>();

        // TODO: Register concrete implementations for parser/validator/schema/resolver/variables
        services.AddSingleton<IYamlParser, Core.Parsing.YamlParser>();
        services.AddSingleton<ISyntaxValidator, Core.Validators.SyntaxValidator>();
        services.AddSingleton<ISchemaManager, Core.Schema.SchemaManager>();
        services.AddSingleton<ITemplateResolver, Core.Templates.TemplateResolver>();
        services.AddSingleton<IVariableProcessor, Core.Variables.VariableProcessor>();
    }
}
