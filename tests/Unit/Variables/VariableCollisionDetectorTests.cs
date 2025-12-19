using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Variables;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Variables;

public class VariableCollisionDetectorTests
{
    [Fact]
    public void DetectCollisions_WithSameVariableAcrossScopes_ReportsCollision()
    {
        // Arrange
        var detector = new VariableCollisionDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> { { "buildNum", "1" } },
            StageVariables = new Dictionary<string, object> { { "buildNum", "2" } },
            JobVariables = new Dictionary<string, object> { { "buildNum", "3" } },
            Scope = VariableScope.Job
        };

        // Act
        var result = detector.Analyze(context);

        // Assert
        result.HasCollisions.Should().BeTrue();
        result.Collisions.Should().HaveCount(1);
        var collision = result.Collisions[0];
        collision.Name.Should().Be("buildNum");
        collision.Values.Should().HaveCount(3);
        collision.Values.Should().ContainKeys("Pipeline", "Stage", "Job");
    }

    [Fact]
    public void DetectCollisions_WithNoConflicts_ReturnsEmpty()
    {
        // Arrange
        var detector = new VariableCollisionDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> { { "a", "1" } },
            StageVariables = new Dictionary<string, object> { { "b", "2" } },
            JobVariables = new Dictionary<string, object> { { "c", "3" } },
            Scope = VariableScope.Job
        };

        // Act
        var result = detector.Analyze(context);

        // Assert
        result.HasCollisions.Should().BeFalse();
        result.Collisions.Should().BeEmpty();
    }

    [Fact]
    public void DetectCollisions_MultipleCollisions_ReportsAll()
    {
        // Arrange
        var detector = new VariableCollisionDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> 
            { 
                { "var1", "p1" }, 
                { "var2", "p2" } 
            },
            StageVariables = new Dictionary<string, object> 
            { 
                { "var1", "s1" }, 
                { "var2", "s2" } 
            },
            Scope = VariableScope.Stage
        };

        // Act
        var result = detector.Analyze(context);

        // Assert
        result.HasCollisions.Should().BeTrue();
        result.Collisions.Should().HaveCount(2);
        result.Collisions.Select(c => c.Name).Should().Contain(new[] { "var1", "var2" });
    }

    [Fact]
    public void GetCollisions_ReturnsAllDetected()
    {
        // Arrange
        var detector = new VariableCollisionDetector();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> { { "x", "1" } },
            JobVariables = new Dictionary<string, object> { { "x", "2" } },
            Scope = VariableScope.Job
        };
        detector.Analyze(context);

        // Act
        var collisions = detector.GetCollisions();

        // Assert
        collisions.Should().HaveCount(1);
        collisions[0].Name.Should().Be("x");
    }
}
