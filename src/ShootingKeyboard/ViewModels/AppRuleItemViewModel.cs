using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;

namespace ShootingKeyboard.ViewModels;

public sealed partial class AppRuleItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private bool _disableSounds;

    [ObservableProperty]
    private bool _muteOnly;

    [ObservableProperty]
    private string? _soundPackIdOverride;

    [ObservableProperty]
    private string? _profileIdOverride;

    public AppRuleItemViewModel(AppRule? rule = null)
    {
        if (rule != null)
        {
            ProcessName = rule.ProcessName;
            DisableSounds = rule.DisableSounds;
            MuteOnly = rule.MuteOnly;
            SoundPackIdOverride = rule.SoundPackIdOverride;
            ProfileIdOverride = rule.ProfileIdOverride;
        }
    }

    public AppRule ToModel()
    {
        return new AppRule
        {
            ProcessName = ProcessName.Trim(),
            DisableSounds = DisableSounds,
            MuteOnly = MuteOnly,
            SoundPackIdOverride = string.IsNullOrWhiteSpace(SoundPackIdOverride) ? null : SoundPackIdOverride,
            ProfileIdOverride = string.IsNullOrWhiteSpace(ProfileIdOverride) ? null : ProfileIdOverride
        };
    }
}

public sealed partial class AppRulesViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly IForegroundAppService _foregroundAppService;
    private readonly ISoundPackManager _soundPackManager;
    private readonly IProfileManager _profileManager;
    private readonly ITrayIconManager _trayIconManager;

    public ObservableCollection<AppRuleItemViewModel> Rules { get; } = new();
    public ObservableCollection<SoundPack> AvailablePacks { get; } = new();
    public ObservableCollection<AppProfile> AvailableProfiles { get; } = new();

    public event Action? RequestClose;

    public AppRulesViewModel(
        IConfigService configService,
        IForegroundAppService foregroundAppService,
        ISoundPackManager soundPackManager,
        IProfileManager profileManager,
        ITrayIconManager trayIconManager)
    {
        _configService = configService;
        _foregroundAppService = foregroundAppService;
        _soundPackManager = soundPackManager;
        _profileManager = profileManager;
        _trayIconManager = trayIconManager;

        LoadRules();
    }

    public void LoadRules()
    {
        var config = _configService.Load();

        AvailablePacks.Clear();
        foreach (var pack in _soundPackManager.GetPacks())
        {
            AvailablePacks.Add(pack);
        }

        AvailableProfiles.Clear();
        foreach (var profile in _profileManager.GetProfiles(config))
        {
            AvailableProfiles.Add(profile);
        }

        Rules.Clear();
        if (config.AppRules != null)
        {
            foreach (var rule in config.AppRules)
            {
                Rules.Add(new AppRuleItemViewModel(rule));
            }
        }
    }

    [RelayCommand]
    public void AddRule()
    {
        Rules.Add(new AppRuleItemViewModel { ProcessName = "new_process" });
    }

    [RelayCommand]
    public void AddCurrentApp()
    {
        var currentApp = _foregroundAppService.GetForegroundApp();
        var procName = currentApp?.ProcessName ?? "current_app";
        Rules.Add(new AppRuleItemViewModel { ProcessName = procName });
    }

    [RelayCommand]
    public void RemoveRule(AppRuleItemViewModel? rule)
    {
        if (rule != null)
        {
            Rules.Remove(rule);
        }
    }

    [RelayCommand]
    public void Save()
    {
        var config = _configService.Load();
        config.AppRules = Rules
            .Where(r => !string.IsNullOrWhiteSpace(r.ProcessName))
            .Select(r => r.ToModel())
            .ToList();

        _configService.Save(config);
        _trayIconManager.ShowNotification("Shooting Keyboard", "App rules saved successfully", BalloonIcon.Info);
        RequestClose?.Invoke();
    }
}
