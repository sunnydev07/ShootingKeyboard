using System;

namespace ShootingKeyboard.Models;

public sealed class RuntimeDiagnosticsSnapshot
{
    public DateTimeOffset CreatedAt { get; set; }
    public bool KeyboardHookRunning { get; set; }
    public bool AppEnabled { get; set; }
    public bool Muted { get; set; }
    public string ActivePackId { get; set; } = string.Empty;
    public string ActivePackName { get; set; } = string.Empty;
    public int AvailablePackCount { get; set; }
    public int LoadedSoundCount { get; set; }
    public string ConfigPath { get; set; } = string.Empty;
    public string LastKey { get; set; } = string.Empty;
    public string LastResolvedSoundId { get; set; } = string.Empty;
    public string LastPlayedSoundId { get; set; } = string.Empty;
    public string LastPlaybackResult { get; set; } = string.Empty;
    public DateTimeOffset? LastEventAt { get; set; }
}
