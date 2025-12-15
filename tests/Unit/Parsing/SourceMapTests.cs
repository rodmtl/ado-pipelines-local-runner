using AdoPipelinesLocalRunner.Contracts;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Parsing;

/// <summary>
/// Tests for ParseError record to improve coverage.
/// </summary>
public class ParseErrorTests
{
    [Fact]
    public void ParseError_WithAllProperties_IsCorrect()
    {
        var loc = new SourceLocation { FilePath = "test.yml", Line = 1, Column = 1 };
        var relLocs = new[] { new SourceLocation { FilePath = "ref.yml", Line = 2, Column = 1 } };

        var error = new ParseError
        {
            Code = "PARSE_ERR",
            Message = "Parse error occurred",
            Severity = Severity.Error,
            Location = loc,
            RelatedLocations = relLocs,
            Suggestion = "Check format"
        };

        Assert.Equal("PARSE_ERR", error.Code);
        Assert.Equal("Parse error occurred", error.Message);
        Assert.Equal(Severity.Error, error.Severity);
        Assert.NotNull(error.Location);
        Assert.NotNull(error.RelatedLocations);
        Assert.Equal("Check format", error.Suggestion);
    }

    [Fact]
    public void ParseError_WithoutLocation_IsValid()
    {
        var error = new ParseError
        {
            Code = "CODE",
            Message = "No location",
            Severity = Severity.Warning
        };

        Assert.Null(error.Location);
        Assert.Null(error.RelatedLocations);
    }

    [Fact]
    public void ParseError_AllSeverityLevels_AreValid()
    {
        var sev1 = new ParseError { Code = "I", Message = "m", Severity = Severity.Info };
        var sev2 = new ParseError { Code = "W", Message = "m", Severity = Severity.Warning };
        var sev3 = new ParseError { Code = "E", Message = "m", Severity = Severity.Error };
        var sev4 = new ParseError { Code = "C", Message = "m", Severity = Severity.Critical };

        Assert.Equal(Severity.Info, sev1.Severity);
        Assert.Equal(Severity.Warning, sev2.Severity);
        Assert.Equal(Severity.Error, sev3.Severity);
        Assert.Equal(Severity.Critical, sev4.Severity);
    }
}
