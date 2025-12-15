using AdoPipelinesLocalRunner.Contracts.Configuration;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Configuration;

public class ValidationConfigurationTests
{
    [Fact]
    public void ValidationConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new ValidationConfiguration();

        // Assert
        Assert.True(config.EnableSyntaxValidation);
        Assert.True(config.EnableSchemaValidation);
        Assert.True(config.EnableTemplateResolution);
        Assert.True(config.EnableVariableProcessing);
        Assert.Null(config.SchemaVersion);
        Assert.False(config.TreatWarningsAsErrors);
        Assert.Equal(10, config.MaxTemplateDepth);
        Assert.Equal(300, config.TimeoutSeconds);
        Assert.Null(config.EnabledRules);
        Assert.Null(config.DisabledRules);
        Assert.Null(config.ExcludePatterns);
    }

    [Fact]
    public void ValidationConfiguration_CustomValues_AreSet()
    {
        // Arrange & Act
        var config = new ValidationConfiguration
        {
            EnableSyntaxValidation = false,
            EnableSchemaValidation = false,
            EnableTemplateResolution = false,
            EnableVariableProcessing = false,
            SchemaVersion = "1.0.0",
            TreatWarningsAsErrors = true,
            MaxTemplateDepth = 20,
            TimeoutSeconds = 600,
            EnabledRules = new[] { "rule1", "rule2" },
            DisabledRules = new[] { "rule3" },
            ExcludePatterns = new[] { "*.test.yml" }
        };

        // Assert
        Assert.False(config.EnableSyntaxValidation);
        Assert.False(config.EnableSchemaValidation);
        Assert.False(config.EnableTemplateResolution);
        Assert.False(config.EnableVariableProcessing);
        Assert.Equal("1.0.0", config.SchemaVersion);
        Assert.True(config.TreatWarningsAsErrors);
        Assert.Equal(20, config.MaxTemplateDepth);
        Assert.Equal(600, config.TimeoutSeconds);
        Assert.Equal(2, config.EnabledRules.Count);
        Assert.Single(config.DisabledRules);
        Assert.Single(config.ExcludePatterns);
    }
}

public class TemplateConfigurationTests
{
    [Fact]
    public void TemplateConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new TemplateConfiguration
        {
            BasePaths = new[] { "/templates" }
        };

        // Assert
        Assert.Single(config.BasePaths);
        Assert.True(config.AllowRemoteTemplates);
        Assert.True(config.EnableCache);
        Assert.Null(config.CacheDirectory);
        Assert.Null(config.Repositories);
        Assert.Equal(30, config.DownloadTimeoutSeconds);
    }

    [Fact]
    public void TemplateConfiguration_CustomValues_AreSet()
    {
        // Arrange
        var repos = new[]
        {
            new RepositoryConfiguration
            {
                Repository = "https://github.com/org/repo",
                AuthToken = "token123",
                DefaultReference = "main",
                BasePath = "/templates"
            }
        };

        // Act
        var config = new TemplateConfiguration
        {
            BasePaths = new[] { "/templates", "/shared" },
            AllowRemoteTemplates = false,
            EnableCache = false,
            CacheDirectory = "/cache",
            Repositories = repos,
            DownloadTimeoutSeconds = 60
        };

        // Assert
        Assert.Equal(2, config.BasePaths.Count);
        Assert.False(config.AllowRemoteTemplates);
        Assert.False(config.EnableCache);
        Assert.Equal("/cache", config.CacheDirectory);
        Assert.Single(config.Repositories);
        Assert.Equal(60, config.DownloadTimeoutSeconds);
    }
}

public class RepositoryConfigurationTests
{
    [Fact]
    public void RepositoryConfiguration_AllProperties_AreSet()
    {
        // Arrange & Act
        var config = new RepositoryConfiguration
        {
            Repository = "https://github.com/org/repo",
            AuthToken = "token123",
            DefaultReference = "main",
            BasePath = "/templates"
        };

        // Assert
        Assert.Equal("https://github.com/org/repo", config.Repository);
        Assert.Equal("token123", config.AuthToken);
        Assert.Equal("main", config.DefaultReference);
        Assert.Equal("/templates", config.BasePath);
    }
}

public class VariableConfigurationTests
{
    [Fact]
    public void VariableConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new VariableConfiguration();

        // Assert
        Assert.Null(config.VariableFiles);
        Assert.Null(config.MockVariableGroups);
        Assert.True(config.FailOnUndefined);
        Assert.True(config.PreserveSecrets);
        Assert.Null(config.SystemVariableOverrides);
    }

    [Fact]
    public void VariableConfiguration_CustomValues_AreSet()
    {
        // Arrange

        var mockGroups = new Dictionary<string, VariableGroupConfiguration>
        {
            { "group1", new VariableGroupConfiguration
                {
                    Name = "group1",
                    Variables = new Dictionary<string, string> { { "key1", "value1" } },
                    IsSecret = false,
                    Description = "Test group"
                }
            }
        };

        // Act
        var config = new VariableConfiguration
        {
            VariableFiles = new[] { "vars.yml" },
            MockVariableGroups = mockGroups,
            FailOnUndefined = false,
            PreserveSecrets = true,
            SystemVariableOverrides = new Dictionary<string, object> { { "System.TeamProject", "TestProject" } }
        };

        // Assert
        Assert.Single(config.VariableFiles);
        Assert.Single(config.MockVariableGroups);
        Assert.False(config.FailOnUndefined);
        Assert.True(config.PreserveSecrets);
        Assert.Single(config.SystemVariableOverrides);
    }
}

