using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Variables;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Variables;

public class VariableScopeHierarchyTests
{
    private static VariableProcessor CreateProcessor() => new();

    [Fact]
    public void ResolveExpression_JobOverridesPipeline_InJobScope()
    {
        var processor = CreateProcessor();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> { { "buildNum", "1" } },
            JobVariables = new Dictionary<string, object> { { "buildNum", "2" } },
            Scope = VariableScope.Job,
            FailOnUnresolved = true
        };

        var result = processor.ResolveExpression("$(buildNum)", context);

        Assert.Equal("2", result);
    }

    [Fact]
    public void ResolveExpression_StepOverridesJobAndStage_InStepScope()
    {
        var processor = CreateProcessor();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> { { "v", "P" } },
            StageVariables = new Dictionary<string, object> { { "v", "S" } },
            JobVariables = new Dictionary<string, object> { { "v", "J" } },
            StepVariables = new Dictionary<string, object> { { "v", "T" } },
            Scope = VariableScope.Step
        };

        var result = processor.ResolveExpression("$(v)", context);
        Assert.Equal("T", result);
    }

    [Fact]
    public void ResolveExpression_StageFallsBackToPipeline_WhenNoStageVar()
    {
        var processor = CreateProcessor();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> { { "cfg", "global" } },
            Scope = VariableScope.Stage
        };

        var result = processor.ResolveExpression("$(cfg)", context);
        Assert.Equal("global", result);
    }

    [Fact]
    public void ResolveExpression_StepVarNotVisibleInStage_NoLeakage()
    {
        var processor = CreateProcessor();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            StepVariables = new Dictionary<string, object> { { "stepOnly", "secret" } },
            Scope = VariableScope.Stage,
            FailOnUnresolved = false
        };

        var result = processor.ResolveExpression("$(stepOnly)", context);
        // When unresolved is allowed, original expression is returned
        Assert.Equal("$(stepOnly)", result);
    }

    [Fact]
    public void ExpandVariables_RespectsHierarchy_InCompositeText()
    {
        var processor = CreateProcessor();
        var context = new VariableContext
        {
            SystemVariables = new Dictionary<string, object>(),
            PipelineVariables = new Dictionary<string, object> { { "name", "global" } },
            JobVariables = new Dictionary<string, object> { { "name", "job" } },
            Scope = VariableScope.Step
        };

        var text = "Hello $(name)";
        var result = processor.ExpandVariables(text, context);
        Assert.Equal("Hello job", result);
    }
}
