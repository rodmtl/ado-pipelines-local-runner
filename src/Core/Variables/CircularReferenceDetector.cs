using AdoPipelinesLocalRunner.Contracts;
using System.Text.RegularExpressions;

namespace AdoPipelinesLocalRunner.Core.Variables;

/// <summary>
/// Detects circular references in variables using depth-first search.
/// Handles variable substitution patterns $(name) and reports cycle paths.
/// </summary>
public class CircularReferenceDetector
{
    private static readonly Regex VariablePattern = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);
    private List<DetectedCycle> _detectedCycles = new();

    /// <summary>
    /// Analyzes variables for circular references.
    /// </summary>
    public CircularReferenceAnalysis AnalyzeVariables(VariableContext context)
    {
        _detectedCycles.Clear();

        // Build dependency graph from all variable scopes
        var graph = BuildDependencyGraph(context);

        // Detect cycles using DFS
        foreach (var node in graph.Keys)
        {
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            var cyclePath = FindCycleDFS(node, graph, visited, stack);
            if (cyclePath.Count > 0)
            {
                _detectedCycles.Add(new DetectedCycle
                {
                    Path = cyclePath,
                    CycleType = "Variable",
                    Locations = Array.Empty<SourceLocation>()
                });
            }
        }

        return new CircularReferenceAnalysis
        {
            HasCycles = _detectedCycles.Count > 0,
            Cycles = _detectedCycles,
            DependencyGraph = graph
        };
    }

    /// <summary>
    /// Checks if a path contains a cycle (repeated node).
    /// </summary>
    public bool ContainsCycle(IEnumerable<string> path)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in path)
        {
            if (seen.Contains(node))
                return true;
            seen.Add(node);
        }
        return false;
    }

    /// <summary>
    /// Gets all detected cycles.
    /// </summary>
    public IReadOnlyList<DetectedCycle> GetDetectedCycles() => _detectedCycles.AsReadOnly();

    /// <summary>
    /// Builds a dependency graph from all variable scopes.
    /// Returns a map of variable names to their dependencies.
    /// </summary>
    private static Dictionary<string, IReadOnlyList<string>> BuildDependencyGraph(VariableContext context)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Add all variables from all scopes
        AddVariablesToGraph(context.PipelineVariables, graph);
        AddVariablesToGraph(context.StageVariables, graph);
        AddVariablesToGraph(context.JobVariables, graph);
        AddVariablesToGraph(context.StepVariables, graph);

        // Convert to read-only
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in graph)
        {
            result[kvp.Key] = kvp.Value.AsReadOnly();
        }
        return result;
    }

    private static void AddVariablesToGraph(
        IReadOnlyDictionary<string, object>? variables,
        Dictionary<string, List<string>> graph)
    {
        if (variables == null) return;

        foreach (var kvp in variables)
        {
            if (!graph.TryGetValue(kvp.Key, out var deps))
            {
                deps = new List<string>();
                graph[kvp.Key] = deps;
            }

            // Extract variable references from the value
            if (kvp.Value is string strValue)
            {
                var matches = VariablePattern.Matches(strValue);
                foreach (Match match in matches)
                {
                    var depName = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(depName) && !deps.Contains(depName))
                        deps.Add(depName);
                }
            }
        }
    }

    /// <summary>
    /// Performs DFS to find a cycle starting from a node.
    /// Returns the cycle path if found, empty list otherwise.
    /// </summary>
    private static List<string> FindCycleDFS(
        string node,
        Dictionary<string, IReadOnlyList<string>> graph,
        HashSet<string> globalVisited,
        Stack<string> path,
        int maxDepth = 20)
    {
        if (path.Count > maxDepth)
            return new List<string>(); // Max depth to prevent infinite recursion

        if (path.Contains(node))
        {
            // Found cycle - extract path
            var cyclePath = new List<string>();
            var foundStart = false;
            foreach (var item in path)
            {
                if (item.Equals(node, StringComparison.OrdinalIgnoreCase))
                    foundStart = true;
                if (foundStart)
                    cyclePath.Add(item);
            }
            cyclePath.Add(node); // Complete the cycle
            return cyclePath;
        }

        path.Push(node);

        if (!graph.TryGetValue(node, out var dependencies))
        {
            path.Pop();
            globalVisited.Add(node);
            return new List<string>();
        }

        foreach (var dep in dependencies)
        {
            var cycle = FindCycleDFS(dep, graph, globalVisited, path, maxDepth);
            if (cycle.Count > 0)
            {
                path.Pop();
                return cycle;
            }
        }

        path.Pop();
        globalVisited.Add(node);
        return new List<string>();
    }
}

/// <summary>
/// Result of circular reference analysis.
/// </summary>
public record CircularReferenceAnalysis
{
    public bool HasCycles { get; init; }
    public IReadOnlyList<DetectedCycle> Cycles { get; init; } = new List<DetectedCycle>();
    public IReadOnlyDictionary<string, IReadOnlyList<string>> DependencyGraph { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}

/// <summary>
/// Information about a detected cycle.
/// </summary>
public record DetectedCycle
{
    public IReadOnlyList<string> Path { get; init; } = new List<string>();
    public string CycleType { get; init; } = "Variable"; // "Variable" or "Template"
    public IReadOnlyList<SourceLocation> Locations { get; init; } = new List<SourceLocation>();
}
