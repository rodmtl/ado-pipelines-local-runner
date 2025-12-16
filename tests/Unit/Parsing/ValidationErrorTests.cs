using AdoPipelinesLocalRunner.Contracts;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Parsing;

/// <summary>
/// Tests for ValidationError record to improve coverage.
/// </summary>
public class ValidationErrorTests
{
    [Fact]
    public void ValidationError_WithAllProperties_IsCorrect()
    {
        // Arrange
        var loc = new SourceLocation 
        { 
            FilePath = "pipeline.yml", 
            Line = 10, 
            Column = 5 
        };
        var relLocs = new[] 
        { 
            new SourceLocation { FilePath = "template.yml", Line = 2, Column = 1 } 
        };

        // Act
        var error = new ValidationError
        {
            Code = "SCHEMA_INVALID",
            Message = "Property 'name' is required",
            Severity = Severity.Error,
            Location = loc,
            RelatedLocations = relLocs,
            Suggestion = "Add the 'name' property to your job definition"
        };

        // Assert
        error.Code.Should().Be("SCHEMA_INVALID");
        error.Message.Should().Be("Property 'name' is required");
        error.Severity.Should().Be(Severity.Error);
        error.Location.Should().NotBeNull();
        error.Location!.FilePath.Should().Be("pipeline.yml");
        error.Location.Line.Should().Be(10);
        error.Location.Column.Should().Be(5);
        error.RelatedLocations.Should().HaveCount(1);
        error.Suggestion.Should().Be("Add the 'name' property to your job definition");
    }

    [Fact]
    public void ValidationError_WithoutOptionalProperties_IsValid()
    {
        // Act
        var error = new ValidationError
        {
            Code = "MISSING_PROPERTY",
            Message = "Required property missing",
            Severity = Severity.Warning
        };

        // Assert
        error.Location.Should().BeNull();
        error.RelatedLocations.Should().BeNull();
        error.Suggestion.Should().BeNull();
    }

    [Fact]
    public void ValidationError_InheritsFromParseError()
    {
        // Act
        var error = new ValidationError
        {
            Code = "TEST",
            Message = "Test message",
            Severity = Severity.Info
        };

        // Assert
        error.Should().BeAssignableTo<ParseError>();
    }

    [Fact]
    public void ValidationError_AllSeverityLevels_AreValid()
    {
        // Arrange & Act
        var infoError = new ValidationError 
        { 
            Code = "INFO", 
            Message = "Info message", 
            Severity = Severity.Info 
        };
        var warningError = new ValidationError 
        { 
            Code = "WARN", 
            Message = "Warning message", 
            Severity = Severity.Warning 
        };
        var error = new ValidationError 
        { 
            Code = "ERR", 
            Message = "Error message", 
            Severity = Severity.Error 
        };
        var criticalError = new ValidationError 
        { 
            Code = "CRIT", 
            Message = "Critical message", 
            Severity = Severity.Critical 
        };

        // Assert
        infoError.Severity.Should().Be(Severity.Info);
        warningError.Severity.Should().Be(Severity.Warning);
        error.Severity.Should().Be(Severity.Error);
        criticalError.Severity.Should().Be(Severity.Critical);
    }

    [Fact]
    public void ValidationError_WithMultipleRelatedLocations_IsValid()
    {
        // Arrange
        var relatedLocs = new[]
        {
            new SourceLocation { FilePath = "template1.yml", Line = 5, Column = 1 },
            new SourceLocation { FilePath = "template2.yml", Line = 10, Column = 3 },
            new SourceLocation { FilePath = "base.yml", Line = 1, Column = 1 }
        };

        // Act
        var error = new ValidationError
        {
            Code = "CIRCULAR_REFERENCE",
            Message = "Circular template reference detected",
            Severity = Severity.Error,
            RelatedLocations = relatedLocs
        };

        // Assert
        error.RelatedLocations.Should().HaveCount(3);
        error.RelatedLocations![0].FilePath.Should().Be("template1.yml");
        error.RelatedLocations[1].FilePath.Should().Be("template2.yml");
        error.RelatedLocations[2].FilePath.Should().Be("base.yml");
    }

    [Fact]
    public void ValidationError_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var error1 = new ValidationError
        {
            Code = "TEST",
            Message = "Test message",
            Severity = Severity.Error
        };

        var error2 = new ValidationError
        {
            Code = "TEST",
            Message = "Test message",
            Severity = Severity.Error
        };

        var error3 = new ValidationError
        {
            Code = "DIFFERENT",
            Message = "Test message",
            Severity = Severity.Error
        };

        // Assert
        error1.Should().Be(error2);
        error1.Should().NotBe(error3);
    }
}
