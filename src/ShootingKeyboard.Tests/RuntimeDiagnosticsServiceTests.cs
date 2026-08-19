using System;
using System.Collections.Generic;
using Moq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class RuntimeDiagnosticsServiceTests
{
    private readonly Mock<IKeyboardHook> _keyboardHookMock = new();
    private readonly Mock<IAudioEngine> _audioEngineMock = new();
    private readonly Mock<ISoundPackManager> _soundPackManagerMock = new();

    private readonly RuntimeDiagnosticsService _diagnosticsService = new();

    [Fact]
    public void CreateSnapshot_ReturnsInitialStateFromServices()
    {
        var config = new AppConfig
        {
            IsEnabled = true,
            IsMuted = false,
            ActivePackId = "warzone"
        };

        var activePack = new SoundPack
        {
            Id = "warzone",
            Name = "Warzone"
        };

        _keyboardHookMock.SetupGet(k => k.IsRunning).Returns(true);
        _soundPackManagerMock.SetupGet(s => s.ActivePack).Returns(activePack);
        _soundPackManagerMock.Setup(s => s.GetPacks()).Returns(new List<SoundPack> { activePack });
        _audioEngineMock.Setup(a => a.GetLoadedSoundIds()).Returns(new List<string> { "shot1", "shot2" });

        var snapshot = _diagnosticsService.CreateSnapshot(
            config,
            _keyboardHookMock.Object,
            _audioEngineMock.Object,
            _soundPackManagerMock.Object,
            "C:\\test\\config.json");

        Assert.True(snapshot.KeyboardHookRunning);
        Assert.True(snapshot.AppEnabled);
        Assert.False(snapshot.Muted);
        Assert.Equal("warzone", snapshot.ActivePackId);
        Assert.Equal("Warzone", snapshot.ActivePackName);
        Assert.Equal(1, snapshot.AvailablePackCount);
        Assert.Equal(2, snapshot.LoadedSoundCount);
        Assert.Equal("C:\\test\\config.json", snapshot.ConfigPath);
    }

    [Fact]
    public void RecordKeyEvent_UpdatesLastKeyAndTimestamp()
    {
        var keyEvent = new KeyEvent(0x41, true); // 'A' key down
        _diagnosticsService.RecordKeyEvent(keyEvent);

        var snapshot = _diagnosticsService.CreateSnapshot(
            new AppConfig(),
            _keyboardHookMock.Object,
            _audioEngineMock.Object,
            _soundPackManagerMock.Object,
            "config.json");

        Assert.NotNull(snapshot.LastEventAt);
        Assert.Contains("0x41", snapshot.LastKey);
        Assert.Contains("Down", snapshot.LastKey);
    }

    [Fact]
    public void RecordResolvedSound_UpdatesLastResolvedSoundId()
    {
        _diagnosticsService.RecordResolvedSound(0x41, "shot_ak47");

        var snapshot = _diagnosticsService.CreateSnapshot(
            new AppConfig(),
            _keyboardHookMock.Object,
            _audioEngineMock.Object,
            _soundPackManagerMock.Object,
            "config.json");

        Assert.Equal("shot_ak47", snapshot.LastResolvedSoundId);
    }

    [Fact]
    public void RecordPlayback_Success_UpdatesLastPlayedAndResult()
    {
        _diagnosticsService.RecordPlayback("shot_ak47", true, "ok");

        var snapshot = _diagnosticsService.CreateSnapshot(
            new AppConfig(),
            _keyboardHookMock.Object,
            _audioEngineMock.Object,
            _soundPackManagerMock.Object,
            "config.json");

        Assert.Equal("shot_ak47", snapshot.LastPlayedSoundId);
        Assert.Equal("Success", snapshot.LastPlaybackResult);
    }

    [Fact]
    public void RecordPlayback_Failure_UpdatesResultWithReason()
    {
        _diagnosticsService.RecordPlayback("shot_ak47", false, "file-not-found");

        var snapshot = _diagnosticsService.CreateSnapshot(
            new AppConfig(),
            _keyboardHookMock.Object,
            _audioEngineMock.Object,
            _soundPackManagerMock.Object,
            "config.json");

        Assert.Equal("shot_ak47", snapshot.LastPlayedSoundId);
        Assert.Contains("file-not-found", snapshot.LastPlaybackResult);
    }
}
