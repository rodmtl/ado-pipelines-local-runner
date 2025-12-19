using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Reflection;
using AdoPipelinesLocalRunner;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Configuration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Cli;

public class ProgramTests
{
    private static MethodInfo GetPrivate(string name) => typeof(Program).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

    [Fact]
    public void TryParseInlineVariables_ReturnsFalseAndDoesNotPopulateOnInvalidEntry()
    {
        var method = GetPrivate("TryParseInlineVariables");
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var success = (bool)method.Invoke(null, new object?[] { new[] { "INVALID" }, output })!;

        success.Should().BeFalse();
        output.Should().BeEmpty();
    }

    [Fact]
    public void TryParseInlineVariables_ParsesMultipleAndOverridesDuplicates()
    {
        var method = GetPrivate("TryParseInlineVariables");
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var success = (bool)method.Invoke(null, new object?[] { new[] { "FOO=bar", "foo=baz" }, output })!;

        success.Should().BeTrue();
        output.Should().ContainKey("FOO").WhoseValue.Should().Be("baz");
    }

    [Fact]
    public async Task TrySaveReportAsync_WritesFileWithoutChangingExitCode()
    {
        var method = GetPrivate("TrySaveReportAsync");
        var tempFile = Path.Combine(Path.GetTempPath(), $"azp-log-{Guid.NewGuid():N}.txt");
        var ctx = CreateInvocationContext();

        await (Task)method.Invoke(null, new object?[] { ctx, tempFile, "hello" })!;

        File.ReadAllText(tempFile).Should().Be("hello");
        ctx.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task TrySaveReportAsync_SetsExitCodeOnFailure()
    {
        var method = GetPrivate("TrySaveReportAsync");
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "log.txt");
        var ctx = CreateInvocationContext();

        await (Task)method.Invoke(null, new object?[] { ctx, invalidPath, "content" })!;

        ctx.ExitCode.Should().Be(3);
        File.Exists(invalidPath).Should().BeFalse();
    }

    [Fact]
    public void DisplayRootHelp_WritesUsageToConsole()
    {
        var method = GetPrivate("DisplayRootHelp");
        var writer = new StringWriter();
        var original = Console.Out;
        Console.SetOut(writer);

        try
        {
            method.Invoke(null, Array.Empty<object?>());
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = writer.ToString();
        output.Should().Contain("Usage: azp-local validate [OPTIONS]");
        output.Should().Contain("azp-local validate --pipeline");
    }

    [Fact]
    public void ConfigureServices_RegistersRequiredDependencies()
    {
        var method = GetPrivate("ConfigureServices");
        var services = new ServiceCollection();
        var previous = Environment.GetEnvironmentVariable("AZP_LOCAL_VERBOSITY");
        Environment.SetEnvironmentVariable("AZP_LOCAL_VERBOSITY", "detailed");

        try
        {
            method.Invoke(null, new object?[] { services });
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZP_LOCAL_VERBOSITY", previous);
        }

        var provider = services.BuildServiceProvider();

        provider.GetService<IErrorReporter>().Should().NotBeNull();
        provider.GetService<AdoPipelinesLocalRunner.Core.Orchestration.IValidationOrchestrator>().Should().NotBeNull();
        provider.GetService<IYamlParser>().Should().NotBeNull();
        provider.GetService<ISyntaxValidator>().Should().NotBeNull();
        provider.GetService<ISchemaManager>().Should().NotBeNull();
        provider.GetService<ITemplateResolver>().Should().NotBeNull();
        provider.GetService<IVariableProcessor>().Should().NotBeNull();
        provider.GetService<ILogger<AdoPipelinesLocalRunner.Core.Orchestration.ValidationOrchestrator>>().Should().NotBeNull();
    }

    private static InvocationContext CreateInvocationContext()
    {
        var root = new RootCommand("test-root");
        var parser = new CommandLineBuilder(root).Build();
        var parseResult = parser.Parse(Array.Empty<string>());
        return new InvocationContext(parseResult);
    }
}
