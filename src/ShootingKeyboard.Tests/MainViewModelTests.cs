using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using ShootingKeyboard.ViewModels;
using Xunit;

namespace ShootingKeyboard.Tests;

public class MainViewModelTests
{
    private readonly Mock<IConfigService> _configServiceMock = new();
    private readonly Mock<IKeyboardHook> _keyboardHookMock = new();
    private readonly Mock<IAudioEngine> _audioEngineMock = new();
    private readonly Mock<ISoundPackManager> _soundPackManagerMock = new();
    private readonly Mock<IComboTracker> _comboTrackerMock = new();
    private readonly Mock<IBindingResolver> _bindingResolverMock = new();
    private readonly Mock<IOverlayManager> _overlayManagerMock = new();
    private readonly Mock<ITrayIconManager> _trayIconManagerMock = new();
    private readonly Mock<IStartupManager> _startupManagerMock = new();
    private readonly Mock<IRuntimeDiagnosticsService> _diagnosticsServiceMock = new();
    private readonly Mock<IKeyPressFilter> _keyPressFilterMock = new();
    private readonly Mock<ISoundVariantSelector> _variantSelectorMock = new();
    private readonly Mock<IForegroundAppService> _foregroundAppServiceMock = new();
    private readonly Mock<IAppRuleEvaluator> _appRuleEvaluatorMock = new();
    private readonly Mock<IProfileManager> _profileManagerMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();

    private readonly SoundPack _testPack;

    public MainViewModelTests()
    {
        _testPack = new SoundPack
        {
            Id = "warzone",
            Name = "Warzone",
            Sounds = new List<SoundEntry>
            {
                new SoundEntry { Id = "shot_default", DisplayName = "Assault Rifle", Volume = 0.9f, File = "shot.wav" },
                new SoundEntry { Id = "shot_tier2", DisplayName = "Tier 2 Shot", Volume = 1.0f, IsComboVariant = true, ComboTier = 2 }
            }
        };

        _soundPackManagerMock.Setup(m => m.GetPacks()).Returns(new List<SoundPack> { _testPack });
        _soundPackManagerMock.SetupGet(m => m.ActivePack).Returns(_testPack);
        _soundPackManagerMock.Setup(m => m.GetPack("warzone")).Returns(_testPack);

        _keyPressFilterMock.Setup(f => f.ShouldProcess(It.Is<KeyEvent>(k => k.IsPressed), It.IsAny<AppConfig>())).Returns(true);
        _keyPressFilterMock.Setup(f => f.ShouldProcess(It.Is<KeyEvent>(k => !k.IsPressed), It.IsAny<AppConfig>())).Returns(false);

        _variantSelectorMock.Setup(v => v.SelectClip(It.IsAny<SoundEntry>()))
            .Returns((SoundEntry s) => new SelectedSoundClip { AudioId = s?.Id ?? "", FilePath = s?.File ?? "", Volume = s?.Volume ?? 1.0f });

        _appRuleEvaluatorMock.Setup(e => e.Evaluate(It.IsAny<ForegroundAppInfo>(), It.IsAny<AppConfig>()))
            .Returns(new AppRuleDecision { ShouldPlay = true, Reason = "no-rule" });

        _profileManagerMock.Setup(p => p.GetProfiles(It.IsAny<AppConfig>()))
            .Returns(new List<AppProfile> { new AppProfile { Id = "default", Name = "Default" } });
        _profileManagerMock.Setup(p => p.GetActiveProfile(It.IsAny<AppConfig>()))
            .Returns(new AppProfile { Id = "default", Name = "Default" });
    }

    private MainViewModel CreateViewModel(AppConfig? config = null)
    {
        var cfg = config ?? new AppConfig
        {
            MasterVolume = 0.75f,
            IsMuted = false,
            IsEnabled = true,
            ActivePackId = "warzone",
            OverlayEnabled = true,
            PerformanceMode = false,
            ComboWindowMs = 400
        };

        _configServiceMock.Setup(c => c.Load()).Returns(cfg);

        return new MainViewModel(
            _configServiceMock.Object,
            _keyboardHookMock.Object,
            _audioEngineMock.Object,
            _soundPackManagerMock.Object,
            _comboTrackerMock.Object,
            _bindingResolverMock.Object,
            _overlayManagerMock.Object,
            _trayIconManagerMock.Object,
            _startupManagerMock.Object,
            _diagnosticsServiceMock.Object,
            _keyPressFilterMock.Object,
            _variantSelectorMock.Object,
            _foregroundAppServiceMock.Object,
            _appRuleEvaluatorMock.Object,
            _profileManagerMock.Object,
            _serviceProviderMock.Object);
    }

