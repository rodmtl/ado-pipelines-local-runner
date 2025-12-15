using System.Diagnostics;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Validators;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Performance;

/// <summary>
/// Performance benchmarks for validating NFR-1 compliance.
/// Tests verify that syntax validation completes within performance targets.
/// </summary>
public class PerformanceBenchmarks
{
    /// <summary>
    /// NFR-1: Syntax validation of typical pipelines (< 500 lines) completes in < 1s.
    /// </summary>
    [Fact]
    public async Task SyntaxValidation_LargePipeline_CompletesBelowThreshold()
    {
        // Arrange
        var document = GenerateLargePipelineDocument(500);
        var validator = new SyntaxValidator();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await validator.ValidateAsync(document);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            because: "Syntax validation must complete within 1 second for typical pipelines");
    }

    /// <summary>
    /// NFR-1: Syntax validation of very large pipelines (1000 lines) completes quickly.
    /// </summary>
    [Fact]
    public async Task SyntaxValidation_VeryLargePipeline_Completes()
    {
        // Arrange
        var document = GenerateLargePipelineDocument(1000);
        var validator = new SyntaxValidator();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await validator.ValidateAsync(document);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000,
            because: "Very large pipelines should still validate within reasonable time");
    }

    /// <summary>
    /// Performance test: Multi-stage pipeline with many jobs.
    /// </summary>
    [Fact]
    public async Task SyntaxValidation_MultiStageWithManyJobs_PerformanceAcceptable()
    {
        // Arrange
        var jobs = Enumerable.Range(1, 50)
            .Select(i => (object)new { displayName = $"Job{i}", steps = new object[0] })
            .ToList();

        var document = new PipelineDocument
        {
            Trigger = new { },
            Jobs = jobs
        };

        var validator = new SyntaxValidator();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await validator.ValidateAsync(document);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            because: "50 jobs should validate quickly without performance issues");
    }

    /// <summary>
    /// Performance test: Deeply nested stages and jobs.
    /// </summary>
    [Fact]
    public async Task SyntaxValidation_DeeplyNestedStructure_PerformanceAcceptable()
    {
        // Arrange
        var stages = Enumerable.Range(1, 10)
            .Select(stageIdx => (object)new
            {
                displayName = $"Stage{stageIdx}",
                jobs = Enumerable.Range(1, 5)
                    .Select(jobIdx => (object)new
                    {
                        displayName = $"Job{jobIdx}",
                        steps = new object[0]
                    })
                    .ToList()
            })
            .ToList();

        var document = new PipelineDocument
        {
            Trigger = new { },
            Stages = stages
        };

        var validator = new SyntaxValidator();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await validator.ValidateAsync(document);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            because: "Deeply nested structures should validate without performance degradation");
    }

    /// <summary>
    /// Helper: Generate a large pipeline document with specified line count.
    /// </summary>
    private static PipelineDocument GenerateLargePipelineDocument(int estimatedLineCount)
    {
        // Each step roughly adds 3-4 lines in YAML, so target jobs accordingly
        var jobCount = estimatedLineCount / 30;
        var stepsPerJob = 10;

        var jobs = Enumerable.Range(1, jobCount)
            .Select(j => (object)new
            {
                displayName = $"BuildJob{j}",
                steps = Enumerable.Range(1, stepsPerJob)
                    .Select(s => (object)new
                    {
                        script = $"echo 'Step {s} in Job {j}'",
                        displayName = $"Step{s}"
                    })
                    .ToList()
            })
            .ToList();

        return new PipelineDocument
        {
            Name = "LargePipeline",
            Trigger = new { },
            Jobs = jobs
        };
    }
}
