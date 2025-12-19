using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Variables;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Variables;

public class VariableFileLoaderTests
{
    [Fact]
    public async Task Load_YamlVariablesList_FlattensCorrectly()
    {
        var file = Path.Combine(Path.GetTempPath(), $"vars-{Guid.NewGuid():N}.yml");
        var yaml = """
variables:
  - name: a
    value: 1
  - name: b
    value: text
""";
        await File.WriteAllTextAsync(file, yaml);

        var loader = new VariableFileLoader();
        var result = loader.Load(new[] { file });

        result.Should().ContainKey("a").WhoseValue.Should().Be("1");
        result.Should().ContainKey("b").WhoseValue.Should().Be("text");
    }

    [Fact]
    public async Task Load_JsonVariablesList_MergesAndOverrides()
    {
        var f1 = Path.Combine(Path.GetTempPath(), $"vars1-{Guid.NewGuid():N}.json");
        var f2 = Path.Combine(Path.GetTempPath(), $"vars2-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(f1, "{\"variables\":[{\"name\":\"x\",\"value\":\"1\"},{\"name\":\"y\",\"value\":\"a\"}]}\n");
        await File.WriteAllTextAsync(f2, "{\"variables\":[{\"name\":\"y\",\"value\":\"b\"}]}\n");

        var loader = new VariableFileLoader();
        var result = loader.Load(new[] { f1, f2 });

        result.Should().ContainKey("x").WhoseValue.Should().Be("1");
        result.Should().ContainKey("y").WhoseValue.Should().Be("b");
    }
}
