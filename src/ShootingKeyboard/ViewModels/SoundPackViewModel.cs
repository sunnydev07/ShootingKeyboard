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
    private readonly ISoundPackValidator _soundPackValidator;

    public ObservableCollection<SoundPack> Packs { get; } = new();
    public ObservableCollection<SoundPackValidationIssue> SelectedPackIssues { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetActiveCommand))]
    private SoundPack? _selectedPack;

    [ObservableProperty]
    private string _activePackId = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetActiveCommand))]
    private bool _selectedPackIsValid;

    [ObservableProperty]
    private string _selectedPackValidationSummary = string.Empty;

    public SoundPackViewModel(
        ISoundPackManager soundPackManager,
        IAudioEngine audioEngine,
        IConfigService configService,
        ISoundPackValidator soundPackValidator)
    {
        _soundPackManager = soundPackManager;
        _audioEngine = audioEngine;
        _configService = configService;
        _soundPackValidator = soundPackValidator;

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

        ValidateCurrentPack();
    }

    partial void OnSelectedPackChanged(SoundPack? value)
    {
        ValidateCurrentPack();
    }

    [RelayCommand]
    public void ValidateSelectedPack()
    {
        ValidateCurrentPack();
    }

    private void ValidateCurrentPack()
    {
        SelectedPackIssues.Clear();

        if (SelectedPack == null)
        {
            SelectedPackIsValid = false;
            SelectedPackValidationSummary = string.Empty;
            SetActiveCommand.NotifyCanExecuteChanged();
            return;
        }

        var result = _soundPackValidator.Validate(SelectedPack);
        foreach (var issue in result.Issues)
        {
            SelectedPackIssues.Add(issue);
        }

        SelectedPackIsValid = result.IsValid;
        var errorCount = result.Issues.Count(i => i.Severity == SoundPackValidationSeverity.Error);
        var warningCount = result.Issues.Count(i => i.Severity == SoundPackValidationSeverity.Warning);

        if (result.IsValid && warningCount == 0)
        {
            SelectedPackValidationSummary = "Pack is valid";
        }
        else if (result.IsValid && warningCount > 0)
        {
            SelectedPackValidationSummary = $"0 error(s), {warningCount} warning(s)";
        }
        else
        {
            SelectedPackValidationSummary = $"{errorCount} error(s), {warningCount} warning(s)";
        }

        SetActiveCommand.NotifyCanExecuteChanged();
    }

    private bool CanSetActive => SelectedPack != null && SelectedPackIsValid;

    [RelayCommand(CanExecute = nameof(CanSetActive))]
    public void SetActive()
    {
        if (SelectedPack != null && SelectedPackIsValid)
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
