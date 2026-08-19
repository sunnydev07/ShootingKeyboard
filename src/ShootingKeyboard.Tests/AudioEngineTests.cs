using System;
using System.IO;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class AudioEngineTests
{
    private readonly string _testWavPath;

    public AudioEngineTests()
    {
        var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        _testWavPath = Path.Combine(solutionDir, "sound-packs", "Warzone", "shot_default.wav");
    }

    [Fact]
    public void AudioEngineService_InitialMasterVolume_IsDefault()
    {
        using var engine = new AudioEngineService();
        Assert.Equal(0.7f, engine.GetMasterVolume(), 2);
    }

    [Fact]
    public void AudioEngineService_SetMasterVolume_ClampsBetweenZeroAndOne()
    {
        using var engine = new AudioEngineService();

        engine.SetMasterVolume(1.5f);
        Assert.Equal(1.0f, engine.GetMasterVolume());

        engine.SetMasterVolume(-0.5f);
        Assert.Equal(0.0f, engine.GetMasterVolume());

        engine.SetMasterVolume(0.45f);
        Assert.Equal(0.45f, engine.GetMasterVolume());
    }

    [Fact]
    public void AudioEngineService_LoadSound_LoadsValidWavIntoCache()
    {
        using var engine = new AudioEngineService();

        if (File.Exists(_testWavPath))
        {
            var loaded = engine.LoadSound("test_shot", _testWavPath);
            Assert.True(loaded);
            Assert.True(engine.IsSoundLoaded("test_shot"));
            Assert.Contains("test_shot", engine.GetLoadedSoundIds());
        }
    }

    [Fact]
    public void AudioEngineService_LoadSound_NonExistentFile_ReturnsFalse()
    {
        using var engine = new AudioEngineService();
        var loaded = engine.LoadSound("fake_sound", "C:\\nonexistent_file_path.wav");

        Assert.False(loaded);
        Assert.False(engine.IsSoundLoaded("fake_sound"));
    }

    [Fact]
    public void AudioEngineService_UnloadSound_RemovesFromCache()
    {
        using var engine = new AudioEngineService();

        if (File.Exists(_testWavPath))
        {
            engine.LoadSound("test_shot", _testWavPath);
            Assert.True(engine.IsSoundLoaded("test_shot"));

            engine.UnloadSound("test_shot");
            Assert.False(engine.IsSoundLoaded("test_shot"));
        }
    }

    [Fact]
    public void AudioEngineService_Play_DoesNotThrowForLoadedOrUnloadedSounds()
    {
        using var engine = new AudioEngineService();

        if (File.Exists(_testWavPath))
        {
            engine.LoadSound("test_shot", _testWavPath);
            engine.Play("test_shot", 0.5f);
            engine.PlayWithPitch("test_shot", 0.8f, 1.2f);
        }

        // Unloaded sound should silently not throw
        engine.Play("missing_sound", 1.0f);
    }

    [Fact]
    public void AudioEngineService_GetOutputDevices_DoesNotThrow()
    {
        using var engine = new AudioEngineService();
        var devices = engine.GetOutputDevices();
        Assert.NotNull(devices);
    }

    [Fact]
    public void AudioEngineService_SetOutputDevice_NullOrEmpty_SetsDefault()
    {
        using var engine = new AudioEngineService();
        var result = engine.SetOutputDevice(null);
        Assert.True(result);
        Assert.Null(engine.CurrentOutputDeviceId);

        result = engine.SetOutputDevice("");
        Assert.True(result);
        Assert.Null(engine.CurrentOutputDeviceId);
    }

    [Fact]
    public void AudioEngineService_SetOutputDevice_InvalidId_ReturnsFalseAndResetsToDefault()
    {
        using var engine = new AudioEngineService();
        var result = engine.SetOutputDevice("nonexistent_device_guid_12345");
        Assert.False(result);
        Assert.Null(engine.CurrentOutputDeviceId);
    }
}
