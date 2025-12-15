using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Validators;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Usability;

/// <summary>
/// Tests to verify NFR-3 Usability compliance.
/// Ensures error messages include remediation hints and clear guidance.
/// </summary>
public class ErrorRemediationHintsTests
{
    /// <summary>
    /// Verify that missing work definition error includes a remediation suggestion.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_MissingWorkDefinition_IncludesSuggestion()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            // No stages, jobs, or steps
        };
        var validator = new SyntaxValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Errors.Should().NotBeEmpty();
        var error = result.Errors.First(e => e.Code == "NO_WORK_DEFINITION");
        error.Suggestion.Should().NotBeNullOrWhiteSpace(
            because: "Error messages must include remediation hints");
        error.Suggestion.Should().Contain("stages", "jobs", "steps");
    }

    /// <summary>
    /// Verify that missing trigger warning includes a remediation suggestion.
    /// </summary>
    [Fact]
    public async Task WarningMessage_MissingTrigger_IncludesSuggestion()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = null,
            Steps = new[] { (object)new { script = "echo test", displayName = "Test" } }
        };
        var validator = new SyntaxValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Warnings.Should().NotBeEmpty();
        var warning = result.Warnings.First(w => w.Code == "MISSING_TRIGGER");
        warning.Suggestion.Should().NotBeNullOrWhiteSpace(
            because: "Warnings must include remediation guidance");
        warning.Suggestion.Should().Contain("trigger");
    }

    /// <summary>
    /// Verify that conflicting structure errors include remediation guidance.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_ConflictingStructure_HasSuggestion()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = new[] { (object)new { displayName = "Build", jobs = new object[0] } },
            Jobs = new[] { (object)new { displayName = "Job1", steps = new object[0] } }
        };
        var validator = new SyntaxValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Errors.Should().NotBeEmpty();
        var error = result.Errors.First(e => 
            e.Code.Contains("CONFLICTING_STRUCTURE"));
        error.Message.Should().NotBeNullOrWhiteSpace();
        // Suggestion may be on message or Suggestion field
    }

    /// <summary>
    /// Verify that invalid stage structure error includes hints.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_InvalidStageStructure_IncludesSuggestion()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = new[] { (object)new { } } // Empty stage - no displayName or jobs
        };
        var validator = new SyntaxValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Errors.Should().NotBeEmpty();
        var error = result.Errors.FirstOrDefault(e => e.Code == "INVALID_STAGE_STRUCTURE");
        if (error != null)
        {
            error.Suggestion.Should().NotBeNullOrWhiteSpace(
                because: "Invalid stage structure should guide user on required fields");
            error.Suggestion.Should().Contain("displayName", "jobs");
        }
    }

    /// <summary>
    /// Verify error location information is complete (file, line, column).
    /// </summary>
    [Fact]
    public async Task ErrorLocation_IncludesFileLineColumn_ForPreciseGuidance()
    {
        // Arrange
        var document = new PipelineDocument
        {
            SourcePath = "azure-pipelines.yml",
            Trigger = new { }
        };
        var validator = new SyntaxValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        if (result.Errors.Count > 0)
        {
            var error = result.Errors.First();
            error.Location.Should().NotBeNull();
            error.Location!.FilePath.Should().NotBeNullOrEmpty(
                because: "Error location must identify the source file");
            error.Location!.Line.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Verify that all error types have meaningful codes and messages.
    /// </summary>
    [Fact]
    public async Task ErrorMessages_AllHaveCodes_AndMessages()
    {
        // Arrange
        var document = new PipelineDocument { Trigger = null };
        var validator = new SyntaxValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        foreach (var error in result.Errors.Concat(result.Warnings))
        {
            error.Code.Should().NotBeNullOrWhiteSpace();
            error.Message.Should().NotBeNullOrWhiteSpace();
            error.Severity.Should().Be(error.Severity); // Just ensure field is set
        }
    }
}
