using System.Collections.Generic;
using System.Linq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class ProfileManagerTests
{
    private readonly ProfileManager _manager = new();

    [Fact]
    public void GetProfiles_WhenProfilesEmpty_CreatesDefaultProfileFromRoot()
    {
        var config = new AppConfig
        {
            MasterVolume = 0.85f,
            IsMuted = false,
            ActivePackId = "scifi",
            ComboWindowMs = 500
        };

        var profiles = _manager.GetProfiles(config);

        Assert.Single(profiles);
        var def = profiles[0];
        Assert.Equal("default", def.Id);
        Assert.Equal("Default", def.Name);
        Assert.Equal(0.85f, def.MasterVolume);
        Assert.Equal("scifi", def.ActivePackId);
        Assert.Equal(500, def.ComboWindowMs);
        Assert.Equal("default", config.ActiveProfileId);
    }

    [Fact]
    public void CreateProfile_ClonesActiveProfile_AndAddsToProfiles()
    {
        var config = new AppConfig
        {
            MasterVolume = 0.6f,
            ActivePackId = "warzone"
        };

        var newProfile = _manager.CreateProfile(config, "Gaming");

        Assert.NotNull(newProfile);
        Assert.Equal("Gaming", newProfile.Name);
        Assert.StartsWith("profile_", newProfile.Id);
        Assert.Equal(0.6f, newProfile.MasterVolume);
        Assert.Equal("warzone", newProfile.ActivePackId);
        Assert.Equal(2, config.Profiles.Count);
    }

    [Fact]
    public void DeleteProfile_NonActiveProfile_Succeeds()
    {
        var config = new AppConfig();
        var p2 = _manager.CreateProfile(config, "Work");

        Assert.Equal(2, config.Profiles.Count);

        var deleted = _manager.DeleteProfile(config, p2.Id);

        Assert.True(deleted);
        Assert.Single(config.Profiles);
    }

    [Fact]
    public void DeleteProfile_ActiveProfile_ReturnsFalse()
    {
        var config = new AppConfig();
        var p2 = _manager.CreateProfile(config, "Work");
        _manager.SetActiveProfile(config, p2.Id);

        var deleted = _manager.DeleteProfile(config, p2.Id);

        Assert.False(deleted);
        Assert.Equal(2, config.Profiles.Count);
    }

    [Fact]
    public void DeleteProfile_LastRemainingProfile_ReturnsFalse()
    {
        var config = new AppConfig();

        var deleted = _manager.DeleteProfile(config, "default");

        Assert.False(deleted);
        Assert.Single(config.Profiles);
    }

    [Fact]
    public void SetActiveProfile_CopiesRootToPreviousActive_AndAppliesTargetToRoot()
    {
        var config = new AppConfig
        {
            MasterVolume = 0.5f,
            ActivePackId = "warzone"
        };
        var p2 = _manager.CreateProfile(config, "SciFi Mode");
        p2.MasterVolume = 0.9f;
        p2.ActivePackId = "scifi";

        // Modify current root settings before switching
        config.MasterVolume = 0.3f;

        // Switch to p2
        var success = _manager.SetActiveProfile(config, p2.Id);

        Assert.True(success);
        Assert.Equal(p2.Id, config.ActiveProfileId);
        // Root now has p2 settings
        Assert.Equal(0.9f, config.MasterVolume);
        Assert.Equal("scifi", config.ActivePackId);

        // Check previous active profile ("default") received root edits (0.3f)
        var defaultProfile = config.Profiles.First(p => p.Id == "default");
        Assert.Equal(0.3f, defaultProfile.MasterVolume);
    }
}
