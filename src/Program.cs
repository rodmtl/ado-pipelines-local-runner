using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Reporting;

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

        var validateCmd = new Command("validate", "Validate an Azure DevOps pipeline YAML file");
        var pipelineOpt = new Option<string>(name: "--pipeline", description: "Path to the pipeline YAML file") { IsRequired = true };
        var basePathOpt = new Option<string?>(name: "--base-path", description: "Base path for resolving local templates");
        var schemaVerOpt = new Option<string?>(name: "--schema-version", description: "Schema version to validate against");
        var varsOpt = new Option<string[]>(name: "--vars", description: "Variable files to include") { Arity = ArgumentArity.ZeroOrMore };
        var strictOpt = new Option<bool>(name: "--strict", description: "Treat warnings as errors");
        var outputOpt = new Option<string>(name: "--output", description: "Output format: text|json|sarif|markdown", getDefaultValue: () => "text");

        validateCmd.AddOption(pipelineOpt);
        validateCmd.AddOption(basePathOpt);
        validateCmd.AddOption(schemaVerOpt);
        validateCmd.AddOption(varsOpt);
        validateCmd.AddOption(strictOpt);
        validateCmd.AddOption(outputOpt);

        validateCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var pipeline = ctx.ParseResult.GetValueForOption(pipelineOpt)!;
            var basePath = ctx.ParseResult.GetValueForOption(basePathOpt);
            var schemaVersion = ctx.ParseResult.GetValueForOption(schemaVerOpt);
            var vars = ctx.ParseResult.GetValueForOption(varsOpt) ?? Array.Empty<string>();
            var strict = ctx.ParseResult.GetValueForOption(strictOpt);
            var output = ctx.ParseResult.GetValueForOption(outputOpt) ?? "text";

            var orchestrator = serviceProvider.GetRequiredService<Core.Orchestration.IValidationOrchestrator>();
            var reporter = serviceProvider.GetRequiredService<Contracts.IErrorReporter>();
            var format = output.ToLowerInvariant() switch
            {
                "json" => Contracts.Configuration.OutputFormat.Json,
                "sarif" => Contracts.Configuration.OutputFormat.Sarif,
                "markdown" => Contracts.Configuration.OutputFormat.Markdown,
                _ => Contracts.Configuration.OutputFormat.Text
            };

            var req = new Contracts.Commands.ValidateRequest
            {
                Path = pipeline,
                BaseDirectory = basePath,
                SchemaVersion = schemaVersion,
                VariableFiles = vars,
                FailOnWarnings = strict,
                OutputFormat = Contracts.Commands.OutputFormat.Text
            };

            var resp = await orchestrator.ValidateAsync(req, CancellationToken.None);
            var report = reporter.GenerateReport(pipeline, resp.Details.AllErrors, resp.Details.AllWarnings, format);
            Console.WriteLine(report.Content);
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
            builder.SetMinimumLevel(LogLevel.Information);
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
