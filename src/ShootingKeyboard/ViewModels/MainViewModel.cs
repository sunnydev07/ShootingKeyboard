using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using ShootingKeyboard.Views;

namespace ShootingKeyboard.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IConfigService _configService;
    private readonly IKeyboardHook _keyboardHook;
    private readonly IAudioEngine _audioEngine;
    private readonly ISoundPackManager _soundPackManager;
    private readonly IComboTracker _comboTracker;
    private readonly IBindingResolver _bindingResolver;
    private readonly IOverlayManager _overlayManager;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IStartupManager _startupManager;
    private readonly IRuntimeDiagnosticsService _diagnosticsService;
    private readonly IKeyPressFilter _keyPressFilter;
    private readonly ISoundVariantSelector _variantSelector;
    private readonly IForegroundAppService _foregroundAppService;
    private readonly IAppRuleEvaluator _appRuleEvaluator;
    private readonly IServiceProvider _serviceProvider;

    private SettingsWindow? _settingsWindow;
    private KeyBindingWindow? _keyBindingWindow;
    private SoundPackWindow? _soundPackWindow;
    private DiagnosticsWindow? _diagnosticsWindow;
    private AppRulesWindow? _appRulesWindow;
    private bool _disposed;

    public MainViewModel(
        IConfigService configService,
        IKeyboardHook keyboardHook,
        IAudioEngine audioEngine,
        ISoundPackManager soundPackManager,
        IComboTracker comboTracker,
        IBindingResolver bindingResolver,
        IOverlayManager overlayManager,
        ITrayIconManager trayIconManager,
        IStartupManager startupManager,
        IRuntimeDiagnosticsService diagnosticsService,
        IKeyPressFilter keyPressFilter,
        ISoundVariantSelector variantSelector,
        IForegroundAppService foregroundAppService,
        IAppRuleEvaluator appRuleEvaluator,
        IServiceProvider serviceProvider)
    {
        _configService = configService;
        _keyboardHook = keyboardHook;
        _audioEngine = audioEngine;
        _soundPackManager = soundPackManager;
        _comboTracker = comboTracker;
        _bindingResolver = bindingResolver;
        _overlayManager = overlayManager;
        _trayIconManager = trayIconManager;
        _startupManager = startupManager;
        _diagnosticsService = diagnosticsService;
        _keyPressFilter = keyPressFilter;
        _variantSelector = variantSelector;
        _foregroundAppService = foregroundAppService;
        _appRuleEvaluator = appRuleEvaluator;
        _serviceProvider = serviceProvider;
    }

    public void Initialize()
    {
        var config = _configService.Load();

        // Configure audio engine
        _audioEngine.SetMasterVolume(config.MasterVolume);
        _audioEngine.SetMuted(config.IsMuted);

        // Configure combo tracker
        _comboTracker.ComboWindowMs = config.ComboWindowMs;

        // Configure overlay
        _overlayManager.IsEnabled = config.OverlayEnabled && !config.PerformanceMode;

        // Discover and load sound packs
        _soundPackManager.Refresh();
        if (!string.IsNullOrEmpty(config.ActivePackId))
        {
            _soundPackManager.SetActivePack(config.ActivePackId);
        }
        else if (_soundPackManager.GetPacks().Count > 0)
        {
            _soundPackManager.SetActivePack(_soundPackManager.GetPacks()[0].Id);
        }

        LoadActivePackAudio();

        // Subscribe to hook events
        _keyboardHook.KeyPressed += OnKeyPressed;
        _soundPackManager.PacksChanged += OnPacksChanged;
        _comboTracker.ComboChanged += OnComboChanged;
        _comboTracker.TierChanged += OnTierChanged;

        // Subscribe to tray events
        _trayIconManager.ShowSettingsRequested += (s, e) => ShowSettingsWindow();
        _trayIconManager.DiagnosticsRequested += (s, e) => ShowDiagnosticsWindow();
        _trayIconManager.ToggleMuteRequested += (s, e) => ToggleMute();
        _trayIconManager.ToggleEnabledRequested += (s, e) => ToggleEnabled();
        _trayIconManager.ExitRequested += (s, e) => ExitApp();

        UpdateTrayTooltip();

        if (config.IsEnabled)
        {
            try
            {
                _keyboardHook.Start();
            }
            catch (Exception ex)
            {
                _trayIconManager.ShowNotification("Shooting Keyboard", $"Failed to start keyboard hook: {ex.Message}", BalloonIcon.Error);
            }
        }
    }

    private void LoadActivePackAudio()
    {
        var activePack = _soundPackManager.ActivePack;
        if (activePack == null) return;

        _audioEngine.UnloadAllSounds();
        foreach (var sound in activePack.Sounds)
        {
            if (File.Exists(sound.File))
            {
                _audioEngine.LoadSound(sound.Id, sound.File);
            }

            if (sound.Variants != null)
            {
                for (int i = 0; i < sound.Variants.Count; i++)
                {
                    var variantFile = sound.Variants[i];
                    if (File.Exists(variantFile))
                    {
                        var variantId = _variantSelector.GetVariantAudioId(sound.Id, i);
                        _audioEngine.LoadSound(variantId, variantFile);
                    }
                }
            }
        }
    }

    private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
    {
        _diagnosticsService.RecordKeyEvent(e.KeyEvent);

        var config = _configService.Load();

        if (!_keyPressFilter.ShouldProcess(e.KeyEvent, config))
        {
            if (!e.IsPressed)
            {
                _diagnosticsService.RecordPlayback(string.Empty, false, "key-up");
            }
            else
            {
                _diagnosticsService.RecordPlayback(string.Empty, false, "filtered");
            }
            return;
        }

        if (!config.IsEnabled || config.IsMuted)
        {
            _diagnosticsService.RecordPlayback(string.Empty, false, !config.IsEnabled ? "app-disabled" : "muted");
            return;
        }

        var foregroundApp = _foregroundAppService.GetForegroundApp();
        var ruleDecision = _appRuleEvaluator.Evaluate(foregroundApp, config);
        if (!ruleDecision.ShouldPlay)
        {
            _diagnosticsService.RecordPlayback(string.Empty, false, ruleDecision.Reason);
            return;
        }

        var activePack = _soundPackManager.ActivePack;
        if (!string.IsNullOrEmpty(ruleDecision.SoundPackIdOverride))
        {
            var overriddenPack = _soundPackManager.GetPack(ruleDecision.SoundPackIdOverride);
            if (overriddenPack != null)
            {
                activePack = overriddenPack;
            }
        }

        if (activePack == null)
        {
            _diagnosticsService.RecordPlayback(string.Empty, false, "no-active-pack");
            return;
        }

        // Advance combo
        _comboTracker.RegisterKeyPress();
        var tier = _comboTracker.CurrentTier;
        var comboCount = _comboTracker.ComboCount;

        // Resolve sound
        var resolvedSoundId = _bindingResolver.ResolveSound(e.KeyCode, activePack, config);
        _diagnosticsService.RecordResolvedSound(e.KeyCode, resolvedSoundId);

        if (string.IsNullOrEmpty(resolvedSoundId))
        {
            _diagnosticsService.RecordPlayback(string.Empty, false, "no-resolved-sound");
            return;
        }

        // Check if there is a combo variant for this tier
        string finalSoundId = resolvedSoundId;
        if (tier > 0 && !config.PerformanceMode)
        {
            var comboVariant = activePack.Sounds.FirstOrDefault(s => s.IsComboVariant && s.ComboTier == tier);
            if (comboVariant != null)
            {
                finalSoundId = comboVariant.Id;
            }
        }

        // Calculate pitch boost for higher tiers
        float pitch = 1.0f;
        if (tier > 0 && !config.PerformanceMode)
        {
            pitch = Math.Clamp(1.0f + (tier * 0.05f), 0.5f, 2.0f);
        }

        var soundEntry = activePack.GetSound(finalSoundId);
        if (soundEntry == null)
        {
            _diagnosticsService.RecordPlayback(finalSoundId, false, "sound-not-found");
            return;
        }

        var clip = _variantSelector.SelectClip(soundEntry);
        string audioId = clip.AudioId;
        float volume = clip.Volume;

        var keyGroup = KeyGroups.GetGroupForKey(e.KeyCode);
        if (!string.IsNullOrEmpty(keyGroup) && config.GroupVolumeOverrides != null && config.GroupVolumeOverrides.TryGetValue(keyGroup, out var grpVol))
        {
            volume *= grpVol;
        }

        if (config.KeyVolumeOverrides != null && config.KeyVolumeOverrides.TryGetValue(e.KeyCode, out var keyVol))
        {
            volume *= keyVol;
        }

        if (!_audioEngine.IsSoundLoaded(audioId))
        {
            if (File.Exists(clip.FilePath))
            {
                _audioEngine.LoadSound(audioId, clip.FilePath);
            }
            else
            {
                _diagnosticsService.RecordPlayback(audioId, false, "file-not-found");
                return;
            }
        }

        _audioEngine.PlayWithPitch(audioId, volume, pitch);
        _diagnosticsService.RecordPlayback(audioId, true, "ok");

        // Visual overlay feedback
        if (config.OverlayEnabled && !config.PerformanceMode)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                GetCursorPos(out var pt);
                _overlayManager.ShowKeyPressEffect(new Point(pt.X, pt.Y));
                _overlayManager.UpdateComboDisplay(comboCount, tier);
            });
        }
    }

    private void OnPacksChanged(object? sender, EventArgs e)
    {
        LoadActivePackAudio();
        UpdateTrayTooltip();
    }

    private void OnComboChanged(object? sender, int count)
    {
        var config = _configService.Load();
        if (config.OverlayEnabled && !config.PerformanceMode)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                _overlayManager.UpdateComboDisplay(count, _comboTracker.CurrentTier);
            });
        }
    }

    private void OnTierChanged(object? sender, int tier)
    {
        // Handled via combo changed
    }

    public void ToggleMute()
    {
        var config = _configService.Load();
        config.IsMuted = !config.IsMuted;
        _configService.Save(config);
        _audioEngine.SetMuted(config.IsMuted);
        UpdateTrayTooltip();
        _trayIconManager.ShowNotification("Shooting Keyboard", config.IsMuted ? "Audio muted" : "Audio unmuted", BalloonIcon.Info);
    }

    public void ToggleEnabled()
    {
        var config = _configService.Load();
        config.IsEnabled = !config.IsEnabled;
        _configService.Save(config);

        if (config.IsEnabled)
        {
            try
            {
                _keyboardHook.Start();
            }
            catch (Exception ex)
            {
                _trayIconManager.ShowNotification("Shooting Keyboard", $"Failed to start keyboard hook: {ex.Message}", BalloonIcon.Error);
            }
        }
        else
        {
            _keyboardHook.Stop();
            _comboTracker.Reset();
            _keyPressFilter.Reset();
        }

        UpdateTrayTooltip();
        _trayIconManager.ShowNotification("Shooting Keyboard", config.IsEnabled ? "Resumed (active)" : "Paused", BalloonIcon.Info);
    }

    public void ShowSettingsWindow()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                var vm = _serviceProvider.GetRequiredService<SettingsViewModel>();
                vm.RequestOpenKeyBindings += ShowKeyBindingWindow;
                vm.RequestOpenSoundPacks += ShowSoundPackWindow;
                vm.RequestOpenAppRules += ShowAppRulesWindow;
                _settingsWindow = new SettingsWindow(vm);
            }

            _settingsWindow.Show();
            _settingsWindow.Activate();
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }
        });
    }

    public void ShowKeyBindingWindow()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_keyBindingWindow == null || !_keyBindingWindow.IsLoaded)
            {
                var vm = _serviceProvider.GetRequiredService<KeyBindingViewModel>();
                _keyBindingWindow = new KeyBindingWindow(vm);
            }

            _keyBindingWindow.Show();
            _keyBindingWindow.Activate();
        });
    }

    public void ShowSoundPackWindow()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_soundPackWindow == null || !_soundPackWindow.IsLoaded)
            {
                var vm = _serviceProvider.GetRequiredService<SoundPackViewModel>();
                _soundPackWindow = new SoundPackWindow(vm);
            }

            _soundPackWindow.Show();
            _soundPackWindow.Activate();
        });
    }

    public void ShowAppRulesWindow()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_appRulesWindow == null || !_appRulesWindow.IsLoaded)
            {
                var vm = _serviceProvider.GetRequiredService<AppRulesViewModel>();
                _appRulesWindow = new AppRulesWindow(vm);
            }

            _appRulesWindow.Show();
            _appRulesWindow.Activate();
        });
    }

    public void ShowDiagnosticsWindow()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_diagnosticsWindow == null || !_diagnosticsWindow.IsLoaded)
            {
                var vm = _serviceProvider.GetRequiredService<DiagnosticsViewModel>();
                _diagnosticsWindow = new DiagnosticsWindow(vm);
            }

            _diagnosticsWindow.Show();
            _diagnosticsWindow.Activate();
            if (_diagnosticsWindow.WindowState == WindowState.Minimized)
            {
                _diagnosticsWindow.WindowState = WindowState.Normal;
            }
        });
    }

    private void UpdateTrayTooltip()
    {
        var config = _configService.Load();
        var packName = _soundPackManager.ActivePack?.Name ?? "None";
        var status = !config.IsEnabled ? "Paused" : (config.IsMuted ? "Muted" : "Active");
        _trayIconManager.UpdateTooltip($"Shooting Keyboard — {status} ({packName})");
    }

    public void ExitApp()
    {
        Dispose();
        Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _keyboardHook.KeyPressed -= OnKeyPressed;
        _soundPackManager.PacksChanged -= OnPacksChanged;
        _comboTracker.ComboChanged -= OnComboChanged;
        _comboTracker.TierChanged -= OnTierChanged;

        _keyboardHook.Dispose();
        _audioEngine.Dispose();
        _comboTracker.Dispose();
        _trayIconManager.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
