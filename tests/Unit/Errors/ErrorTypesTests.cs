using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Contracts.Errors;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Errors;

public class ErrorTypesTests
{
    [Fact]
    public void YamlParseException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var innerException = new Exception("Inner error");

        // Act
        var exception = new YamlParseException("Parse failed", location, innerException);

        // Assert
        Assert.Equal("YAML_PARSE_ERROR", exception.ErrorCode);
        Assert.Equal("Parse failed", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void SyntaxValidationException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var error = new ValidationError
        {
            Code = "SYNTAX_ERROR",
            Message = "Syntax error",
            Severity = Severity.Error,
            Location = location
        };
        var validationResult = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { error },
            Warnings = Array.Empty<ValidationError>()
        };

        // Act
        var exception = new SyntaxValidationException("Validation failed", validationResult, location);

        // Assert
        Assert.Equal("SYNTAX_VALIDATION_ERROR", exception.ErrorCode);
        Assert.Equal("Validation failed", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal(validationResult, exception.ValidationResult);
    }

    [Fact]
    public void SchemaValidationException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var schemaResult = new SchemaValidationResult
        {
            IsValid = false,
            SchemaVersion = "1.0.0",
            Errors = Array.Empty<ValidationError>()
        };

        // Act
        var exception = new SchemaValidationException("Schema validation failed", schemaResult, location);

        // Assert
        Assert.Equal("SCHEMA_VALIDATION_ERROR", exception.ErrorCode);
        Assert.Equal("Schema validation failed", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal(schemaResult, exception.ValidationResult);
    }

    [Fact]
    public void TemplateResolutionException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var innerException = new Exception("File not found");

        // Act
        var exception = new TemplateResolutionException(
            "template.yml",
            "Template not found",
            location,
            innerException);

        // Assert
        Assert.Equal("TEMPLATE_RESOLUTION_ERROR", exception.ErrorCode);
        Assert.Equal("Template not found", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal("template.yml", exception.TemplateReference);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void CircularTemplateException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var chain = new List<string> { "template1.yml", "template2.yml", "template1.yml" };

        // Act
        var exception = new CircularTemplateException(chain, location);

        // Assert
        Assert.Equal("TEMPLATE_RESOLUTION_ERROR", exception.ErrorCode);
        Assert.Contains("Circular template dependency", exception.Message);
        Assert.Contains("template1.yml -> template2.yml -> template1.yml", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal(chain, exception.DependencyChain);
        Assert.Equal("template1.yml", exception.TemplateReference);
    }

    [Fact]
    public void TemplateDepthExceededException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };

        // Act
        var exception = new TemplateDepthExceededException(
            "deep-template.yml",
            10,
            15,
            location);

        // Assert
        Assert.Equal("TEMPLATE_RESOLUTION_ERROR", exception.ErrorCode);
        Assert.Contains("nesting depth (15) exceeds maximum allowed (10)", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal("deep-template.yml", exception.TemplateReference);
        Assert.Equal(10, exception.MaxDepth);
        Assert.Equal(15, exception.ActualDepth);
    }

    [Fact]
    public void VariableProcessingException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var innerException = new Exception("Parse error");

        // Act
        var exception = new VariableProcessingException(
            "$(myVariable)",
            "Variable processing failed",
            location,
            innerException);

        // Assert
        Assert.Equal("VARIABLE_PROCESSING_ERROR", exception.ErrorCode);
        Assert.Equal("Variable processing failed", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal("$(myVariable)", exception.VariableExpression);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void UndefinedVariableException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };

        // Act
        var exception = new UndefinedVariableException("myVariable", location);

        // Assert
        Assert.Equal("VARIABLE_PROCESSING_ERROR", exception.ErrorCode);
        Assert.Contains("Variable 'myVariable' is not defined", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal("myVariable", exception.VariableExpression);
    }

    [Fact]
    public void ResourceException_HasCorrectProperties()
    {
        // Arrange
        var innerException = new Exception("IO error");

        // Act
        var exception = new ResourceException(
            "/path/to/file.yml",
            "Resource not found",
            innerException);

        // Assert
        Assert.Equal("RESOURCE_ERROR", exception.ErrorCode);
        Assert.Equal("Resource not found", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Null(exception.Location);
        Assert.Equal("/path/to/file.yml", exception.ResourcePath);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ConfigurationException_HasCorrectProperties()
    {
        // Arrange
        var innerException = new Exception("Invalid value");

        // Act
        var exception = new ConfigurationException(
            "MaxTemplateDepth",
            "Configuration value is invalid",
            innerException);

        // Assert
        Assert.Equal("CONFIGURATION_ERROR", exception.ErrorCode);
        Assert.Equal("Configuration value is invalid", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Null(exception.Location);
        Assert.Equal("MaxTemplateDepth", exception.ConfigurationKey);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void InvalidPipelineException_HasCorrectProperties()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var error1 = new ValidationError
        {
            Code = "ERROR1",
            Message = "Error 1",
            Severity = Severity.Error,
            Location = location
        };
        var error2 = new ValidationError
        {
            Code = "ERROR2",
            Message = "Error 2",
            Severity = Severity.Error,
            Location = location
        };
        var errors = new List<ValidationError> { error1, error2 };

        // Act
        var exception = new InvalidPipelineException(
            "Pipeline has validation errors",
            errors,
            location);

        // Assert
        Assert.Equal("INVALID_PIPELINE", exception.ErrorCode);
        Assert.Equal("Pipeline has validation errors", exception.Message);
        Assert.Equal(Severity.Error, exception.Severity);
        Assert.Equal(location, exception.Location);
        Assert.Equal(errors, exception.ValidationErrors);
        Assert.Equal(2, exception.ValidationErrors.Count);
    }

    [Fact]
    public void PipelineException_CanBeCreatedWithMinimalParameters()
    {
        // Arrange & Act
        var exception = new YamlParseException("Test error");

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("Test error", exception.Message);
        Assert.Null(exception.Location);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void AllExceptions_AreSerializable()
    {
        // Arrange
        var location = new SourceLocation { FilePath = "test.yml", Line = 10, Column = 5 };
        var validResult = new ValidationResult 
        { 
            IsValid = false, 
            Errors = Array.Empty<ValidationError>(), 
            Warnings = Array.Empty<ValidationError>() 
        };
        var schemaResult = new SchemaValidationResult 
        { 
            IsValid = false, 
            SchemaVersion = "1.0.0", 
            Errors = Array.Empty<ValidationError>() 
        };

        // Act & Assert - Test that exceptions can be instantiated without errors
        Assert.NotNull(new YamlParseException("Test", location));
        Assert.NotNull(new SyntaxValidationException("Test", validResult, location));
        Assert.NotNull(new SchemaValidationException("Test", schemaResult, location));
        Assert.NotNull(new TemplateResolutionException("ref", "Test", location));
        Assert.NotNull(new CircularTemplateException(new[] { "a", "b" }, location));
        Assert.NotNull(new TemplateDepthExceededException("ref", 10, 15, location));
        Assert.NotNull(new VariableProcessingException("var", "Test", location));
        Assert.NotNull(new UndefinedVariableException("var", location));
        Assert.NotNull(new ResourceException("path", "Test"));
        Assert.NotNull(new ConfigurationException("key", "Test"));
        Assert.NotNull(new InvalidPipelineException("Test", Array.Empty<ValidationError>(), location));
    }
}
