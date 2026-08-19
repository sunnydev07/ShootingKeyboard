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

    public ObservableCollection<SoundPack> AvailablePacks { get; } = new();

    public event Action? RequestClose;
    public event Action? RequestOpenKeyBindings;
    public event Action? RequestOpenSoundPacks;

    public SettingsViewModel(
        IConfigService configService,
        ISoundPackManager soundPackManager,
        IAudioEngine audioEngine,
        IKeyboardHook keyboardHook,
        IStartupManager startupManager,
        IOverlayManager overlayManager,
        IComboTracker comboTracker,
        ITrayIconManager trayIconManager)
    {
        _configService = configService;
        _soundPackManager = soundPackManager;
        _audioEngine = audioEngine;
        _keyboardHook = keyboardHook;
        _startupManager = startupManager;
        _overlayManager = overlayManager;
        _comboTracker = comboTracker;
        _trayIconManager = trayIconManager;

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
        _startupManager.SetStartupEnabled(config.StartWithWindows);
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
}