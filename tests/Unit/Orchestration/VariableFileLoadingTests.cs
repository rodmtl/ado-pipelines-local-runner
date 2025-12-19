using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Commands;
using AdoPipelinesLocalRunner.Core.Orchestration;
using AdoPipelinesLocalRunner.Core.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;
using OutputFormatConfig = AdoPipelinesLocalRunner.Contracts.Configuration.OutputFormat;

namespace AdoPipelinesLocalRunner.Tests.Unit.Orchestration;

public class VariableFileLoadingTests
{
    private static ValidationOrchestrator CreateOrchestratorReturningDoc(string rawYaml)
    {
        var doc = new PipelineDocument { SourcePath = "pipeline.yml", RawContent = rawYaml };
        var parser = new Mock<IYamlParser>();
        parser.Setup(p => p.ParseFileAsync<PipelineDocument>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResult<PipelineDocument>
            {
                Success = true,
                Data = doc,
                Errors = Array.Empty<ParseError>(),
                SourceMap = Mock.Of<ISourceMap>()
            });

        var syntax = new Mock<ISyntaxValidator>();
        syntax.Setup(s => s.ValidateAsync(It.IsAny<PipelineDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult { IsValid = true, Errors = Array.Empty<ValidationError>(), Warnings = Array.Empty<ValidationError>() });

        var schema = new Mock<ISchemaManager>();
        var templates = new Mock<ITemplateResolver>();
        var reporter = new Mock<IErrorReporter>();
        reporter.Setup(r => r.GenerateReport(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationError>>(), It.IsAny<IReadOnlyList<ValidationError>>(), OutputFormatConfig.Text))
            .Returns(new ReportOutput { Content = "ok", Format = OutputFormatConfig.Text });
        var logger = new Mock<ILogger<ValidationOrchestrator>>();

        return new ValidationOrchestrator(parser.Object, syntax.Object, schema.Object, templates.Object, new VariableProcessor(), reporter.Object, logger.Object);
    }

    [Fact]
    public async Task ValidateAsync_LoadsVariablesFromYamlFile_FlattensVariablesSection()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"vars-{Guid.NewGuid():N}.yml");
        var yaml = """
variables:
  - name: buildConfiguration
    value: Release
  - name: targetPlatform
    value: x64
""";
        await File.WriteAllTextAsync(tmp, yaml);

        // Load via loader and process via VariableProcessor to validate integration
        var loader = new AdoPipelinesLocalRunner.Core.Variables.VariableFileLoader();
        var vars = loader.Load(new[] { tmp });
        var processor = new VariableProcessor();
        var doc = new PipelineDocument { SourcePath = "pipeline.yml", RawContent = "script: echo $(buildConfiguration) $(targetPlatform)" };
        var vctx = new VariableContext { SystemVariables = new Dictionary<string, object>(), PipelineVariables = vars, FailOnUnresolved = true };
        var vres = await processor.ProcessAsync(doc, vctx);

        vres.ProcessedDocument!.RawContent!.Should().Contain("Release");
        vres.ProcessedDocument!.RawContent!.Should().Contain("x64");
    }

    [Fact]
    public async Task ValidateAsync_LoadsVariablesFromJsonFile_SupportsMerging()
    {
        var tmp1 = Path.Combine(Path.GetTempPath(), $"vars1-{Guid.NewGuid():N}.json");
        var tmp2 = Path.Combine(Path.GetTempPath(), $"vars2-{Guid.NewGuid():N}.json");
        var json1 = JsonSerializer.Serialize(new { variables = new[]{ new { name = "name", value = "app" }, new { name = "env", value = "dev" } } });
        var json2 = JsonSerializer.Serialize(new { variables = new[]{ new { name = "env", value = "prod" } } });
        await File.WriteAllTextAsync(tmp1, json1);
        await File.WriteAllTextAsync(tmp2, json2);

        var loader = new AdoPipelinesLocalRunner.Core.Variables.VariableFileLoader();
        var vars = loader.Load(new[] { tmp1, tmp2 });
        var processor = new VariableProcessor();
        var doc = new PipelineDocument { SourcePath = "pipeline.yml", RawContent = "script: echo $(name)-$(env)" };
        var vctx = new VariableContext { SystemVariables = new Dictionary<string, object>(), PipelineVariables = vars, FailOnUnresolved = true };
        var vres = await processor.ProcessAsync(doc, vctx);

        var output = vres.ProcessedDocument!.RawContent!;
        output.Should().Contain("app-prod"); // second file overrides env
    }
}
