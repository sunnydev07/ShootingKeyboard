using System;
using System.Collections.Generic;
using Moq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using ShootingKeyboard.ViewModels;
using Xunit;

namespace ShootingKeyboard.Tests;

public class SettingsViewModelTests
{
    private readonly Mock<IConfigService> _configServiceMock = new();
    private readonly Mock<ISoundPackManager> _soundPackManagerMock = new();
    private readonly Mock<IAudioEngine> _audioEngineMock = new();
    private readonly Mock<IKeyboardHook> _keyboardHookMock = new();
    private readonly Mock<IStartupManager> _startupManagerMock = new();
    private readonly Mock<IOverlayManager> _overlayManagerMock = new();
    private readonly Mock<IComboTracker> _comboTrackerMock = new();
    private readonly Mock<ITrayIconManager> _trayIconManagerMock = new();

    private readonly List<SoundPack> _testPacks;

    public SettingsViewModelTests()
    {
        _testPacks = new List<SoundPack>
        {
            new SoundPack
            {
                Id = "warzone",
                Name = "Warzone",
                Sounds = new List<SoundEntry>
                {
                    new SoundEntry { Id = "gun_default", DisplayName = "Default Shot", File = "shot.wav" }
                }
            },
            new SoundPack
            {
                Id = "scifi",
                Name = "Sci-Fi",
                Sounds = new List<SoundEntry>
                {
                    new SoundEntry { Id = "laser_blaster", DisplayName = "Laser", File = "laser.wav" }
                }
            }
        };

        _soundPackManagerMock.Setup(m => m.GetPacks()).Returns(_testPacks);
    }

    private SettingsViewModel CreateViewModel(AppConfig? config = null)
    {
        var cfg = config ?? new AppConfig
        {
            MasterVolume = 0.8f,
            IsMuted = false,
            IsEnabled = true,
            ActivePackId = "warzone",
            OverlayEnabled = true,
            PerformanceMode = false,
            StartWithWindows = false,
            ComboWindowMs = 450
        };

        _configServiceMock.Setup(c => c.Load()).Returns(cfg);

        return new SettingsViewModel(
            _configServiceMock.Object,
            _soundPackManagerMock.Object,
            _audioEngineMock.Object,
            _keyboardHookMock.Object,
            _startupManagerMock.Object,
            _overlayManagerMock.Object,
            _comboTrackerMock.Object,
            _trayIconManagerMock.Object);
    }

    [Fact]
    public void LoadFromConfig_InitializesPropertiesCorrectly()
    {
        var vm = CreateViewModel();

        Assert.Equal(0.8f, vm.MasterVolume);
        Assert.False(vm.IsMuted);
        Assert.True(vm.IsEnabled);
        Assert.True(vm.OverlayEnabled);
        Assert.False(vm.PerformanceMode);
        Assert.False(vm.StartWithWindows);
        Assert.Equal(450, vm.ComboWindowMs);
        Assert.Equal(2, vm.AvailablePacks.Count);
        Assert.NotNull(vm.SelectedPack);
        Assert.Equal("warzone", vm.SelectedPack.Id);
    }

    [Fact]
    public void Save_PersistsModifiedSettingsAndAppliesRuntimeChanges()
    {
        var vm = CreateViewModel();
        var closeFired = false;
        vm.RequestClose += () => closeFired = true;

        // Modify settings
        vm.MasterVolume = 0.5f;
        vm.IsMuted = true;
        vm.IsEnabled = false;
        vm.OverlayEnabled = false;
        vm.PerformanceMode = true;
        vm.StartWithWindows = true;
        vm.ComboWindowMs = 600;
        vm.SelectedPack = _testPacks[1]; // scifi

        _keyboardHookMock.SetupGet(k => k.IsRunning).Returns(true);

        // Act
        vm.Save();

        // Verify config saved
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg =>
            cfg.MasterVolume == 0.5f &&
            cfg.IsMuted == true &&
            cfg.IsEnabled == false &&
            cfg.OverlayEnabled == false &&
            cfg.PerformanceMode == true &&
            cfg.StartWithWindows == true &&
            cfg.ComboWindowMs == 600 &&
            cfg.ActivePackId == "scifi"
        )), Times.Once);

        // Verify runtime updates
        _audioEngineMock.Verify(a => a.SetMasterVolume(0.5f), Times.Once);
        _audioEngineMock.Verify(a => a.SetMuted(true), Times.Once);
        _comboTrackerMock.VerifySet(ct => ct.ComboWindowMs = 600, Times.Once);
        _overlayManagerMock.VerifySet(o => o.IsEnabled = false, Times.Once);
        _startupManagerMock.Verify(s => s.SetStartupEnabled(true), Times.Once);
        _soundPackManagerMock.Verify(s => s.SetActivePack("scifi"), Times.Once);
        _keyboardHookMock.Verify(k => k.Stop(), Times.Once);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", "Settings saved successfully", BalloonIcon.Info), Times.Once);

        Assert.True(closeFired);
    }

    [Fact]
    public void Save_StartsHook_WhenEnabledAndNotRunning()
    {
        var vm = CreateViewModel();
        vm.IsEnabled = true;
        _keyboardHookMock.SetupGet(k => k.IsRunning).Returns(false);

        vm.Save();

        _keyboardHookMock.Verify(k => k.Start(), Times.Once);
    }

    [Fact]
    public void Save_WhenHookStartFails_HandlesGracefullyWithoutCrashing()
    {
        var vm = CreateViewModel();
        vm.IsEnabled = true;
        _keyboardHookMock.SetupGet(k => k.IsRunning).Returns(false);
        _keyboardHookMock.Setup(k => k.Start()).Throws(new InvalidOperationException("Hook failed"));

        // Act (should not throw)
        vm.Save();

        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", It.Is<string>(msg => msg.Contains("Failed to update keyboard hook")), BalloonIcon.Error), Times.Once);
    }

    [Fact]
    public void ResetDefaults_CallsResetOnConfigServiceAndReloads()
    {
        var vm = CreateViewModel();

        // Setup ResetToDefaults behavior
        _configServiceMock.Setup(c => c.ResetToDefaults()).Callback(() =>
        {
            _configServiceMock.Setup(c => c.Load()).Returns(AppConfig.CreateDefault());
        });

        vm.ResetDefaults();

        _configServiceMock.Verify(c => c.ResetToDefaults(), Times.Once);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", "Settings reset to defaults", BalloonIcon.Info), Times.Once);
        Assert.Equal(0.7f, vm.MasterVolume);
    }

    [Fact]
    public void TestSound_PlaysDefaultSoundOfSelectedPack()
    {
        var vm = CreateViewModel();
        vm.MasterVolume = 0.9f;
        vm.SelectedPack = _testPacks[0]; // warzone with gun_default

        vm.TestSound();

        _audioEngineMock.Verify(a => a.Play("gun_default", 0.9f), Times.Once);
    }

    [Fact]
    public void OpenKeyBindingsCommand_FiresRequestOpenKeyBindingsEvent()
    {
        var vm = CreateViewModel();
        var eventFired = false;
        vm.RequestOpenKeyBindings += () => eventFired = true;

        vm.OpenKeyBindingsCommand.Execute(null);

        Assert.True(eventFired);
    }

    [Fact]
    public void OpenSoundPacksCommand_FiresRequestOpenSoundPacksEvent()
    {
        var vm = CreateViewModel();
        var eventFired = false;
        vm.RequestOpenSoundPacks += () => eventFired = true;

        vm.OpenSoundPacksCommand.Execute(null);

        Assert.True(eventFired);
    }
}
