using AdoPipelinesLocalRunner.Contracts;
using System.Collections.Generic;

namespace AdoPipelinesLocalRunner.Core.Variables;

/// <summary>
/// Detects and reports variable collisions (same name at multiple scope levels).
/// </summary>
public class VariableCollisionDetector
{
    private List<VariableCollision> _detectedCollisions = new();

    /// <summary>
    /// Analyzes variables in a context for naming collisions across scopes.
    /// </summary>
    public CollisionAnalysisResult Analyze(VariableContext context)
    {
        _detectedCollisions.Clear();

        // Collect all variable names and their scope assignments
        var varsByName = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        AddVariablesToMap(context.PipelineVariables, "Pipeline", varsByName);
        AddVariablesToMap(context.StageVariables, "Stage", varsByName);
        AddVariablesToMap(context.JobVariables, "Job", varsByName);
        AddVariablesToMap(context.StepVariables, "Step", varsByName);

        // Detect collisions (same name in multiple scopes)
        foreach (var kvp in varsByName)
        {
            if (kvp.Value.Count > 1)
            {
                _detectedCollisions.Add(new VariableCollision
                {
                    Name = kvp.Key,
                    Values = kvp.Value,
                    Severity = CollisionSeverity.Warning
                });
            }
        }

        return new CollisionAnalysisResult
        {
            HasCollisions = _detectedCollisions.Count > 0,
            Collisions = _detectedCollisions,
            Severity = _detectedCollisions.Count > 0 ? CollisionSeverity.Warning : CollisionSeverity.Info
        };
    }

    /// <summary>
    /// Gets all detected collisions from the last analysis.
    /// </summary>
    public IReadOnlyList<VariableCollision> GetCollisions() => _detectedCollisions.AsReadOnly();

    private static void AddVariablesToMap(
        IReadOnlyDictionary<string, object>? variables,
        string scope,
        Dictionary<string, Dictionary<string, object>> map)
    {
        if (variables == null) return;

        foreach (var kvp in variables)
        {
            if (!map.TryGetValue(kvp.Key, out var scopes))
            {
                scopes = new Dictionary<string, object>();
                map[kvp.Key] = scopes;
            }

            scopes[scope] = kvp.Value;
        }
    }
}

/// <summary>
/// Result of collision analysis.
/// </summary>
public record CollisionAnalysisResult
{
    public bool HasCollisions { get; init; }
    public IReadOnlyList<VariableCollision> Collisions { get; init; } = new List<VariableCollision>();
    public CollisionSeverity Severity { get; init; } = CollisionSeverity.Info;
}

/// <summary>
/// Represents a variable collision across scopes.
/// </summary>
public record VariableCollision
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> Values { get; init; } = new Dictionary<string, object>();
    public CollisionSeverity Severity { get; init; } = CollisionSeverity.Info;
    public SourceLocation? PrimaryLocation { get; init; }
    public IReadOnlyList<SourceLocation>? ConflictLocations { get; init; }
}

/// <summary>
/// Severity level of a collision.
/// </summary>
public enum CollisionSeverity
{
    Info,
    Warning,
    Error
}
