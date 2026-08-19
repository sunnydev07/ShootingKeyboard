using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShootingKeyboard.Models;

/// <summary>
/// Root configuration object for the application. Serialized to JSON at %AppData%/ShootingKeyboard/config.json
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// Master volume (0.0 - 1.0)
    /// </summary>
    [JsonPropertyName("masterVolume")]
    public float MasterVolume { get; set; } = 0.7f;

    /// <summary>
    /// Global mute toggle
    /// </summary>
    [JsonPropertyName("isMuted")]
    public bool IsMuted { get; set; } = false;

    /// <summary>
    /// Whether the keyboard hook is active
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// ID of the active sound pack
    /// </summary>
    [JsonPropertyName("activePackId")]
    public string ActivePackId { get; set; } = "warzone";

    /// <summary>
    /// Whether on-screen overlay is enabled
    /// </summary>
    [JsonPropertyName("overlayEnabled")]
    public bool OverlayEnabled { get; set; } = true;

    /// <summary>
    /// Whether performance mode is active (reduces CPU/visuals)
    /// </summary>
    [JsonPropertyName("performanceMode")]
    public bool PerformanceMode { get; set; } = false;

    /// <summary>
    /// Whether to start with Windows
    /// </summary>
    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// Combo window in milliseconds (how long to wait between keystrokes to continue combo)
    /// </summary>
    [JsonPropertyName("comboWindowMs")]
    public int ComboWindowMs { get; set; } = 400;

    /// <summary>
    /// Per-key explicit sound bindings (VirtualKeyCode -> soundId)
    /// </summary>
    [JsonPropertyName("keyBindings")]
    public Dictionary<int, string> KeyBindings { get; set; } = new();

    /// <summary>
    /// Per-group sound bindings (GroupName -> soundId)
    /// </summary>
    [JsonPropertyName("groupBindings")]
    public Dictionary<string, string> GroupBindings { get; set; } = new();

    /// <summary>
    /// Per-group volume overrides (GroupName -> volume 0.0-1.0)
    /// </summary>
    [JsonPropertyName("groupVolumeOverrides")]
    public Dictionary<string, float> GroupVolumeOverrides { get; set; } = new();

    /// <summary>
    /// Per-key volume overrides (VirtualKeyCode -> volume 0.0-1.0)
    /// </summary>
    [JsonPropertyName("keyVolumeOverrides")]
    public Dictionary<int, float> KeyVolumeOverrides { get; set; } = new();

    /// <summary>
    /// Keystroke playback filtering and repeat cooldown settings
    /// </summary>
    [JsonPropertyName("playbackFilter")]
    public PlaybackFilterConfig PlaybackFilter { get; set; } = new();

    /// <summary>
    /// ID of the currently active profile
    /// </summary>
    [JsonPropertyName("activeProfileId")]
    public string ActiveProfileId { get; set; } = "default";

    /// <summary>
    /// Available configuration profiles
    /// </summary>
    [JsonPropertyName("profiles")]
    public List<AppProfile> Profiles { get; set; } = new();

    /// <summary>
    /// Per-application playback and override rules
    /// </summary>
    [JsonPropertyName("appRules")]
    public List<AppRule> AppRules { get; set; } = new();

    /// <summary>
    /// Visual overlay customization settings
    /// </summary>
    [JsonPropertyName("overlay")]
    public OverlayConfig Overlay { get; set; } = new();

    /// <summary>
    /// Scheduled quiet hours configuration
    /// </summary>
    [JsonPropertyName("quietHours")]
    public QuietHoursConfig QuietHours { get; set; } = new();

    /// <summary>
    /// Creates a default configuration
    /// </summary>
    public static AppConfig CreateDefault() => new();

    /// <summary>
    /// Validates and clamps config values
    /// </summary>
    public void Validate()
    {
        MasterVolume = Math.Clamp(MasterVolume, 0f, 1f);
        ComboWindowMs = Math.Clamp(ComboWindowMs, 50, 2000);

        if (GroupVolumeOverrides != null)
        {
            var invalidGroups = GroupVolumeOverrides.Keys
                .Where(k => !KeyGroups.All.Contains(k))
                .ToList();
            foreach (var g in invalidGroups)
            {
                GroupVolumeOverrides.Remove(g);
            }

            foreach (var (k, v) in GroupVolumeOverrides.ToList())
            {
                GroupVolumeOverrides[k] = Math.Clamp(v, 0f, 1f);
            }
        }
        else
        {
            GroupVolumeOverrides = new();
        }

        if (KeyVolumeOverrides != null)
        {
            foreach (var (k, v) in KeyVolumeOverrides.ToList())
            {
                KeyVolumeOverrides[k] = Math.Clamp(v, 0f, 1f);
            }
        }
        else
        {
            KeyVolumeOverrides = new();
        }

        if (PlaybackFilter != null)
        {
            PlaybackFilter.GlobalCooldownMs = Math.Clamp(PlaybackFilter.GlobalCooldownMs, 0, 1000);

            if (PlaybackFilter.GroupCooldownMs != null)
            {
                var invalidKeys = PlaybackFilter.GroupCooldownMs.Keys
                    .Where(k => !KeyGroups.All.Contains(k))
                    .ToList();
                foreach (var k in invalidKeys)
                {
                    PlaybackFilter.GroupCooldownMs.Remove(k);
                }

                foreach (var (k, v) in PlaybackFilter.GroupCooldownMs.ToList())
                {
                    PlaybackFilter.GroupCooldownMs[k] = Math.Clamp(v, 0, 5000);
                }
            }
            else
            {
                PlaybackFilter.GroupCooldownMs = new();
            }

            if (PlaybackFilter.KeyCooldownMs != null)
            {
                foreach (var (k, v) in PlaybackFilter.KeyCooldownMs.ToList())
                {
                    PlaybackFilter.KeyCooldownMs[k] = Math.Clamp(v, 0, 5000);
                }
            }
            else
            {
                PlaybackFilter.KeyCooldownMs = new();
            }
        }
        else
        {
            PlaybackFilter = new();
        }

        if (Profiles == null)
        {
            Profiles = new List<AppProfile>();
        }

        if (Profiles.Count == 0)
        {
            Profiles.Add(new AppProfile
            {
                Id = "default",
                Name = "Default",
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
            });
        }

        if (string.IsNullOrEmpty(ActiveProfileId) || !Profiles.Any(p => p.Id == ActiveProfileId))
        {
            ActiveProfileId = Profiles[0].Id;
        }

        if (AppRules == null)
        {
            AppRules = new List<AppRule>();
        }

        if (Overlay == null)
        {
            Overlay = new OverlayConfig();
        }

        var validPositions = new[] { "TopCenter", "TopLeft", "TopRight", "BottomCenter" };
        if (string.IsNullOrEmpty(Overlay.ComboPosition) || !validPositions.Contains(Overlay.ComboPosition, StringComparer.OrdinalIgnoreCase))
        {
            Overlay.ComboPosition = "TopCenter";
        }

        Overlay.Scale = Math.Clamp(Overlay.Scale, 0.5, 2.0);

        if (string.IsNullOrWhiteSpace(Overlay.RippleColor) || !Overlay.RippleColor.StartsWith("#"))
        {
            Overlay.RippleColor = "#FFA500";
        }

        if (QuietHours == null)
        {
            QuietHours = new QuietHoursConfig();
        }
    }
}

