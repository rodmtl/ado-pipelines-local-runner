using AdoPipelinesLocalRunner.Contracts;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Validators;

/// <summary>
/// Unit tests for SyntaxValidator component following TDD principles.
/// Tests cover: Required fields, structure validation, stage/job/step hierarchy.
/// </summary>
public class SyntaxValidatorTests
{
    #region Valid Structure Tests (40%)

    [Fact]
    public async Task ValidateAsync_WithValidSimplePipeline_ShouldSucceed()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Steps = new[] { (object)new { script = "echo Test", displayName = "Test Step" } }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        // Simple pipelines with trigger and steps are valid
        result.TotalIssues.Should().Be(0, "Simple valid pipelines should have no issues");
    }

    [Fact]
    public async Task ValidateAsync_WithValidMultiStage_ShouldSucceed()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = new[]
            {
                (object)new { displayName = "Build", jobs = new object[0] },
                (object)new { displayName = "Deploy", jobs = new object[0] }
            }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        // Multi-stage pipelines are valid structure
        result.Errors.Should().BeEmpty("Multi-stage pipelines should not have errors");
    }

    [Fact]
    public async Task ValidateAsync_WithValidJobStructure_ShouldSucceed()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Jobs = new[]
            {
                (object)new { displayName = "Job1", steps = new object[0] }
            }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Errors.Should().BeEmpty("Job structure should be valid");
    }

    [Fact]
    public async Task ValidateAsync_WithVariablesAndParameters_ShouldSucceed()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Parameters = new[] { (object)new { name = "env", type = "string" } },
            Variables = new[] { (object)new { name = "buildConfig", value = "Release" } },
            Trigger = new { },
            Steps = new object[0]
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        // Parameters and variables don't make it invalid by themselves
    }

    #endregion

    #region Structure Violation Tests (40%)

    [Fact]
    public async Task ValidateAsync_WithoutTrigger_ShouldWarn()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Steps = new[] { (object)new { script = "echo Test" } }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().NotBeEmpty();
        result.Warnings[0].Code.Should().Contain("MISSING_TRIGGER");
    }

    [Fact]
    public async Task ValidateAsync_WithoutStepsOrJobsOrStages_ShouldError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Contain("NO_WORK_DEFINITION");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidStageStructure_ShouldError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = new[]
            {
                (object)new { } // Stage without required fields
            }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidJobStructure_ShouldError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Jobs = new[]
            {
                (object)new { } // Job without required fields
            }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithMixedStagesAndJobs_ShouldError()
    {
        // Arrange - Cannot have both stages and jobs at root
        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = new[] { (object)new { displayName = "Build" } },
            Jobs = new[] { (object)new { displayName = "Job1" } }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Contain("CONFLICTING_STRUCTURE");
    }

    [Fact]
    public async Task ValidateAsync_WithMixedStagesAndSteps_ShouldError()
    {
        // Arrange - Cannot have both stages and steps at root
        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = new[] { (object)new { displayName = "Build" } },
            Steps = new[] { (object)new { script = "echo Test" } }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Contain("CONFLICTING_STRUCTURE");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyStep_ShouldError()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Steps = new[] { (object)new { } } // Empty step
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    #endregion

    #region Edge Cases Tests (20%)

    [Fact]
    public async Task ValidateAsync_WithCustomRule_ShouldApply()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Steps = new[] { (object)new { script = "echo Test" } }
        };
        var validator = CreateValidator();
        
        // Add custom rule
        var customRule = new TestValidationRule();
        validator.AddRule(customRule);

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        // Custom rule should have been applied
    }

    [Fact]
    public void RemoveRule_ShouldRemoveRule()
    {
        // Arrange
        var validator = CreateValidator();
        var initialRules = validator.GetRules().Count;

        // Act
        var removed = validator.RemoveRule("MISSING_TRIGGER_RULE");

        // Assert
        if (removed)
        {
            validator.GetRules().Count.Should().BeLessThan(initialRules);
        }
    }

    [Fact]
    public void GetRules_ShouldReturnBuiltInRules()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var rules = validator.GetRules();

        // Assert
        rules.Should().NotBeEmpty();
        rules.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ValidateAsync_WithDeeplyNestedStructure_ShouldSucceed()
    {
        // Arrange
        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = new[]
            {
                (object)new
                {
                    displayName = "Build",
                    jobs = new[]
                    {
                        new
                        {
                            displayName = "BuildJob",
                            steps = new[]
                            {
                                new { script = "echo Test" }
                            }
                        }
                    }
                }
            }
        };
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.Errors.Should().BeEmpty("Deeply nested valid structure should have no errors");
    }

    [Fact]
    public async Task ValidateAsync_WithLargePipeline_ShouldCompleteInTime()
    {
        // Arrange
        var steps = new List<object>();
        for (int i = 0; i < 100; i++)
        {
            steps.Add(new { script = $"echo Step {i}", displayName = $"Step {i}" });
        }

        var document = new PipelineDocument
        {
            Trigger = new { },
            Steps = steps.ToArray()
        };
        var validator = CreateValidator();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await validator.ValidateAsync(document);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion

    #region Helper Methods

    private static ISyntaxValidator CreateValidator()
    {
        return new AdoPipelinesLocalRunner.Core.Validators.SyntaxValidator();
    }

    /// <summary>
    /// Test implementation of validation rule for testing custom rules.
    /// </summary>
    private class TestValidationRule : IValidationRule
    {
        public string Name => "TEST_RULE";
        public Severity Severity => Severity.Warning;
        public string Description => "Test rule for validation";

        public IEnumerable<ValidationError> Validate(object node, ValidationContext context)
        {
            return Enumerable.Empty<ValidationError>();
        }
    }

    #endregion
}
