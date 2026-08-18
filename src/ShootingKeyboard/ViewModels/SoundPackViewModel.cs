using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;

namespace ShootingKeyboard.ViewModels;

public sealed partial class SoundPackViewModel : ObservableObject
{
    private readonly ISoundPackManager _soundPackManager;
    private readonly IAudioEngine _audioEngine;
    private readonly IConfigService _configService;

    public ObservableCollection<SoundPack> Packs { get; } = new();

    [ObservableProperty]
    private SoundPack? _selectedPack;

    [ObservableProperty]
    private string _activePackId = string.Empty;

    public SoundPackViewModel(
        ISoundPackManager soundPackManager,
        IAudioEngine audioEngine,
        IConfigService configService)
    {
        _soundPackManager = soundPackManager;
        _audioEngine = audioEngine;
        _configService = configService;

        LoadPacks();
    }

    public void LoadPacks()
    {
        _soundPackManager.Refresh();
        var config = _configService.Load();
        ActivePackId = config.ActivePackId;

        Packs.Clear();
        foreach (var pack in _soundPackManager.GetPacks())
        {
            Packs.Add(pack);
        }

        SelectedPack = Packs.FirstOrDefault(p => p.Id.Equals(ActivePackId, StringComparison.OrdinalIgnoreCase))
                       ?? Packs.FirstOrDefault();
    }

    [RelayCommand]
    public void SetActive()
    {
        if (SelectedPack != null)
        {
            var config = _configService.Load();
            config.ActivePackId = SelectedPack.Id;
            _configService.Save(config);

            _soundPackManager.SetActivePack(SelectedPack.Id);
            ActivePackId = SelectedPack.Id;
        }
    }

    [RelayCommand]
    public void PlayPreview(string? soundId)
    {
        if (!string.IsNullOrEmpty(soundId) && SelectedPack != null)
        {
            var sound = SelectedPack.Sounds.FirstOrDefault(s => s.Id == soundId);
            if (sound != null && File.Exists(sound.File))
            {
                if (!_audioEngine.IsSoundLoaded(soundId))
                {
                    _audioEngine.LoadSound(soundId, sound.File);
                }
                _audioEngine.Play(soundId, sound.Volume);
            }
        }
    }

    [RelayCommand]
    public void OpenPacksFolder()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var packsDir = Path.Combine(appData, "ShootingKeyboard", "packs");
            Directory.CreateDirectory(packsDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = packsDir,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore launch errors
        }
    }
}
