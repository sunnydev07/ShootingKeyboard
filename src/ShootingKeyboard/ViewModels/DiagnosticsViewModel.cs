using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;

namespace ShootingKeyboard.ViewModels;

public sealed partial class DiagnosticsViewModel : ObservableObject
{
    private readonly IRuntimeDiagnosticsService _diagnosticsService;
    private readonly IConfigService _configService;
    private readonly IKeyboardHook _keyboardHook;
    private readonly IAudioEngine _audioEngine;
    private readonly ISoundPackManager _soundPackManager;

    [ObservableProperty]
    private RuntimeDiagnosticsSnapshot? _snapshot;

    public DiagnosticsViewModel(
        IRuntimeDiagnosticsService diagnosticsService,
        IConfigService configService,
        IKeyboardHook keyboardHook,
        IAudioEngine audioEngine,
        ISoundPackManager soundPackManager)
    {
        _diagnosticsService = diagnosticsService;
        _configService = configService;
        _keyboardHook = keyboardHook;
        _audioEngine = audioEngine;
        _soundPackManager = soundPackManager;

        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        var config = _configService.Load();
        Snapshot = _diagnosticsService.CreateSnapshot(
            config,
            _keyboardHook,
            _audioEngine,
            _soundPackManager,
            _configService.ConfigPath);
    }
}
