using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShootingKeyboard.Models;

public sealed class AppProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("masterVolume")]
    public float MasterVolume { get; set; } = 0.7f;

    [JsonPropertyName("isMuted")]
    public bool IsMuted { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("activePackId")]
    public string ActivePackId { get; set; } = "warzone";

    [JsonPropertyName("overlayEnabled")]
    public bool OverlayEnabled { get; set; } = true;

    [JsonPropertyName("performanceMode")]
    public bool PerformanceMode { get; set; }

    [JsonPropertyName("comboWindowMs")]
    public int ComboWindowMs { get; set; } = 400;

    [JsonPropertyName("keyBindings")]
    public Dictionary<int, string> KeyBindings { get; set; } = new();

    [JsonPropertyName("groupBindings")]
    public Dictionary<string, string> GroupBindings { get; set; } = new();

    [JsonPropertyName("groupVolumeOverrides")]
    public Dictionary<string, float> GroupVolumeOverrides { get; set; } = new();

    [JsonPropertyName("keyVolumeOverrides")]
    public Dictionary<int, float> KeyVolumeOverrides { get; set; } = new();

    [JsonPropertyName("playbackFilter")]
    public PlaybackFilterConfig PlaybackFilter { get; set; } = new();

    public AppProfile Clone(string newId, string newName)
    {
        return new AppProfile
        {
            Id = newId,
            Name = newName,
            MasterVolume = MasterVolume,
            IsMuted = IsMuted,
            IsEnabled = IsEnabled,
            ActivePackId = ActivePackId,
            OverlayEnabled = OverlayEnabled,
            PerformanceMode = PerformanceMode,
            ComboWindowMs = ComboWindowMs,
            KeyBindings = new Dictionary<int, string>(KeyBindings ?? new()),
            GroupBindings = new Dictionary<string, string>(GroupBindings ?? new()),
            GroupVolumeOverrides = new Dictionary<string, float>(GroupVolumeOverrides ?? new()),
            KeyVolumeOverrides = new Dictionary<int, float>(KeyVolumeOverrides ?? new()),
            PlaybackFilter = new PlaybackFilterConfig
            {
                IgnoreKeyRepeats = PlaybackFilter?.IgnoreKeyRepeats ?? true,
                GlobalCooldownMs = PlaybackFilter?.GlobalCooldownMs ?? 20,
                GroupCooldownMs = new Dictionary<string, int>(PlaybackFilter?.GroupCooldownMs ?? new()),
                KeyCooldownMs = new Dictionary<int, int>(PlaybackFilter?.KeyCooldownMs ?? new())
            }
        };
    }
}
