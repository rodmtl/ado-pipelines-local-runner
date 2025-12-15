using AdoPipelinesLocalRunner.Contracts;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Contracts;

/// <summary>
/// Tests to cover ValidationError record type with various configurations.
/// </summary>
public class ValidationErrorTests
{
    [Fact]
    public void ValidationError_WithAllProperties_IsCorrect()
    {
        var error = new ValidationError
        {
            Code = "TEST_CODE",
            Message = "Test message",
            Severity = Severity.Error,
            Suggestion = "Fix this"
        };

        Assert.Equal("TEST_CODE", error.Code);
        Assert.Equal("Test message", error.Message);
        Assert.Equal(Severity.Error, error.Severity);
        Assert.Equal("Fix this", error.Suggestion);
    }

    [Fact]
    public void ValidationError_WithRelatedLocations_IsCorrect()
    {
        var relLocs = new List<SourceLocation>
        {
            new SourceLocation { FilePath = "file1.yml", Line = 1, Column = 1 },
            new SourceLocation { FilePath = "file2.yml", Line = 2, Column = 2 }
        };

        var error = new ValidationError
        {
            Code = "RELATED_ERROR",
            Message = "Error with related locations",
            Severity = Severity.Warning,
            RelatedLocations = relLocs
        };

        Assert.NotNull(error.RelatedLocations);
        Assert.Equal(2, error.RelatedLocations.Count);
    }

    [Fact]
    public void ValidationError_WithoutLocation_IsValid()
    {
        var error = new ValidationError
        {
            Code = "NO_LOC",
            Message = "Error without location",
            Severity = Severity.Critical
        };

        Assert.Null(error.Location);
    }

    [Fact]
    public void ValidationError_WithSeverities_ChecksAll()
    {
        var infoError = new ValidationError { Code = "I", Message = "info", Severity = Severity.Info };
        var warnError = new ValidationError { Code = "W", Message = "warn", Severity = Severity.Warning };
        var errError = new ValidationError { Code = "E", Message = "err", Severity = Severity.Error };
        var critError = new ValidationError { Code = "C", Message = "crit", Severity = Severity.Critical };

        Assert.Equal(Severity.Info, infoError.Severity);
        Assert.Equal(Severity.Warning, warnError.Severity);
        Assert.Equal(Severity.Error, errError.Severity);
        Assert.Equal(Severity.Critical, critError.Severity);
    }

    [Fact]
    public void ValidationError_WithNullSuggestion_IsValid()
    {
        var error = new ValidationError
        {
            Code = "CODE",
            Message = "Message",
            Severity = Severity.Error,
            Suggestion = null
        };

        Assert.Null(error.Suggestion);
    }
}
