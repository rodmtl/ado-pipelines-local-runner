using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Variables;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Variables;

public class CircularReferenceDetectorTests
{
    [Fact]
    public void DetectVariableCycles_WithDirectCycle_ReportsIt()
    {
        // Arrange: A -> B -> A
        var detector = new CircularReferenceDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "varA", "$(varB)" },
                { "varB", "$(varA)" }
            },
            Scope = VariableScope.Pipeline
        };

        // Act
        var analysis = detector.AnalyzeVariables(context);

        // Assert
        analysis.HasCycles.Should().BeTrue();
        analysis.Cycles.Should().NotBeEmpty();
    }

    [Fact]
    public void DetectVariableCycles_WithoutCycles_ReportsNone()
    {
        // Arrange
        var detector = new CircularReferenceDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "varA", "value-a" },
                { "varB", "value-b" }
            },
            Scope = VariableScope.Pipeline
        };

        // Act
        var analysis = detector.AnalyzeVariables(context);

        // Assert
        analysis.HasCycles.Should().BeFalse();
        analysis.Cycles.Should().BeEmpty();
    }

    [Fact]
    public void DetectVariableCycles_WithIndirectCycle_ReportsIt()
    {
        // Arrange: A -> B -> C -> A
        var detector = new CircularReferenceDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "varA", "$(varB)" },
                { "varB", "$(varC)" },
                { "varC", "$(varA)" }
            },
            Scope = VariableScope.Pipeline
        };

        // Act
        var analysis = detector.AnalyzeVariables(context);

        // Assert
        analysis.HasCycles.Should().BeTrue();
        analysis.Cycles.Should().NotBeEmpty();
    }

    [Fact]
    public void ContainsCycle_ChecksPath()
    {
        // Arrange
        var detector = new CircularReferenceDetector();
        var cyclePath = new[] { "A", "B", "C", "A" };

        // Act
        var result = detector.ContainsCycle(cyclePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsCycle_NoCyclePath_ReturnsFalse()
    {
        // Arrange
        var detector = new CircularReferenceDetector();
        var path = new[] { "A", "B", "C" };

        // Act
        var result = detector.ContainsCycle(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetDetectedCycles_ReturnsCycles()
    {
        // Arrange
        var detector = new CircularReferenceDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object>
            {
                { "x", "$(y)" },
                { "y", "$(x)" }
            },
            Scope = VariableScope.Pipeline
        };
        detector.AnalyzeVariables(context);

        // Act
        var cycles = detector.GetDetectedCycles();

        // Assert
        cycles.Should().NotBeEmpty();
    }
}
