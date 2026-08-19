using System;
using System.Collections.Generic;
using System.Linq;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class ProfileManager : IProfileManager
{
    public IReadOnlyList<AppProfile> GetProfiles(AppConfig config)
    {
        EnsureProfiles(config);
        return config.Profiles.AsReadOnly();
    }

    public AppProfile GetActiveProfile(AppConfig config)
    {
        EnsureProfiles(config);
        return config.Profiles.FirstOrDefault(p => p.Id.Equals(config.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
               ?? config.Profiles[0];
    }

    public AppProfile CreateProfile(AppConfig config, string name)
    {
        EnsureProfiles(config);
        var activeProfile = GetActiveProfile(config);
        var id = "profile_" + Guid.NewGuid().ToString("N")[..8];
        var newProfile = activeProfile.Clone(id, string.IsNullOrWhiteSpace(name) ? "New Profile" : name.Trim());
        config.Profiles.Add(newProfile);
        return newProfile;
    }

    public bool DeleteProfile(AppConfig config, string profileId)
    {
        EnsureProfiles(config);
        if (config.Profiles.Count <= 1) return false;

        // Prevent deleting active profile
        if (profileId.Equals(config.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var profile = config.Profiles.FirstOrDefault(p => p.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
        if (profile == null) return false;

        return config.Profiles.Remove(profile);
    }

    public bool SetActiveProfile(AppConfig config, string profileId)
    {
        EnsureProfiles(config);
        var targetProfile = config.Profiles.FirstOrDefault(p => p.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
        if (targetProfile == null) return false;

        CopyRootSettingsToActiveProfile(config);
        config.ActiveProfileId = targetProfile.Id;
        ApplyActiveProfileToRootSettings(config);
        return true;
    }

    public void CopyRootSettingsToActiveProfile(AppConfig config)
    {
        EnsureProfiles(config);
        var activeProfile = GetActiveProfile(config);

        activeProfile.MasterVolume = config.MasterVolume;
        activeProfile.IsMuted = config.IsMuted;
        activeProfile.IsEnabled = config.IsEnabled;
        activeProfile.ActivePackId = config.ActivePackId;
        activeProfile.OverlayEnabled = config.OverlayEnabled;
        activeProfile.PerformanceMode = config.PerformanceMode;
        activeProfile.ComboWindowMs = config.ComboWindowMs;

        activeProfile.KeyBindings = new Dictionary<int, string>(config.KeyBindings ?? new());
        activeProfile.GroupBindings = new Dictionary<string, string>(config.GroupBindings ?? new());
        activeProfile.GroupVolumeOverrides = new Dictionary<string, float>(config.GroupVolumeOverrides ?? new());
        activeProfile.KeyVolumeOverrides = new Dictionary<int, float>(config.KeyVolumeOverrides ?? new());

        activeProfile.PlaybackFilter = new PlaybackFilterConfig
        {
            IgnoreKeyRepeats = config.PlaybackFilter?.IgnoreKeyRepeats ?? true,
            GlobalCooldownMs = config.PlaybackFilter?.GlobalCooldownMs ?? 20,
            GroupCooldownMs = new Dictionary<string, int>(config.PlaybackFilter?.GroupCooldownMs ?? new()),
            KeyCooldownMs = new Dictionary<int, int>(config.PlaybackFilter?.KeyCooldownMs ?? new())
        };
    }

    public void ApplyActiveProfileToRootSettings(AppConfig config)
    {
        EnsureProfiles(config);
        var activeProfile = GetActiveProfile(config);

        config.MasterVolume = activeProfile.MasterVolume;
        config.IsMuted = activeProfile.IsMuted;
        config.IsEnabled = activeProfile.IsEnabled;
        config.ActivePackId = activeProfile.ActivePackId;
        config.OverlayEnabled = activeProfile.OverlayEnabled;
        config.PerformanceMode = activeProfile.PerformanceMode;
        config.ComboWindowMs = activeProfile.ComboWindowMs;

        config.KeyBindings = new Dictionary<int, string>(activeProfile.KeyBindings ?? new());
        config.GroupBindings = new Dictionary<string, string>(activeProfile.GroupBindings ?? new());
        config.GroupVolumeOverrides = new Dictionary<string, float>(activeProfile.GroupVolumeOverrides ?? new());
        config.KeyVolumeOverrides = new Dictionary<int, float>(activeProfile.KeyVolumeOverrides ?? new());

        config.PlaybackFilter = new PlaybackFilterConfig
        {
            IgnoreKeyRepeats = activeProfile.PlaybackFilter?.IgnoreKeyRepeats ?? true,
            GlobalCooldownMs = activeProfile.PlaybackFilter?.GlobalCooldownMs ?? 20,
            GroupCooldownMs = new Dictionary<string, int>(activeProfile.PlaybackFilter?.GroupCooldownMs ?? new()),
            KeyCooldownMs = new Dictionary<int, int>(activeProfile.PlaybackFilter?.KeyCooldownMs ?? new())
        };
    }

    private static void EnsureProfiles(AppConfig config)
    {
        if (config.Profiles == null)
        {
            config.Profiles = new List<AppProfile>();
        }

        if (config.Profiles.Count == 0)
        {
            config.Profiles.Add(new AppProfile
            {
                Id = "default",
                Name = "Default",
                MasterVolume = config.MasterVolume,
                IsMuted = config.IsMuted,
                IsEnabled = config.IsEnabled,
                ActivePackId = config.ActivePackId,
                OverlayEnabled = config.OverlayEnabled,
                PerformanceMode = config.PerformanceMode,
                ComboWindowMs = config.ComboWindowMs,
                KeyBindings = new Dictionary<int, string>(config.KeyBindings ?? new()),
                GroupBindings = new Dictionary<string, string>(config.GroupBindings ?? new()),
                GroupVolumeOverrides = new Dictionary<string, float>(config.GroupVolumeOverrides ?? new()),
                KeyVolumeOverrides = new Dictionary<int, float>(config.KeyVolumeOverrides ?? new()),
                PlaybackFilter = new PlaybackFilterConfig
                {
                    IgnoreKeyRepeats = config.PlaybackFilter?.IgnoreKeyRepeats ?? true,
                    GlobalCooldownMs = config.PlaybackFilter?.GlobalCooldownMs ?? 20,
                    GroupCooldownMs = new Dictionary<string, int>(config.PlaybackFilter?.GroupCooldownMs ?? new()),
                    KeyCooldownMs = new Dictionary<int, int>(config.PlaybackFilter?.KeyCooldownMs ?? new())
                }
            });
        }

        if (string.IsNullOrEmpty(config.ActiveProfileId) || !config.Profiles.Any(p => p.Id == config.ActiveProfileId))
        {
            config.ActiveProfileId = config.Profiles[0].Id;
        }
    }
}
