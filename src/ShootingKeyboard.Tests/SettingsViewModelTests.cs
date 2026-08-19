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
    private readonly Mock<IProfileManager> _profileManagerMock = new();
    private readonly Mock<IProfileImportExportService> _profileImportExportServiceMock = new();

    private readonly List<SoundPack> _testPacks;
    private readonly List<AppProfile> _testProfiles;

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

        _testProfiles = new List<AppProfile>
        {
            new AppProfile { Id = "default", Name = "Default", MasterVolume = 0.8f, ActivePackId = "warzone" },
            new AppProfile { Id = "gaming", Name = "Gaming", MasterVolume = 1.0f, ActivePackId = "scifi" }
        };

        _soundPackManagerMock.Setup(m => m.GetPacks()).Returns(_testPacks);
        _profileManagerMock.Setup(p => p.GetProfiles(It.IsAny<AppConfig>()))
            .Returns(_testProfiles);

        _audioEngineMock.Setup(a => a.GetOutputDevices())
            .Returns(new List<AudioDeviceInfo> { new AudioDeviceInfo { Id = "device_1", Name = "Headphones" } });
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
            ComboWindowMs = 450,
            ActiveProfileId = "default",
            PlaybackFilter = new PlaybackFilterConfig
            {
                IgnoreKeyRepeats = true,
                GlobalCooldownMs = 25
            }
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
            _trayIconManagerMock.Object,
            _profileManagerMock.Object,
            _profileImportExportServiceMock.Object);
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
        Assert.True(vm.IgnoreKeyRepeats);
        Assert.Equal(25, vm.GlobalCooldownMs);
        Assert.True(vm.ShowRipple);
        Assert.True(vm.ShowCombo);
        Assert.Equal("#FFA500", vm.RippleColor);
        Assert.Equal("TopCenter", vm.ComboPosition);
        Assert.Equal(1.0, vm.OverlayScale);
        Assert.False(vm.QuietHoursEnabled);
        Assert.Equal("22:00", vm.QuietHoursStart);
        Assert.Equal("08:00", vm.QuietHoursEnd);
        Assert.Equal(2, vm.AvailablePacks.Count);
        Assert.NotNull(vm.SelectedPack);
        Assert.Equal("warzone", vm.SelectedPack.Id);
        Assert.Equal(2, vm.Profiles.Count);
        Assert.NotNull(vm.SelectedProfile);
        Assert.Equal("default", vm.SelectedProfile.Id);
        Assert.Equal(2, vm.AvailableAudioDevices.Count);
        Assert.NotNull(vm.SelectedAudioDevice);
    }

    [Fact]
    public void CreateProfile_WhenNameProvided_CreatesProfileAndReloads()
    {
        var vm = CreateViewModel();
        vm.NewProfileName = "Streaming";

        var newProf = new AppProfile { Id = "profile_stream", Name = "Streaming" };
        _profileManagerMock.Setup(p => p.CreateProfile(It.IsAny<AppConfig>(), "Streaming")).Returns(newProf);

        vm.CreateProfileCommand.Execute(null);

        _profileManagerMock.Verify(p => p.CopyRootSettingsToActiveProfile(It.IsAny<AppConfig>()), Times.Once);
        _profileManagerMock.Verify(p => p.CreateProfile(It.IsAny<AppConfig>(), "Streaming"), Times.Once);
        _configServiceMock.Verify(c => c.Save(It.IsAny<AppConfig>()), Times.Once);
        Assert.Equal(string.Empty, vm.NewProfileName);
    }

    [Fact]
    public void ActivateProfile_UpdatesActiveProfileAndAppliesRuntime()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = _testProfiles[1]; // gaming

        _profileManagerMock.Setup(p => p.SetActiveProfile(It.IsAny<AppConfig>(), "gaming"))
            .Callback<AppConfig, string>((cfg, id) =>
            {
                cfg.ActiveProfileId = "gaming";
                cfg.MasterVolume = 1.0f;
                cfg.ActivePackId = "scifi";
            })
            .Returns(true);

        vm.ActivateSelectedProfileCommand.Execute(null);

        _profileManagerMock.Verify(p => p.SetActiveProfile(It.IsAny<AppConfig>(), "gaming"), Times.Once);
        _configServiceMock.Verify(c => c.Save(It.IsAny<AppConfig>()), Times.Once);
        _audioEngineMock.Verify(a => a.SetMasterVolume(1.0f), Times.AtLeastOnce());
        _soundPackManagerMock.Verify(s => s.SetActivePack("scifi"), Times.AtLeastOnce());
    }

    [Fact]
    public void DeleteProfile_ActiveProfile_ShowsWarningWithoutDeleting()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = _testProfiles[0]; // "default" which is active

        vm.DeleteSelectedProfileCommand.Execute(null);

        _profileManagerMock.Verify(p => p.DeleteProfile(It.IsAny<AppConfig>(), It.IsAny<string>()), Times.Never);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", It.Is<string>(s => s.Contains("Cannot delete")), BalloonIcon.Warning), Times.Once);
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
        vm.IgnoreKeyRepeats = false;
        vm.GlobalCooldownMs = 50;
        vm.ShowRipple = false;
        vm.RippleColor = "#00FF00";
        vm.ComboPosition = "TopRight";
        vm.OverlayScale = 1.8;
        vm.QuietHoursEnabled = true;
        vm.QuietHoursStart = "23:00";
        vm.QuietHoursEnd = "07:00";
        vm.SelectedAudioDevice = new AudioDeviceInfo { Id = "device_1", Name = "Headphones" };
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
            cfg.PlaybackFilter.IgnoreKeyRepeats == false &&
            cfg.PlaybackFilter.GlobalCooldownMs == 50 &&
            cfg.Overlay.ShowRipple == false &&
            cfg.Overlay.RippleColor == "#00FF00" &&
            cfg.Overlay.ComboPosition == "TopRight" &&
            cfg.Overlay.Scale == 1.8 &&
            cfg.QuietHours.Enabled == true &&
            cfg.QuietHours.Start == new TimeSpan(23, 0, 0) &&
            cfg.QuietHours.End == new TimeSpan(7, 0, 0) &&
            cfg.AudioOutputDeviceId == "device_1" &&
            cfg.ActivePackId == "scifi"
        )), Times.Once);

        // Verify runtime updates
        _audioEngineMock.Verify(a => a.SetMasterVolume(0.5f), Times.AtLeastOnce());
        _audioEngineMock.Verify(a => a.SetMuted(true), Times.AtLeastOnce());
        _audioEngineMock.Verify(a => a.SetOutputDevice("device_1"), Times.Once);
        _comboTrackerMock.VerifySet(ct => ct.ComboWindowMs = 600, Times.AtLeastOnce());
        _overlayManagerMock.VerifySet(o => o.IsEnabled = false, Times.AtLeastOnce());
        _overlayManagerMock.Verify(o => o.ApplyConfig(It.Is<OverlayConfig>(oc => oc.ComboPosition == "TopRight")), Times.Once);
        _startupManagerMock.Verify(s => s.SetStartupEnabled(true), Times.Once);
        _soundPackManagerMock.Verify(s => s.SetActivePack("scifi"), Times.Once);
        _keyboardHookMock.Verify(k => k.Stop(), Times.Once);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", "Settings saved successfully", BalloonIcon.Info), Times.Once);

        Assert.True(closeFired);
    }

    [Fact]
    public void LiveApply_UpdatesRuntimeServicesOnPropertyChange()
    {
        var vm = CreateViewModel();

        _audioEngineMock.Invocations.Clear();
        _comboTrackerMock.Invocations.Clear();
        _overlayManagerMock.Invocations.Clear();

        vm.MasterVolume = 0.35f;
        _audioEngineMock.Verify(a => a.SetMasterVolume(0.35f), Times.Once);

        vm.IsMuted = true;
        _audioEngineMock.Verify(a => a.SetMuted(true), Times.Once);

        vm.ComboWindowMs = 700;
        _comboTrackerMock.VerifySet(ct => ct.ComboWindowMs = 700, Times.Once);

        vm.OverlayEnabled = false;
        _overlayManagerMock.VerifySet(o => o.IsEnabled = false, Times.Once);

        vm.OverlayEnabled = true;
        vm.PerformanceMode = true;
        _overlayManagerMock.VerifySet(o => o.IsEnabled = false, Times.AtLeastOnce());
    }

    [Fact]
    public void RevertRuntimeChanges_RestoresServicesFromSavedConfig()
    {
        var originalConfig = new AppConfig
        {
            MasterVolume = 0.8f,
            IsMuted = false,
            ComboWindowMs = 400,
            OverlayEnabled = true,
            PerformanceMode = false,
            StartWithWindows = false
        };
        var vm = CreateViewModel(originalConfig);

        // Change runtime values
        vm.MasterVolume = 0.2f;
        vm.IsMuted = true;
        vm.ComboWindowMs = 900;
        vm.OverlayEnabled = false;

        // Act
        vm.RevertRuntimeChanges();

        // Assert - restored to original config values
        _audioEngineMock.Verify(a => a.SetMasterVolume(0.8f), Times.AtLeastOnce());
        _audioEngineMock.Verify(a => a.SetMuted(false), Times.AtLeastOnce());
        _comboTrackerMock.VerifySet(ct => ct.ComboWindowMs = 400, Times.AtLeastOnce());
        _overlayManagerMock.VerifySet(o => o.IsEnabled = true, Times.AtLeastOnce());
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

    [Fact]
    public void OpenAppRulesCommand_FiresRequestOpenAppRulesEvent()
    {
        var vm = CreateViewModel();
        var eventFired = false;
        vm.RequestOpenAppRules += () => eventFired = true;

        vm.OpenAppRulesCommand.Execute(null);

        Assert.True(eventFired);
    }

    [Fact]
    public void ImportProfileFromFile_AddsProfileAndReloads()
    {
        var vm = CreateViewModel();
        var imported = new AppProfile { Id = "imported_id", Name = "Imported Profile", MasterVolume = 0.9f };
        _profileImportExportServiceMock.Setup(p => p.ImportProfile("C:\\test\\profile.json")).Returns(imported);

        var result = vm.ImportProfileFromFile("C:\\test\\profile.json");

        Assert.True(result);
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.Profiles.Any(p => p.Name == "Imported Profile"))), Times.Once);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", It.Is<string>(s => s.Contains("imported")), BalloonIcon.Info), Times.Once);
    }

    [Fact]
    public void ExportSelectedProfileToFile_ExportsSelectedProfile()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = _testProfiles[0];

        var result = vm.ExportSelectedProfileToFile("C:\\test\\exported.json");

        Assert.True(result);
        _profileImportExportServiceMock.Verify(p => p.ExportProfile(_testProfiles[0], "C:\\test\\exported.json"), Times.Once);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", It.Is<string>(s => s.Contains("exported")), BalloonIcon.Info), Times.Once);
    }
}