/// <summary>
/// Represents a sound pack descriptor (loaded from pack.json)
/// </summary>
public sealed class SoundPack
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("sounds")]
    public List<SoundEntry> Sounds { get; set; } = new();

    [JsonPropertyName("defaults")]
    public PackDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Gets a sound entry by ID
    /// </summary>
    public SoundEntry? GetSound(string soundId) => Sounds.FirstOrDefault(s => s.Id == soundId);
}

/// <summary>
/// Individual sound entry within a pack
/// </summary>
public sealed class SoundEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("volume")]
    public float Volume { get; set; } = 1.0f;

    [JsonPropertyName("variants")]
    public List<string> Variants { get; set; } = new();

    [JsonPropertyName("isComboVariant")]
    public bool IsComboVariant { get; set; } = false;

    [JsonPropertyName("comboTier")]
    public int ComboTier { get; set; } = 0;
}

/// <summary>
/// Default settings for a sound pack
/// </summary>
public sealed class PackDefaults
{
    [JsonPropertyName("volume")]
    public float Volume { get; set; } = 1.0f;

    [JsonPropertyName("comboWindowMs")]
    public int ComboWindowMs { get; set; } = 400;
}

/// <summary>
/// Logical key groups for bulk binding
/// </summary>
public static class KeyGroups
{
    public const string Letters = "Letters";
    public const string Numbers = "Numbers";
    public const string WASD = "WASD";
    public const string Arrows = "Arrows";
    public const string FKeys = "FKeys";
    public const string Space = "Space";
    public const string Enter = "Enter";
    public const string Modifiers = "Modifiers";
    public const string Punctuation = "Punctuation";
    public const string Navigation = "Navigation";
    public const string Numpad = "Numpad";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Letters, Numbers, WASD, Arrows, FKeys, Space, Enter, Modifiers, Punctuation, Navigation, Numpad
    };

    /// <summary>
    /// Determines which group a virtual key code belongs to
    /// </summary>
    public static string? GetGroupForKey(int virtualKeyCode)
    {
        return virtualKeyCode switch
        {
            // Letters A-Z (0x41-0x5A)
            >= 0x41 and <= 0x5A => virtualKeyCode is 0x57 or 0x41 or 0x53 or 0x44 ? WASD : Letters, // W=0x57, A=0x41, S=0x53, D=0x44

            // Numbers 0-9 (0x30-0x39)
            >= 0x30 and <= 0x39 => Numbers,

            // F1-F24 (0x70-0x87)
            >= 0x70 and <= 0x87 => FKeys,

            // Arrow keys (0x25-0x28)
            >= 0x25 and <= 0x28 => Arrows,

            // Space (0x20)
            0x20 => Space,

            // Enter (0x0D)
            0x0D => Enter,

            // Modifiers
            0x10 or 0x11 or 0x12 or 0x5B or 0x5C or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 => Modifiers, // Shift, Ctrl, Alt, Win, L/R variants

            // Navigation keys
            0x21 or 0x22 or 0x23 or 0x24 or 0x2D or 0x2E => Navigation, // PgUp, PgDn, End, Home, Ins, Del

            // Numpad (0x60-0x6F)
            >= 0x60 and <= 0x6F => Numpad,

            // Punctuation and others
            _ => Punctuation
        };
    }
}