public class VariableGroupConfigurationTests
{
    [Fact]
    public void VariableGroupConfiguration_AllProperties_AreSet()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var config = new VariableGroupConfiguration
        {
            Name = "TestGroup",
            Variables = variables,
            IsSecret = true,
            Description = "Test Description"
        };

        // Assert
        Assert.Equal("TestGroup", config.Name);
        Assert.Equal(2, config.Variables.Count);
        Assert.True(config.IsSecret);
        Assert.Equal("Test Description", config.Description);
    }
}

public class SchemaConfigurationTests
{
    [Fact]
    public void SchemaConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new SchemaConfiguration();

        // Assert
        Assert.Null(config.CustomSchemaPath);
        Assert.Null(config.SchemaUrl);
        Assert.True(config.UseEmbeddedSchema);
        Assert.False(config.StrictMode);
        Assert.Null(config.TypeMappings);
    }

    [Fact]
    public void SchemaConfiguration_CustomValues_AreSet()
    {
        // Arrange
        var typeMappings = new Dictionary<string, string>
        {
            { "customType", "string" }
        };

        // Act
        var config = new SchemaConfiguration
        {
            CustomSchemaPath = "/schema/custom.json",
            SchemaUrl = "https://example.com/schema.json",
            UseEmbeddedSchema = false,
            StrictMode = true,
            TypeMappings = typeMappings
        };

        // Assert
        Assert.Equal("/schema/custom.json", config.CustomSchemaPath);
        Assert.Equal("https://example.com/schema.json", config.SchemaUrl);
        Assert.False(config.UseEmbeddedSchema);
        Assert.True(config.StrictMode);
        Assert.Single(config.TypeMappings);
    }
}

public class ApplicationConfigurationTests
{
    [Fact]
    public void ApplicationConfiguration_AllProperties_AreSet()
    {
        // Arrange
        var validation = new ValidationConfiguration();
        var template = new TemplateConfiguration { BasePaths = new[] { "/" } };
        var variable = new VariableConfiguration();
        var schema = new SchemaConfiguration();
        var logging = new LoggingConfiguration { MinimumLevel = LogLevel.Information };
        var output = new OutputConfiguration { DefaultFormat = OutputFormat.Text };

        // Act
        var config = new ApplicationConfiguration
        {
            Validation = validation,
            Template = template,
            Variable = variable,
            Schema = schema,
            Logging = logging,
            Output = output
        };

        // Assert
        Assert.NotNull(config.Validation);
        Assert.NotNull(config.Template);
        Assert.NotNull(config.Variable);
        Assert.NotNull(config.Schema);
        Assert.NotNull(config.Logging);
        Assert.NotNull(config.Output);
    }
}

public class LoggingConfigurationTests
{
    [Fact]
    public void LoggingConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new LoggingConfiguration();

        // Assert
        Assert.Equal(LogLevel.Information, config.MinimumLevel);
        Assert.True(config.LogToConsole);
        Assert.False(config.LogToFile);
        Assert.Null(config.LogFilePath);
        Assert.True(config.IncludeTimestamps);
    }

    [Fact]
    public void LoggingConfiguration_CustomValues_AreSet()
    {
        // Arrange & Act
        var config = new LoggingConfiguration
        {
            MinimumLevel = LogLevel.Debug,
            LogToConsole = false,
            LogToFile = true,
            LogFilePath = "/logs/app.log",
            IncludeTimestamps = false
        };

        // Assert
        Assert.Equal(LogLevel.Debug, config.MinimumLevel);
        Assert.False(config.LogToConsole);
        Assert.True(config.LogToFile);
        Assert.Equal("/logs/app.log", config.LogFilePath);
        Assert.False(config.IncludeTimestamps);
    }
}

public class OutputConfigurationTests
{
    [Fact]
    public void OutputConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new OutputConfiguration();

        // Assert
        Assert.Equal(OutputFormat.Text, config.DefaultFormat);
        Assert.True(config.UseColors);
        Assert.False(config.Verbose);
        Assert.True(config.ShowProgress);
        Assert.Equal(120, config.MaxWidth);
    }

    [Fact]
    public void OutputConfiguration_CustomValues_AreSet()
    {
        // Arrange & Act
        var config = new OutputConfiguration
        {
            DefaultFormat = OutputFormat.Json,
            UseColors = false,
            Verbose = true,
            ShowProgress = false,
            MaxWidth = 80
        };

        // Assert
        Assert.Equal(OutputFormat.Json, config.DefaultFormat);
        Assert.False(config.UseColors);
        Assert.True(config.Verbose);
        Assert.False(config.ShowProgress);
        Assert.Equal(80, config.MaxWidth);
    }
}
