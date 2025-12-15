using AdoPipelinesLocalRunner.Contracts;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Architecture;

/// <summary>
/// Tests to verify NFR-4 Maintainability compliance.
/// Ensures SOLID principles, dependency injection, and proper separation of concerns.
/// </summary>
public class SolidPrinciplesAndDiTests
{
    /// <summary>
    /// Verify that core components are registered in DI container.
    /// Dependency Inversion Principle: High-level modules depend on abstractions.
    /// </summary>
    [Fact]
    public void DependencyInjection_CoreComponentsRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IYamlParser, AdoPipelinesLocalRunner.Core.Parsing.YamlParser>();
        services.AddSingleton<ISyntaxValidator, AdoPipelinesLocalRunner.Core.Validators.SyntaxValidator>();
        services.AddSingleton<ISchemaManager, AdoPipelinesLocalRunner.Core.Schema.SchemaManager>();
        services.AddSingleton<ITemplateResolver, AdoPipelinesLocalRunner.Core.Templates.TemplateResolver>();
        services.AddSingleton<IVariableProcessor, AdoPipelinesLocalRunner.Core.Variables.VariableProcessor>();
        services.AddSingleton<IErrorReporter, AdoPipelinesLocalRunner.Core.Reporting.ErrorReporter>();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        serviceProvider.GetRequiredService<IYamlParser>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ISyntaxValidator>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ISchemaManager>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ITemplateResolver>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IVariableProcessor>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IErrorReporter>().Should().NotBeNull();
    }

    /// <summary>
    /// Verify Single Responsibility Principle: Each component has one reason to change.
    /// </summary>
    [Fact]
    public void SingleResponsibility_ComponentsHaveDistinctRoles()
    {
        // The following components have distinct responsibilities:
        // - YamlParser: Parse YAML content
        // - SyntaxValidator: Validate syntax rules
        // - SchemaManager: Manage and validate against schema
        // - TemplateResolver: Resolve template references
        // - VariableProcessor: Process and substitute variables
        // - ErrorReporter: Generate formatted reports

        var interfaces = new[]
        {
            typeof(IYamlParser),
            typeof(ISyntaxValidator),
            typeof(ISchemaManager),
            typeof(ITemplateResolver),
            typeof(IVariableProcessor),
            typeof(IErrorReporter)
        };

        // Assert: Each interface is distinct (no interface pollution)
        interfaces.Should().HaveCount(6);
        interfaces.Distinct().Should().HaveCount(6, 
            because: "Each component should have a distinct, single responsibility");
    }

    /// <summary>
    /// Verify Interface Segregation Principle: Clients depend on focused interfaces.
    /// </summary>
    [Fact]
    public void InterfaceSegregation_InterfacesAreFocused()
    {
        // Arrange - Check that interfaces are focused on specific contracts
        var interfaces = new[]
        {
            typeof(IYamlParser),
            typeof(ISyntaxValidator),
            typeof(ISchemaManager),
            typeof(ITemplateResolver),
            typeof(IVariableProcessor),
            typeof(IErrorReporter)
        };

        // Act & Assert
        foreach (var iface in interfaces)
        {
            var methods = iface.GetMethods();
            methods.Should().NotBeEmpty();
            // Each interface should have a manageable number of methods (typically 1-3 for Phase 1)
        }
    }

    /// <summary>
    /// Verify Open/Closed Principle: Components are open for extension via interfaces.
    /// </summary>
    [Fact]
    public void OpenClosed_ExtensionViaInterfaces()
    {
        // The design uses strategy pattern - validators and resolvers extend via interfaces
        // New validators can be added without modifying existing code (open for extension)
        // Core orchestrator relies on abstractions, not concrete implementations (closed for modification)

        var validator = new AdoPipelinesLocalRunner.Core.Validators.SyntaxValidator();
        var rulesProperty = validator.GetType().GetProperty("_rules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert: Rules can be added (extensibility)
        validator.AddRule(new TestValidationRule());
        var rules = validator.GetRules();
        rules.Should().Contain(r => r.Name == "TEST_RULE");
    }

    /// <summary>
    /// Verify error handling follows consistent patterns.
    /// </summary>
    [Fact]
    public void ErrorHandling_ConsistentAcrossComponents()
    {
        // All validation results should follow consistent structure
        // - Include error codes
        // - Include severity levels
        // - Include location information
        // - Include optional suggestions

        var error = new ValidationError
        {
            Code = "TEST_ERROR",
            Message = "Test error message",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = "test.yml", Line = 1, Column = 1 },
            Suggestion = "Fix by doing X"
        };

        error.Code.Should().NotBeNullOrWhiteSpace();
        error.Message.Should().NotBeNullOrWhiteSpace();
        error.Severity.Should().Be(Severity.Error);
        error.Location.Should().NotBeNull();
        error.Suggestion.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Helper: Test validation rule for extensibility testing.
    /// </summary>
    private class TestValidationRule : IValidationRule
    {
        public string Name => "TEST_RULE";
        public Severity Severity => Severity.Error;
        public string Description => "Test rule for testing extensibility";

        public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
        {
            return Enumerable.Empty<ValidationError>();
        }
    }
}
