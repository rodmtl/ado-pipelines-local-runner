using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdoPipelinesLocalRunner;

/// <summary>
/// Main entry point for the ADO Pipelines Local Runner CLI application.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Create root command
        var rootCommand = new RootCommand("Azure DevOps Pipelines Local Runner - Validate and test pipelines locally");

        // TODO: Add commands here (validate, etc.)
        // For now, just a placeholder

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("ADO Pipelines Local Runner");
            Console.WriteLine("Use --help to see available commands");
        });

        // Execute command
        return await rootCommand.InvokeAsync(args);
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
        // TODO: Register all service implementations
    }
}
