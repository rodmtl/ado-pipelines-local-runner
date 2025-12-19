using AdoPipelinesLocalRunner.Contracts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AdoPipelinesLocalRunner.Core.Variables;

public interface IVariableFileLoader
{
    IReadOnlyDictionary<string, object> Load(IEnumerable<string> files, string? baseDir = null, List<ValidationError>? errors = null);
}

public class VariableFileLoader : IVariableFileLoader
{
    private readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public IReadOnlyDictionary<string, object> Load(IEnumerable<string> files, string? baseDir = null, List<ValidationError>? errors = null)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var path = baseDir != null && !Path.IsPathRooted(file) ? Path.Combine(baseDir, file) : file;
            if (!File.Exists(path))
            {
                errors?.Add(new ValidationError
                {
                    Code = "VARIABLE_FILE_NOT_FOUND",
                    Message = $"Variable file not found: {path}",
                    Severity = Severity.Error,
                    Location = new SourceLocation { FilePath = path, Line = 0, Column = 0 }
                });
                continue;
            }

            try
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                var content = File.ReadAllText(path);
                Dictionary<string, object> toMerge = ext == ".json" ? ParseJsonVariables(content) : ParseYamlVariables(content);
                foreach (var kv in toMerge)
                    result[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                errors?.Add(new ValidationError
                {
                    Code = "VARIABLE_FILE_INVALID",
                    Message = $"Failed to read variables from {path}: {ex.Message}",
                    Severity = Severity.Error,
                    Location = new SourceLocation { FilePath = path, Line = 0, Column = 0 }
                });
            }
        }
        return result;
    }

    private Dictionary<string, object> ParseYamlVariables(string yaml)
    {
        var parsed = _yaml.Deserialize<object?>(yaml);
        return ParseYamlObject(parsed);
    }

    private static Dictionary<string, object> ParseYamlObject(object? yamlObject)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (yamlObject is null) return result;

        if (yamlObject is Dictionary<object, object> dict)
        {
            if (dict.TryGetValue("variables", out var varsNode) && varsNode is IEnumerable<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<object, object> vdict)
                    {
                        var name = vdict.TryGetValue("name", out var n) ? n?.ToString() : null;
                        if (!string.IsNullOrEmpty(name) && vdict.TryGetValue("value", out var val))
                        {
                            result[name!] = val ?? string.Empty;
                        }
                    }
                }
            }
            else
            {
                foreach (var kv in dict)
                {
                    var key = kv.Key?.ToString();
                    if (!string.IsNullOrEmpty(key)) result[key!] = kv.Value ?? string.Empty;
                }
            }
        }
        else if (yamlObject is Dictionary<string, object> sdict)
        {
            if (sdict.TryGetValue("variables", out var vars) && vars is IEnumerable<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<object, object> vdict)
                    {
                        var name = vdict.TryGetValue("name", out var n) ? n?.ToString() : null;
                        if (!string.IsNullOrEmpty(name) && vdict.TryGetValue("value", out var val))
                        {
                            result[name!] = val ?? string.Empty;
                        }
                    }
                    else if (item is Dictionary<string, object> vsdict)
                    {
                        if (vsdict.TryGetValue("name", out var nobj) && nobj is not null && vsdict.TryGetValue("value", out var vobj))
                        {
                            result[nobj.ToString()!] = vobj ?? string.Empty;
                        }
                    }
                }
            }
            else
            {
                foreach (var kv in sdict)
                {
                    result[kv.Key] = kv.Value ?? string.Empty;
                }
            }
        }
        return result;
    }

    private static Dictionary<string, object> ParseJsonVariables(string json)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("variables", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (!string.IsNullOrEmpty(name))
                    {
                        object value = el.TryGetProperty("value", out var v) ? v.ToString()! : string.Empty;
                        result[name!] = value;
                    }
                }
            }
        }
        return result;
    }
}
