using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Errors;
using AdoPipelinesLocalRunner.Core.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AdoPipelinesLocalRunner.Tests.Unit.Orchestration;

public class ValidationOrchestratorPrivateTests
{
    private static ValidationOrchestrator CreateOrchestrator() =>
        new ValidationOrchestrator(
            Mock.Of<IYamlParser>(),
            Mock.Of<ISyntaxValidator>(),
            Mock.Of<ISchemaManager>(),
            Mock.Of<ITemplateResolver>(),
            Mock.Of<IVariableProcessor>(),
            Mock.Of<IErrorReporter>(),
            Mock.Of<ILogger<ValidationOrchestrator>>());

    [Fact]
    public void ParseYamlVariables_WithVariablesArray_ReturnsDictionary()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("ParseYamlVariables", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var yamlObject = new Dictionary<object, object>
        {
            ["variables"] = new List<object>
            {
                new Dictionary<object, object> { ["name"] = "ALPHA", ["value"] = "one" },
                new Dictionary<object, object> { ["name"] = "BETA", ["value"] = "two" }
            }
        };

        var result = (Dictionary<string, object>?)method!.Invoke(orchestrator, new object?[] { yamlObject });

        result.Should().NotBeNull();
        result!.Should().ContainKey("ALPHA").WhoseValue.Should().Be("one");
        result.Should().ContainKey("BETA").WhoseValue.Should().Be("two");
    }

    [Fact]
    public void ParseJsonVariables_WithMissingValue_UsesEmptyString()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("ParseJsonVariables", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        const string json = "{\"variables\":[{\"name\":\"X\",\"value\":\"1\"},{\"name\":\"Y\"}]}";
        var result = (Dictionary<string, object>?)method!.Invoke(orchestrator, new object?[] { json });

        result.Should().NotBeNull();
        result!.Should().ContainKey("X").WhoseValue.Should().Be("1");
        result.Should().ContainKey("Y").WhoseValue.Should().Be(string.Empty);
    }

    [Fact]
    public void TryLoadVariableFile_WithYaml_Succeeds()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("TryLoadVariableFile", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var yamlFile = Path.Combine(Path.GetTempPath(), $"vars-{Guid.NewGuid():N}.yml");
        File.WriteAllText(yamlFile, "FOO: bar\nvariables:\n  - name: BAZ\n    value: qux\n");

        var resultDict = new Dictionary<string, object>();
        var errors = new List<ValidationError>();

        try
        {
            var success = (bool)method!.Invoke(orchestrator, new object?[] { yamlFile, deserializer, resultDict, errors })!;
            success.Should().BeTrue();
            resultDict.Should().ContainKey("BAZ").WhoseValue.Should().Be("qux");
            errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(yamlFile)) File.Delete(yamlFile);
        }
    }

    [Fact]
    public void TryLoadVariableFile_WithMissingFile_AddsError()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("TryLoadVariableFile", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.yml");
        var resultDict = new Dictionary<string, object>();
        var errors = new List<ValidationError>();

        var success = (bool)method!.Invoke(orchestrator, new object?[] { missingPath, deserializer, resultDict, errors })!;

        success.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Code == "VARIABLE_FILE_NOT_FOUND");
    }

    [Fact]
    public void LoadVariablesFromFiles_MergesYamlAndJsonVariables()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("LoadVariablesFromFiles", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var tempDir = Path.Combine(Path.GetTempPath(), $"vars-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var yamlPath = Path.Combine(tempDir, "vars.yml");
        var jsonPath = Path.Combine(tempDir, "vars.json");
        File.WriteAllText(yamlPath, "variables:\n  - name: ALPHA\n    value: one\n  - name: BETA\n    value: two\n");
        File.WriteAllText(jsonPath, "{ \"variables\": [ { \"name\": \"gamma\", \"value\": \"3\" } ] }");

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ValidationError>();

        try
        {
            method!.Invoke(orchestrator, new object?[] { new[] { yamlPath, jsonPath }, tempDir, result, errors });

            result.Should().ContainKey("ALPHA").WhoseValue.Should().Be("one");
            result.Should().ContainKey("BETA").WhoseValue.Should().Be("two");
            result.Should().ContainKey("gamma").WhoseValue.Should().Be("3");
            errors.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TryLoadVariableFile_WithInvalidJson_AddsInvalidError()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("TryLoadVariableFile", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var tempFile = Path.Combine(Path.GetTempPath(), $"vars-invalid-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "not-json");

        var resultDict = new Dictionary<string, object>();
        var errors = new List<ValidationError>();

        try
        {
            var success = (bool)method!.Invoke(orchestrator, new object?[] { tempFile, deserializer, resultDict, errors })!;

            success.Should().BeFalse();
            errors.Should().ContainSingle(e => e.Code == "VARIABLE_FILE_INVALID");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseYamlVariables_WithStringDictionary_FlattensEntries()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("ParseYamlVariables", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var yamlObject = new Dictionary<string, object>
        {
            ["variables"] = new List<object>
            {
                new Dictionary<string, object> { ["name"] = "foo", ["value"] = "bar" },
                new Dictionary<string, object> { ["name"] = "baz", ["value"] = 5 }
            },
            ["other"] = "value"
        };

        var result = (Dictionary<string, object>?)method!.Invoke(orchestrator, new object?[] { yamlObject });

        result.Should().NotBeNull();
        result!.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        result.Should().ContainKey("baz").WhoseValue.Should().Be(5);
        // When variables array is present, only array items are returned, not top-level keys
        result.Should().NotContainKey("other");
    }

    [Fact]
    public void CreateYamlDeserializer_ShouldIgnoreUnmatchedProperties()
    {
        var orchestrator = CreateOrchestrator();
        var method = typeof(ValidationOrchestrator).GetMethod("CreateYamlDeserializer", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var deserializer = (IDeserializer)method!.Invoke(orchestrator, Array.Empty<object?>())!;

        var result = deserializer.Deserialize<SimpleVariable>("name: foo\nvalue: bar\nextra: 123");
        result.Name.Should().Be("foo");
        result.Value.Should().Be("bar");
    }

    private class SimpleVariable
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}
