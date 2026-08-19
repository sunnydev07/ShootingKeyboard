using System;
using System.Windows.Input;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class RuntimeDiagnosticsService : IRuntimeDiagnosticsService
{
    private readonly object _lock = new();
    private string _lastKey = string.Empty;
    private string _lastResolvedSoundId = string.Empty;
    private string _lastPlayedSoundId = string.Empty;
    private string _lastPlaybackResult = string.Empty;
    private DateTimeOffset? _lastEventAt;

    public void RecordKeyEvent(KeyEvent keyEvent)
    {
        lock (_lock)
        {
            _lastEventAt = DateTimeOffset.UtcNow;
            var keyName = KeyInterop.KeyFromVirtualKey(keyEvent.KeyCode).ToString();
            _lastKey = $"{keyName} (0x{keyEvent.KeyCode:X2}) [{(keyEvent.IsPressed ? "Down" : "Up")}]";
        }
    }

    public void RecordResolvedSound(int keyCode, string? soundId)
    {
        lock (_lock)
        {
            _lastResolvedSoundId = soundId ?? "(none)";
        }
    }

    public void RecordPlayback(string soundId, bool played, string reason)
    {
        lock (_lock)
        {
            if (played)
            {
                _lastPlayedSoundId = soundId;
                _lastPlaybackResult = "Success";
            }
            else
            {
                _lastPlayedSoundId = string.IsNullOrEmpty(soundId) ? "(none)" : soundId;
                _lastPlaybackResult = $"Failed: {reason}";
            }
        }
    }

    public RuntimeDiagnosticsSnapshot CreateSnapshot(
        AppConfig config,
        IKeyboardHook keyboardHook,
        IAudioEngine audioEngine,
        ISoundPackManager soundPackManager,
        string configPath)
    {
        lock (_lock)
        {
            var activePack = soundPackManager?.ActivePack;
            return new RuntimeDiagnosticsSnapshot
            {
                CreatedAt = DateTimeOffset.UtcNow,
                KeyboardHookRunning = keyboardHook?.IsRunning ?? false,
                AppEnabled = config?.IsEnabled ?? false,
                Muted = config?.IsMuted ?? false,
                ActivePackId = config?.ActivePackId ?? string.Empty,
                ActivePackName = activePack?.Name ?? "(none)",
                AvailablePackCount = soundPackManager?.GetPacks()?.Count ?? 0,
                LoadedSoundCount = audioEngine?.GetLoadedSoundIds()?.Count ?? 0,
                ConfigPath = configPath ?? string.Empty,
                LastKey = _lastKey,
                LastResolvedSoundId = _lastResolvedSoundId,
                LastPlayedSoundId = _lastPlayedSoundId,
                LastPlaybackResult = _lastPlaybackResult,
                LastEventAt = _lastEventAt
            };
        }
    }
}
