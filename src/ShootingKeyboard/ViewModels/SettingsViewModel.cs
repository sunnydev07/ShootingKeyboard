using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;

namespace ShootingKeyboard.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly ISoundPackManager _soundPackManager;
    private readonly IAudioEngine _audioEngine;
    private readonly IKeyboardHook _keyboardHook;
    private readonly IStartupManager _startupManager;
    private readonly IOverlayManager _overlayManager;
    private readonly IComboTracker _comboTracker;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IProfileManager _profileManager;
    private readonly IProfileImportExportService _profileImportExportService;

    private bool _isLoading;

    [ObservableProperty]
    private float _masterVolume;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private SoundPack? _selectedPack;

    [ObservableProperty]
    private bool _overlayEnabled;

    [ObservableProperty]
    private bool _performanceMode;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private int _comboWindowMs;

    [ObservableProperty]
    private bool _ignoreKeyRepeats;

    [ObservableProperty]
    private int _globalCooldownMs;

    [ObservableProperty]
    private AppProfile? _selectedProfile;

    [ObservableProperty]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private bool _showRipple = true;

    [ObservableProperty]
    private bool _showCombo = true;

    [ObservableProperty]
    private string _rippleColor = "#FFA500";

    [ObservableProperty]
    private string _comboPosition = "TopCenter";

    [ObservableProperty]
    private double _overlayScale = 1.0;

    public string[] AvailableComboPositions { get; } = new[] { "TopCenter", "TopLeft", "TopRight", "BottomCenter" };

    public ObservableCollection<AppProfile> Profiles { get; } = new();
    public ObservableCollection<SoundPack> AvailablePacks { get; } = new();

    public event Action? RequestClose;
    public event Action? RequestOpenKeyBindings;
    public event Action? RequestOpenSoundPacks;
    public event Action? RequestOpenAppRules;

    public SettingsViewModel(
        IConfigService configService,
        ISoundPackManager soundPackManager,
        IAudioEngine audioEngine,
        IKeyboardHook keyboardHook,
        IStartupManager startupManager,
        IOverlayManager overlayManager,
        IComboTracker comboTracker,
        ITrayIconManager trayIconManager,
        IProfileManager profileManager,
        IProfileImportExportService profileImportExportService)
    {
        _configService = configService;
        _soundPackManager = soundPackManager;
        _audioEngine = audioEngine;
        _keyboardHook = keyboardHook;
        _startupManager = startupManager;
        _overlayManager = overlayManager;
        _comboTracker = comboTracker;
        _trayIconManager = trayIconManager;
        _profileManager = profileManager;
        _profileImportExportService = profileImportExportService;

        LoadFromConfig();
    }

    public void LoadFromConfig()
    {
        _isLoading = true;
        try
        {
            var config = _configService.Load();
            MasterVolume = config.MasterVolume;
            IsMuted = config.IsMuted;
            IsEnabled = config.IsEnabled;
            OverlayEnabled = config.OverlayEnabled;
            PerformanceMode = config.PerformanceMode;
            StartWithWindows = config.StartWithWindows;
            ComboWindowMs = config.ComboWindowMs;

            var filter = config.PlaybackFilter ?? new PlaybackFilterConfig();
            IgnoreKeyRepeats = filter.IgnoreKeyRepeats;
            GlobalCooldownMs = filter.GlobalCooldownMs;

            var overlay = config.Overlay ?? new OverlayConfig();
            ShowRipple = overlay.ShowRipple;
            ShowCombo = overlay.ShowCombo;
            RippleColor = overlay.RippleColor;
            ComboPosition = overlay.ComboPosition;
            OverlayScale = overlay.Scale;

            Profiles.Clear();
            foreach (var profile in _profileManager.GetProfiles(config))
            {
                Profiles.Add(profile);
            }
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id.Equals(config.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
                              ?? Profiles.FirstOrDefault();

            AvailablePacks.Clear();
            foreach (var pack in _soundPackManager.GetPacks())
            {
                AvailablePacks.Add(pack);
            }

            SelectedPack = AvailablePacks.FirstOrDefault(p => p.Id.Equals(config.ActivePackId, StringComparison.OrdinalIgnoreCase))
                           ?? AvailablePacks.FirstOrDefault();
        }
        finally
        {
            _isLoading = false;
        }
    }

    partial void OnMasterVolumeChanged(float value)
    {
        if (!_isLoading)
        {
            _audioEngine.SetMasterVolume(value);
        }
    }

    partial void OnIsMutedChanged(bool value)
    {
        if (!_isLoading)
        {
            _audioEngine.SetMuted(value);
        }
    }

    partial void OnComboWindowMsChanged(int value)
    {
        if (!_isLoading)
        {
            _comboTracker.ComboWindowMs = value;
        }
    }

    partial void OnOverlayEnabledChanged(bool value)
    {
        if (!_isLoading)
        {
            _overlayManager.IsEnabled = value && !PerformanceMode;
        }
    }

    partial void OnPerformanceModeChanged(bool value)
    {
        if (!_isLoading)
        {
            _overlayManager.IsEnabled = OverlayEnabled && !value;
        }
    }

    public void RevertRuntimeChanges()
    {
        var config = _configService.Load();
        _audioEngine.SetMasterVolume(config.MasterVolume);
        _audioEngine.SetMuted(config.IsMuted);
        _comboTracker.ComboWindowMs = config.ComboWindowMs;
        _overlayManager.IsEnabled = config.OverlayEnabled && !config.PerformanceMode;
        _overlayManager.ApplyConfig(config.Overlay ?? new OverlayConfig());
        _startupManager.SetStartupEnabled(config.StartWithWindows);
    }

    [RelayCommand]
    public void CreateProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName)) return;

        var config = _configService.Load();
        _profileManager.CopyRootSettingsToActiveProfile(config);
        var created = _profileManager.CreateProfile(config, NewProfileName.Trim());
        _configService.Save(config);

        NewProfileName = string.Empty;
        LoadFromConfig();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == created.Id);
        _trayIconManager.ShowNotification("Shooting Keyboard", $"Profile '{created.Name}' created", BalloonIcon.Info);
    }

    [RelayCommand]
    public void DeleteSelectedProfile()
    {
        if (SelectedProfile == null || Profiles.Count <= 1)
        {
            _trayIconManager.ShowNotification("Shooting Keyboard", "Cannot delete the only profile", BalloonIcon.Warning);
            return;
        }

        var config = _configService.Load();
        if (SelectedProfile.Id.Equals(config.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
        {
            _trayIconManager.ShowNotification("Shooting Keyboard", "Cannot delete the active profile. Switch to another profile first.", BalloonIcon.Warning);
            return;
        }

        var name = SelectedProfile.Name;
        if (_profileManager.DeleteProfile(config, SelectedProfile.Id))
        {
            _configService.Save(config);
            LoadFromConfig();
            _trayIconManager.ShowNotification("Shooting Keyboard", $"Profile '{name}' deleted", BalloonIcon.Info);
        }
    }

    [RelayCommand]
    public void ActivateSelectedProfile()
    {
        if (SelectedProfile == null) return;

        var config = _configService.Load();
        if (_profileManager.SetActiveProfile(config, SelectedProfile.Id))
        {
            _configService.Save(config);
            LoadFromConfig();

            // Apply runtime changes
            _audioEngine.SetMasterVolume(config.MasterVolume);
            _audioEngine.SetMuted(config.IsMuted);
            _comboTracker.ComboWindowMs = config.ComboWindowMs;
            _overlayManager.IsEnabled = config.OverlayEnabled && !config.PerformanceMode;
            _startupManager.SetStartupEnabled(config.StartWithWindows);
            _soundPackManager.SetActivePack(config.ActivePackId);

            _trayIconManager.ShowNotification("Shooting Keyboard", $"Profile '{SelectedProfile.Name}' activated", BalloonIcon.Info);
        }
    }

    public bool ImportProfileFromFile(string filePath)
    {
        try
        {
            var profile = _profileImportExportService.ImportProfile(filePath);
            var config = _configService.Load();

            // Resolve ID collisions
            if (config.Profiles.Any(p => p.Id.Equals(profile.Id, StringComparison.OrdinalIgnoreCase)))
            {
                profile.Id = "profile_" + Guid.NewGuid().ToString("N")[..8];
            }

            config.Profiles.Add(profile);
            _configService.Save(config);

            LoadFromConfig();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == profile.Id);
            _trayIconManager.ShowNotification("Shooting Keyboard", $"Profile '{profile.Name}' imported", BalloonIcon.Info);
            return true;
        }
        catch (Exception ex)
        {
            _trayIconManager.ShowNotification("Shooting Keyboard", $"Import failed: {ex.Message}", BalloonIcon.Error);
            return false;
        }
    }

    public bool ExportSelectedProfileToFile(string filePath)
    {
        if (SelectedProfile == null)
        {
            _trayIconManager.ShowNotification("Shooting Keyboard", "No profile selected to export", BalloonIcon.Warning);
            return false;
        }

        try
        {
            _profileImportExportService.ExportProfile(SelectedProfile, filePath);
            _trayIconManager.ShowNotification("Shooting Keyboard", $"Profile '{SelectedProfile.Name}' exported successfully", BalloonIcon.Info);
            return true;
        }
        catch (Exception ex)
        {
            _trayIconManager.ShowNotification("Shooting Keyboard", $"Export failed: {ex.Message}", BalloonIcon.Error);
            return false;
        }
    }

    [RelayCommand]
    public void Save()
    {
        var config = _configService.Load();
        config.MasterVolume = MasterVolume;
        config.IsMuted = IsMuted;
        config.IsEnabled = IsEnabled;
        config.OverlayEnabled = OverlayEnabled;
        config.PerformanceMode = PerformanceMode;
        config.StartWithWindows = StartWithWindows;
        config.ComboWindowMs = ComboWindowMs;

        config.PlaybackFilter.IgnoreKeyRepeats = IgnoreKeyRepeats;
        config.PlaybackFilter.GlobalCooldownMs = GlobalCooldownMs;

        config.Overlay ??= new OverlayConfig();
        config.Overlay.ShowRipple = ShowRipple;
        config.Overlay.ShowCombo = ShowCombo;
        config.Overlay.RippleColor = RippleColor;
        config.Overlay.ComboPosition = ComboPosition;
        config.Overlay.Scale = OverlayScale;

        if (SelectedPack != null)
        {
            config.ActivePackId = SelectedPack.Id;
            _soundPackManager.SetActivePack(SelectedPack.Id);
        }

        _configService.Save(config);

        // Apply runtime changes
        _audioEngine.SetMasterVolume(config.MasterVolume);
        _audioEngine.SetMuted(config.IsMuted);
        _comboTracker.ComboWindowMs = config.ComboWindowMs;
        _overlayManager.IsEnabled = config.OverlayEnabled && !config.PerformanceMode;
        _overlayManager.ApplyConfig(config.Overlay);
        _startupManager.SetStartupEnabled(config.StartWithWindows);

        try
        {
            if (config.IsEnabled && !_keyboardHook.IsRunning)
            {
                _keyboardHook.Start();
            }
            else if (!config.IsEnabled && _keyboardHook.IsRunning)
            {
                _keyboardHook.Stop();
            }
        }
        catch (Exception ex)
        {
            _trayIconManager.ShowNotification("Shooting Keyboard", $"Failed to update keyboard hook state: {ex.Message}", BalloonIcon.Error);
        }

        _trayIconManager.ShowNotification("Shooting Keyboard", "Settings saved successfully", BalloonIcon.Info);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    public void ResetDefaults()
    {
        _configService.ResetToDefaults();
        LoadFromConfig();
        _trayIconManager.ShowNotification("Shooting Keyboard", "Settings reset to defaults", BalloonIcon.Info);
    }

    [RelayCommand]
    public void TestSound()
    {
        if (SelectedPack != null)
        {
            var defaultSound = SelectedPack.Sounds.FirstOrDefault(s => !s.IsComboVariant);
            if (defaultSound != null)
            {
                if (!_audioEngine.IsSoundLoaded(defaultSound.Id) && System.IO.File.Exists(defaultSound.File))
                {
                    _audioEngine.LoadSound(defaultSound.Id, defaultSound.File);
                }
                _audioEngine.Play(defaultSound.Id, MasterVolume);
            }
        }
    }

    [RelayCommand]
    public void OpenKeyBindings() => RequestOpenKeyBindings?.Invoke();

    [RelayCommand]
    public void OpenSoundPacks() => RequestOpenSoundPacks?.Invoke();

    [RelayCommand]
    public void OpenAppRules() => RequestOpenAppRules?.Invoke();
}