    [Fact]
    public void Initialize_ConfiguresDependenciesLoadsPacksAndStartsHook()
    {
        var vm = CreateViewModel();

        vm.Initialize();

        _audioEngineMock.Verify(a => a.SetMasterVolume(0.75f), Times.Once);
        _audioEngineMock.Verify(a => a.SetMuted(false), Times.Once);
        _comboTrackerMock.VerifySet(ct => ct.ComboWindowMs = 400, Times.Once);
        _overlayManagerMock.VerifySet(o => o.IsEnabled = true, Times.Once);
        _soundPackManagerMock.Verify(s => s.Refresh(), Times.Once);
        _soundPackManagerMock.Verify(s => s.SetActivePack("warzone"), Times.Once);
        _keyboardHookMock.Verify(k => k.Start(), Times.Once);
        _trayIconManagerMock.Verify(t => t.UpdateTooltip(It.Is<string>(tt => tt.Contains("Active") && tt.Contains("Warzone"))), Times.AtLeastOnce());
    }

    [Fact]
    public void Initialize_WhenHookStartFails_HandlesGracefullyAndShowsError()
    {
        _keyboardHookMock.Setup(k => k.Start()).Throws(new InvalidOperationException("Failed to install hook"));
        var vm = CreateViewModel();

        // Act (should not throw)
        vm.Initialize();

        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", It.Is<string>(msg => msg.Contains("Failed to start keyboard hook")), BalloonIcon.Error), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WhenKeyDown_RegistersComboResolvesSoundAndPlays()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _bindingResolverMock.Setup(r => r.ResolveSound(0x41, _testPack, It.IsAny<AppConfig>())).Returns("shot_default");
        _comboTrackerMock.SetupGet(c => c.CurrentTier).Returns(0);
        _comboTrackerMock.SetupGet(c => c.ComboCount).Returns(1);
        _audioEngineMock.Setup(a => a.IsSoundLoaded("shot_default")).Returns(true);

        // Act - simulate pressing 'A' (0x41)
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert
        _comboTrackerMock.Verify(c => c.RegisterKeyPress(), Times.Once);
        _audioEngineMock.Verify(a => a.PlayWithPitch("shot_default", 0.9f, 1.0f), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordKeyEvent(It.Is<KeyEvent>(k => k.KeyCode == 0x41 && k.IsPressed)), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordResolvedSound(0x41, "shot_default"), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback("shot_default", true, "ok"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WithVariantSelected_PlaysSelectedVariantClip()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _bindingResolverMock.Setup(r => r.ResolveSound(0x41, _testPack, It.IsAny<AppConfig>())).Returns("shot_default");
        _comboTrackerMock.SetupGet(c => c.CurrentTier).Returns(0);
        _variantSelectorMock.Setup(v => v.SelectClip(It.Is<SoundEntry>(s => s.Id == "shot_default")))
            .Returns(new SelectedSoundClip { AudioId = "shot_default::variant::0", FilePath = "shot_var.wav", Volume = 0.9f });
        _audioEngineMock.Setup(a => a.IsSoundLoaded("shot_default::variant::0")).Returns(true);

        // Act
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert
        _audioEngineMock.Verify(a => a.PlayWithPitch("shot_default::variant::0", 0.9f, 1.0f), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback("shot_default::variant::0", true, "ok"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WithVolumeOverrides_CalculatesEffectiveVolumeCorrectly()
    {
        var config = new AppConfig
        {
            IsEnabled = true,
            IsMuted = false,
            ActivePackId = "warzone",
            KeyVolumeOverrides = new Dictionary<int, float> { { 0x41, 0.5f } }, // 'A' (WASD group)
            GroupVolumeOverrides = new Dictionary<string, float> { { KeyGroups.WASD, 0.8f } }
        };
        var vm = CreateViewModel(config);
        vm.Initialize();

        _bindingResolverMock.Setup(r => r.ResolveSound(0x41, _testPack, It.IsAny<AppConfig>())).Returns("shot_default");
        _comboTrackerMock.SetupGet(c => c.CurrentTier).Returns(0);
        _variantSelectorMock.Setup(v => v.SelectClip(It.Is<SoundEntry>(s => s.Id == "shot_default")))
            .Returns(new SelectedSoundClip { AudioId = "shot_default", FilePath = "shot.wav", Volume = 1.0f });
        _audioEngineMock.Setup(a => a.IsSoundLoaded("shot_default")).Returns(true);

        // Act
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Effective volume = 1.0 (clip) * 0.8 (WASD group) * 0.5 (key 0x41) = 0.40f
        _audioEngineMock.Verify(a => a.PlayWithPitch("shot_default", It.Is<float>(v => Math.Abs(v - 0.4f) < 0.001f), 1.0f), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WhenAppRuleDisablesPlayback_IgnoresAndRecordsDiagnostics()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _appRuleEvaluatorMock.Setup(e => e.Evaluate(It.IsAny<ForegroundAppInfo>(), It.IsAny<AppConfig>()))
            .Returns(new AppRuleDecision { ShouldPlay = false, Reason = "disabled-by-app-rule" });

        // Act
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert
        _comboTrackerMock.Verify(c => c.RegisterKeyPress(), Times.Never);
        _audioEngineMock.Verify(a => a.PlayWithPitch(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<float>()), Times.Never);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback(string.Empty, false, "disabled-by-app-rule"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WhenAppRuleOverridesSoundPack_UsesOverriddenPack()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        var overridePack = new SoundPack
        {
            Id = "override_pack",
            Name = "Override Pack",
            Sounds = new List<SoundEntry>
            {
                new SoundEntry { Id = "laser_shot", DisplayName = "Laser", Volume = 1.0f, File = "laser.wav" }
            }
        };
        _soundPackManagerMock.Setup(m => m.GetPack("override_pack")).Returns(overridePack);

        _appRuleEvaluatorMock.Setup(e => e.Evaluate(It.IsAny<ForegroundAppInfo>(), It.IsAny<AppConfig>()))
            .Returns(new AppRuleDecision { ShouldPlay = true, SoundPackIdOverride = "override_pack", Reason = "matched-rule" });

        _bindingResolverMock.Setup(r => r.ResolveSound(0x41, overridePack, It.IsAny<AppConfig>())).Returns("laser_shot");
        _variantSelectorMock.Setup(v => v.SelectClip(It.Is<SoundEntry>(s => s.Id == "laser_shot")))
            .Returns(new SelectedSoundClip { AudioId = "laser_shot", FilePath = "laser.wav", Volume = 1.0f });
        _audioEngineMock.Setup(a => a.IsSoundLoaded("laser_shot")).Returns(true);

        // Act
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert
        _audioEngineMock.Verify(a => a.PlayWithPitch("laser_shot", 1.0f, 1.0f), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback("laser_shot", true, "ok"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WhenKeyUp_IgnoredAndRecordedInDiagnostics()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        // Act - simulate releasing key
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, false)));

        // Assert
        _comboTrackerMock.Verify(c => c.RegisterKeyPress(), Times.Never);
        _audioEngineMock.Verify(a => a.PlayWithPitch(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<float>()), Times.Never);
        _diagnosticsServiceMock.Verify(d => d.RecordKeyEvent(It.Is<KeyEvent>(k => k.KeyCode == 0x41 && !k.IsPressed)), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback(string.Empty, false, "key-up"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WhenFilteredByKeyPressFilter_IgnoredAndRecordedInDiagnostics()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _keyPressFilterMock.Setup(f => f.ShouldProcess(It.IsAny<KeyEvent>(), It.IsAny<AppConfig>())).Returns(false);

        // Act - simulate pressing 'A'
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert
        _comboTrackerMock.Verify(c => c.RegisterKeyPress(), Times.Never);
        _audioEngineMock.Verify(a => a.PlayWithPitch(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<float>()), Times.Never);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback(string.Empty, false, "filtered"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_WhenMutedOrDisabled_IgnoredAndRecordedInDiagnostics()
    {
        var config = new AppConfig { IsMuted = true, IsEnabled = true };
        var vm = CreateViewModel(config);
        vm.Initialize();

        // Act
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert
        _audioEngineMock.Verify(a => a.PlayWithPitch(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<float>()), Times.Never);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback(string.Empty, false, "muted"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_HigherComboTier_AppliesPitchBoostAndVariantSound()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _bindingResolverMock.Setup(r => r.ResolveSound(0x41, _testPack, It.IsAny<AppConfig>())).Returns("shot_default");
        _comboTrackerMock.SetupGet(c => c.CurrentTier).Returns(2);
        _comboTrackerMock.SetupGet(c => c.ComboCount).Returns(20);
        _audioEngineMock.Setup(a => a.IsSoundLoaded("shot_tier2")).Returns(true);

        // Act - simulate pressing 'A'
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert: Tier 2 has combo variant "shot_tier2" with pitch 1.10f (1.0 + 2*0.05)
        _audioEngineMock.Verify(a => a.PlayWithPitch("shot_tier2", 1.0f, 1.10f), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback("shot_tier2", true, "ok"), Times.Once);
    }

    [Fact]
    public void OnKeyPressed_PerformanceMode_SkipsPitchBoost()
    {
        var config = new AppConfig { PerformanceMode = true, IsEnabled = true, IsMuted = false, ActivePackId = "warzone" };
        var vm = CreateViewModel(config);
        vm.Initialize();

        _bindingResolverMock.Setup(r => r.ResolveSound(0x41, _testPack, It.IsAny<AppConfig>())).Returns("shot_default");
        _comboTrackerMock.SetupGet(c => c.CurrentTier).Returns(2);
        _audioEngineMock.Setup(a => a.IsSoundLoaded("shot_default")).Returns(true);

        // Act
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(new KeyEvent(0x41, true)));

        // Assert: pitch should stay 1.0f in performance mode, base sound used
        _audioEngineMock.Verify(a => a.PlayWithPitch("shot_default", 0.9f, 1.0f), Times.Once);
        _diagnosticsServiceMock.Verify(d => d.RecordPlayback("shot_default", true, "ok"), Times.Once);
    }

    [Fact]
    public void ToggleMute_TogglesMuteStateAndUpdatesAudioEngine()
    {
        var config = new AppConfig { IsMuted = false };
        var vm = CreateViewModel(config);
        vm.Initialize();

        // Act
        vm.ToggleMute();

        // Assert
        Assert.True(config.IsMuted);
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.IsMuted == true)), Times.Once);
        _audioEngineMock.Verify(a => a.SetMuted(true), Times.AtLeastOnce());
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", "Audio muted", BalloonIcon.Info), Times.Once);
    }

    [Fact]
    public void ToggleEnabled_TogglesEnabledStateAndControlsHook()
    {
        var config = new AppConfig { IsEnabled = true };
        var vm = CreateViewModel(config);
        vm.Initialize();

        // Act - Pause
        vm.ToggleEnabled();

        // Assert - Paused
        Assert.False(config.IsEnabled);
        _keyboardHookMock.Verify(k => k.Stop(), Times.Once);
        _comboTrackerMock.Verify(ct => ct.Reset(), Times.Once);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", "Paused", BalloonIcon.Info), Times.Once);

        // Act - Resume
        vm.ToggleEnabled();

        // Assert - Resumed
        Assert.True(config.IsEnabled);
        _keyboardHookMock.Verify(k => k.Start(), Times.Exactly(2)); // Once in Init, once on Resume
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", "Resumed (active)", BalloonIcon.Info), Times.Once);
    }

    [Fact]
    public void PacksChanged_ReloadsAudioAndUpdatesTooltip()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        // Act - trigger packs changed event
        _soundPackManagerMock.Raise(s => s.PacksChanged += null, EventArgs.Empty);

        // Assert
        _audioEngineMock.Verify(a => a.UnloadAllSounds(), Times.AtLeastOnce());
        _trayIconManagerMock.Verify(t => t.UpdateTooltip(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    public void TrayIcon_SoundPackSelected_SwitchesPackAndSavesConfig()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _trayIconManagerMock.Raise(t => t.SoundPackSelected += null, _trayIconManagerMock.Object, "warzone");

        _soundPackManagerMock.Verify(s => s.SetActivePack("warzone"), Times.AtLeastOnce());
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.ActivePackId == "warzone")), Times.AtLeastOnce());
    }

    [Fact]
    public void TrayIcon_ProfileSelected_SwitchesProfileAndSavesConfig()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _profileManagerMock.Setup(p => p.SetActiveProfile(It.IsAny<AppConfig>(), "gaming")).Returns(true);

        _trayIconManagerMock.Raise(t => t.ProfileSelected += null, _trayIconManagerMock.Object, "gaming");

        _profileManagerMock.Verify(p => p.SetActiveProfile(It.IsAny<AppConfig>(), "gaming"), Times.Once);
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.ActivePackId != null)), Times.AtLeastOnce());
    }

    [Fact]
    public void TrayIcon_VolumeSelected_UpdatesVolumeAndSavesConfig()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        _trayIconManagerMock.Raise(t => t.VolumeSelected += null, _trayIconManagerMock.Object, 0.5f);

        _audioEngineMock.Verify(a => a.SetMasterVolume(0.5f), Times.AtLeastOnce());
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.MasterVolume == 0.5f)), Times.AtLeastOnce());
    }

    [Fact]
    public void TrayIcon_ToggleOverlayRequested_TogglesOverlayAndSavesConfig()
    {
        var config = new AppConfig { OverlayEnabled = true };
        var vm = CreateViewModel(config);
        vm.Initialize();

        _trayIconManagerMock.Raise(t => t.ToggleOverlayRequested += null, _trayIconManagerMock.Object, EventArgs.Empty);

        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.OverlayEnabled == false)), Times.AtLeastOnce());
        _overlayManagerMock.VerifySet(o => o.IsEnabled = false, Times.AtLeastOnce());
    }

    [Fact]
    public void Dispose_CleansUpSubscriptionsAndDisposesResources()
    {
        var vm = CreateViewModel();
        vm.Initialize();

        // Act
        vm.Dispose();

        // Assert
        _keyboardHookMock.Verify(k => k.Dispose(), Times.Once);
        _audioEngineMock.Verify(a => a.Dispose(), Times.Once);
        _comboTrackerMock.Verify(ct => ct.Dispose(), Times.Once);
        _trayIconManagerMock.Verify(t => t.Dispose(), Times.Once);
    }
}
