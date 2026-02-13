using System.Text.Json;
using System.Text.Json.Serialization;

namespace GistAutomation.Utils;

public record RunState
{
    [JsonPropertyName("lastRun")]
    public string LastRun { get; set; } = "";

    [JsonPropertyName("last-compliance-check-version")]
    public string LastComplianceVersion { get; set; } = "";
}

public static class RunStateManager
{
    private const string ConfigFileName = "config.json";

    public static RunState Load()
    {
        var configPath = FindConfigFile();
        if (string.IsNullOrEmpty(configPath))
            return new RunState();

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<RunState>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) ?? new RunState();
    }

    public static void Save(RunState state)
    {
        var configPath = FindConfigFile()
            ?? Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(configPath, json);
    }

    private static string? FindConfigFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, ConfigFileName);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        var workingDir = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
        return File.Exists(workingDir) ? workingDir : null;
    }
}
