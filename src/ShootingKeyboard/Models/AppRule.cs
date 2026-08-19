using System.Text.Json.Serialization;

namespace ShootingKeyboard.Models;

public sealed class ForegroundAppInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public string MainWindowTitle { get; set; } = string.Empty;
}

public sealed class AppRule
{
    [JsonPropertyName("processName")]
    public string ProcessName { get; set; } = string.Empty;

    [JsonPropertyName("disableSounds")]
    public bool DisableSounds { get; set; }

    [JsonPropertyName("muteOnly")]
    public bool MuteOnly { get; set; }

    [JsonPropertyName("profileIdOverride")]
    public string? ProfileIdOverride { get; set; }

    [JsonPropertyName("soundPackIdOverride")]
    public string? SoundPackIdOverride { get; set; }
}

public sealed class AppRuleDecision
{
    public bool ShouldPlay { get; set; } = true;
    public string? ProfileIdOverride { get; set; }
    public string? SoundPackIdOverride { get; set; }
    public string Reason { get; set; } = "no-rule";
}
