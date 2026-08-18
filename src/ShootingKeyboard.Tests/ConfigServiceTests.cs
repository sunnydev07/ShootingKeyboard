using System;
using System.IO;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _configFilePath;

    public ConfigServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ShootingKeyboard_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _configFilePath = Path.Combine(_tempDirectory, "config.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore test cleanup errors
        }
    }

    [Fact]
    public void Load_FileDoesNotExist_ReturnsDefaultConfig()
    {
        var service = new ConfigService(_configFilePath);
        var config = service.Load();

        Assert.NotNull(config);
        Assert.Equal(0.7f, config.MasterVolume);
        Assert.False(config.IsMuted);
        Assert.True(config.IsEnabled);
        Assert.Equal("warzone", config.ActivePackId);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PersistsCustomValues()
    {
        var service = new ConfigService(_configFilePath);
        var config = new AppConfig
        {
            MasterVolume = 0.42f,
            IsMuted = true,
            IsEnabled = false,
            ActivePackId = "scifi",
            OverlayEnabled = false,
            PerformanceMode = true,
            StartWithWindows = true,
            ComboWindowMs = 350
        };
        config.KeyBindings[0x41] = "laser_blaster";
        config.GroupBindings[KeyGroups.WASD] = "plasma_shot";

        service.Save(config);

        // Load with a new instance to bypass cache
        var service2 = new ConfigService(_configFilePath);
        var loaded = service2.Load();

        Assert.Equal(0.42f, loaded.MasterVolume, 2);
        Assert.True(loaded.IsMuted);
        Assert.False(loaded.IsEnabled);
        Assert.Equal("scifi", loaded.ActivePackId);
        Assert.False(loaded.OverlayEnabled);
        Assert.True(loaded.PerformanceMode);
        Assert.True(loaded.StartWithWindows);
        Assert.Equal(350, loaded.ComboWindowMs);
        Assert.Equal("laser_blaster", loaded.KeyBindings[0x41]);
        Assert.Equal("plasma_shot", loaded.GroupBindings[KeyGroups.WASD]);
    }

    [Fact]
    public void Load_CorruptFile_FallsBackToDefaultWithoutThrowing()
    {
        File.WriteAllText(_configFilePath, "{ invalid json structure ::: --- }");

        var service = new ConfigService(_configFilePath);
        var config = service.Load();

        Assert.NotNull(config);
        Assert.Equal(0.7f, config.MasterVolume);
    }

    [Fact]
    public void ResetToDefaults_OverwritesConfigWithDefaults()
    {
        var service = new ConfigService(_configFilePath);
        var config = new AppConfig
        {
            MasterVolume = 0.1f,
            ActivePackId = "retro-arcade"
        };
        service.Save(config);

        service.ResetToDefaults();

        var service2 = new ConfigService(_configFilePath);
        var reloaded = service2.Load();

        Assert.Equal(0.7f, reloaded.MasterVolume);
        Assert.Equal("warzone", reloaded.ActivePackId);
    }
}
