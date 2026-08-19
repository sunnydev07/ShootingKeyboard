using System;
using System.Collections.Generic;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class KeyPressFilter : IKeyPressFilter
{
    private readonly Func<DateTimeOffset> _timeProvider;
    private readonly HashSet<int> _pressedKeys = new();
    private readonly object _lock = new();
    private DateTimeOffset? _lastAcceptedKeyDownTime;

    public KeyPressFilter(Func<DateTimeOffset>? timeProvider = null)
    {
        _timeProvider = timeProvider ?? (() => DateTimeOffset.UtcNow);
    }

    public bool ShouldProcess(KeyEvent keyEvent, AppConfig config)
    {
        if (keyEvent == null || config == null) return false;

        lock (_lock)
        {
            if (!keyEvent.IsPressed)
            {
                _pressedKeys.Remove(keyEvent.KeyCode);
                return false;
            }

            var filterConfig = config.PlaybackFilter ?? new PlaybackFilterConfig();

            // Check if held-key repeat should be ignored
            if (filterConfig.IgnoreKeyRepeats && _pressedKeys.Contains(keyEvent.KeyCode))
            {
                return false;
            }

            // Determine effective cooldown: Key -> Group -> Global
            int cooldownMs = filterConfig.GlobalCooldownMs;
            var group = KeyGroups.GetGroupForKey(keyEvent.KeyCode);

            if (filterConfig.KeyCooldownMs != null && filterConfig.KeyCooldownMs.TryGetValue(keyEvent.KeyCode, out var keyCooldown))
            {
                cooldownMs = keyCooldown;
            }
            else if (!string.IsNullOrEmpty(group) && filterConfig.GroupCooldownMs != null && filterConfig.GroupCooldownMs.TryGetValue(group, out var groupCooldown))
            {
                cooldownMs = groupCooldown;
            }

            var now = _timeProvider();

            if (_lastAcceptedKeyDownTime.HasValue && cooldownMs > 0)
            {
                var elapsed = (now - _lastAcceptedKeyDownTime.Value).TotalMilliseconds;
                if (elapsed < cooldownMs)
                {
                    return false;
                }
            }

            // Accept key press
            _pressedKeys.Add(keyEvent.KeyCode);
            _lastAcceptedKeyDownTime = now;
            return true;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _pressedKeys.Clear();
            _lastAcceptedKeyDownTime = null;
        }
    }
}
