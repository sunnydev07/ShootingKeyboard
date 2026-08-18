using System;
using System.Linq;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Resolves key strokes to specific sound entries following the precedence chain:
/// 1. Explicit per-key binding (config.KeyBindings[keyCode])
/// 2. Explicit group binding (config.GroupBindings[groupName])
/// 3. Pack group-specific sound (sound.Group == groupName)
/// 4. Pack default sound (sound.Id == "default" or first non-combo sound or first sound)
/// </summary>
public sealed class BindingResolver : IBindingResolver
{
    public string? ResolveSound(int virtualKeyCode, SoundPack? activePack, AppConfig config)
    {
        if (activePack == null || activePack.Sounds.Count == 0)
            return null;

        // 1. Explicit per-key binding in config
        if (config.KeyBindings.TryGetValue(virtualKeyCode, out var explicitSoundId) && !string.IsNullOrWhiteSpace(explicitSoundId))
        {
            if (activePack.Sounds.Any(s => s.Id.Equals(explicitSoundId, StringComparison.OrdinalIgnoreCase)))
            {
                return explicitSoundId;
            }
        }

        // Determine logical group for key
        var group = KeyGroups.GetGroupForKey(virtualKeyCode);

        // 2. Explicit group binding in config
        if (!string.IsNullOrEmpty(group) &&
            config.GroupBindings.TryGetValue(group, out var groupSoundId) &&
            !string.IsNullOrWhiteSpace(groupSoundId))
        {
            if (activePack.Sounds.Any(s => s.Id.Equals(groupSoundId, StringComparison.OrdinalIgnoreCase)))
            {
                return groupSoundId;
            }
        }

        // 3. Pack defined sound matching group
        if (!string.IsNullOrEmpty(group))
        {
            var packGroupSound = activePack.Sounds.FirstOrDefault(s =>
                !s.IsComboVariant &&
                string.Equals(s.Group, group, StringComparison.OrdinalIgnoreCase));

            if (packGroupSound != null)
            {
                return packGroupSound.Id;
            }
        }

        // 4. Pack default sound: look for sound with Id "default", "shot", "primary", or first base sound
        var defaultSound = activePack.Sounds.FirstOrDefault(s =>
            !s.IsComboVariant &&
            (s.Id.Equals("default", StringComparison.OrdinalIgnoreCase) ||
             s.Id.Equals("primary", StringComparison.OrdinalIgnoreCase) ||
             s.Id.Equals("shot", StringComparison.OrdinalIgnoreCase)))
            ?? activePack.Sounds.FirstOrDefault(s => !s.IsComboVariant)
            ?? activePack.Sounds.FirstOrDefault();

        return defaultSound?.Id;
    }
}
