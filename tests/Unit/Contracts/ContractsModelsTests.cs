using AdoPipelinesLocalRunner.Contracts;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Contracts;

public class ContractsModelsTests
{
    [Fact]
    public void RepositoryContext_AllProperties_AreSet()
    {
        var ctx = new RepositoryContext
        {
            Repository = "https://github.com/org/repo",
            Reference = "refs/heads/main",
            AuthToken = "token"
        };

        Assert.Equal("https://github.com/org/repo", ctx.Repository);
        Assert.Equal("refs/heads/main", ctx.Reference);
        Assert.Equal("token", ctx.AuthToken);
    }

    [Fact]
    public void TemplateResolutionResult_AllProperties_AreSet()
    {
        var result = new TemplateResolutionResult
        {
            Success = true,
            Content = "steps: []",
            Source = "templates/base.yml",
            Errors = Array.Empty<ValidationError>(),
            Metadata = new Dictionary<string, object> { { "key", 1 } }
        };

        Assert.True(result.Success);
        Assert.Equal("steps: []", result.Content);
        Assert.Equal("templates/base.yml", result.Source);
        Assert.Empty(result.Errors);
        Assert.Single(result.Metadata);
    }

    [Fact]
    public void ResolvedTemplate_AllProperties_AreSet()
    {
        var tpl = new ResolvedTemplate
        {
            Reference = "base.yml",
            ResolvedSource = "/abs/path/base.yml",
            Parameters = new Dictionary<string, object> { { "p1", "v1" } },
            Depth = 2
        };

        Assert.Equal("base.yml", tpl.Reference);
        Assert.Equal("/abs/path/base.yml", tpl.ResolvedSource);
        Assert.Equal(2, tpl.Depth);
        Assert.Single(tpl.Parameters);
    }

    [Fact]
    public void SchemaValidationResult_AllProperties_AreSet()
    {
        var svr = new SchemaValidationResult
        {
            IsValid = false,
            SchemaVersion = "1.0.0",
            Errors = Array.Empty<ValidationError>(),
            Metadata = new Dictionary<string, object> { { "durationMs", 10 } }
        };

        Assert.False(svr.IsValid);
        Assert.Equal("1.0.0", svr.SchemaVersion);
        Assert.Empty(svr.Errors);
        Assert.Single(svr.Metadata);
    }

    [Fact]
    public void SchemaDefinition_AllProperties_AreSet()
    {
        var typeSchema = new TypeSchema
        {
            Name = "pipeline",
            Description = "Pipeline root",
            Properties = new Dictionary<string, PropertySchema>
            {
                {
                    "name",
                    new PropertySchema
                    {
                        Name = "name",
                        Description = "Pipeline name",
                        Types = new[] { "string" },
                        DefaultValue = null,
                        AllowedValues = null,
                        Pattern = null
                    }
                }
            },
            Required = new[] { "name" },
            AdditionalPropertiesAllowed = true
        };

        var schema = new SchemaDefinition
        {
            Version = "1.0.0",
            Schema = "https://example/schema.json",
            Types = new Dictionary<string, TypeSchema> { { "pipeline", typeSchema } },
            RootType = "pipeline",
            Metadata = new Dictionary<string, string> { { "author", "test" } }
        };

        Assert.Equal("1.0.0", schema.Version);
        Assert.Equal("https://example/schema.json", schema.Schema);
        Assert.Equal("pipeline", schema.RootType);
        Assert.True(schema.Types.ContainsKey("pipeline"));
        Assert.Equal("pipeline", typeSchema.Name);
        Assert.Equal("Pipeline root", typeSchema.Description);
        Assert.True(typeSchema.Properties.ContainsKey("name"));
        Assert.Contains("name", typeSchema.Required);
        Assert.True(typeSchema.AdditionalPropertiesAllowed);
    }

    [Fact]
    public void PropertySchema_AllProperties_AreSet()
    {
        var prop = new PropertySchema
        {
            Name = "displayName",
            Description = "Human readable name",
            Types = new[] { "string" },
            DefaultValue = "Build",
            AllowedValues = new object[] { "Build", "Test" },
            Pattern = ".+"
        };

        Assert.Equal("displayName", prop.Name);
        Assert.Equal("Human readable name", prop.Description);
        Assert.Contains("string", prop.Types);
        Assert.Equal("Build", prop.DefaultValue);
        Assert.Equal(2, prop.AllowedValues.Count);
        Assert.Equal(".+", prop.Pattern);
    }

    [Fact]
    public void ValidationError_CanBeInstantiated()
    {
        var error = new ValidationError
        {
            Code = "ERR",
            Message = "Something went wrong",
            Severity = Severity.Error,
            Suggestion = "Fix it"
        };

        Assert.Equal("ERR", error.Code);
        Assert.Equal("Something went wrong", error.Message);
        Assert.Equal(Severity.Error, error.Severity);
        Assert.Null(error.Location);
        Assert.Equal("Fix it", error.Suggestion);
    }

    [Fact]
    public void VariableGroup_AllProperties_AreSet()
    {
        var group = new VariableGroup
        {
            Name = "group1",
            Variables = new Dictionary<string, object> { { "k", "v" } },
            IsSecret = true,
            Description = "desc"
        };

        Assert.Equal("group1", group.Name);
        Assert.True(group.IsSecret);
        Assert.Single(group.Variables);
        Assert.Equal("desc", group.Description);
    }
}
