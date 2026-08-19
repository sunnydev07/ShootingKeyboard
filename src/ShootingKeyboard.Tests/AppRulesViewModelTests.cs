using System.Collections.Generic;
using System.Linq;
using Moq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using ShootingKeyboard.ViewModels;
using Xunit;

namespace ShootingKeyboard.Tests;

public class AppRulesViewModelTests
{
    private readonly Mock<IConfigService> _configServiceMock = new();
    private readonly Mock<IForegroundAppService> _foregroundAppServiceMock = new();
    private readonly Mock<ISoundPackManager> _soundPackManagerMock = new();
    private readonly Mock<IProfileManager> _profileManagerMock = new();
    private readonly Mock<ITrayIconManager> _trayIconManagerMock = new();

    private readonly SoundPack _testPack;
    private readonly AppProfile _testProfile;

    public AppRulesViewModelTests()
    {
        _testPack = new SoundPack { Id = "warzone", Name = "Warzone" };
        _testProfile = new AppProfile { Id = "default", Name = "Default" };

        _soundPackManagerMock.Setup(m => m.GetPacks()).Returns(new List<SoundPack> { _testPack });
        _profileManagerMock.Setup(p => p.GetProfiles(It.IsAny<AppConfig>())).Returns(new List<AppProfile> { _testProfile });
    }

    private AppRulesViewModel CreateViewModel(AppConfig? config = null)
    {
        var cfg = config ?? new AppConfig
        {
            AppRules = new List<AppRule>
            {
                new AppRule { ProcessName = "notepad", DisableSounds = true }
            }
        };

        _configServiceMock.Setup(c => c.Load()).Returns(cfg);

        return new AppRulesViewModel(
            _configServiceMock.Object,
            _foregroundAppServiceMock.Object,
            _soundPackManagerMock.Object,
            _profileManagerMock.Object,
            _trayIconManagerMock.Object);
    }

    [Fact]
    public void LoadRules_PopulatesRulesPacksAndProfiles()
    {
        var vm = CreateViewModel();

        Assert.Single(vm.Rules);
        Assert.Equal("notepad", vm.Rules[0].ProcessName);
        Assert.True(vm.Rules[0].DisableSounds);
        Assert.Single(vm.AvailablePacks);
        Assert.Single(vm.AvailableProfiles);
    }

    [Fact]
    public void AddCurrentApp_AddsRuleWithForegroundProcessName()
    {
        var vm = CreateViewModel();
        _foregroundAppServiceMock.Setup(f => f.GetForegroundApp())
            .Returns(new ForegroundAppInfo { ProcessName = "devenv", MainWindowTitle = "Visual Studio" });

        vm.AddCurrentAppCommand.Execute(null);

        Assert.Equal(2, vm.Rules.Count);
        Assert.Equal("devenv", vm.Rules[1].ProcessName);
    }

    [Fact]
    public void RemoveRule_RemovesSpecifiedRule()
    {
        var vm = CreateViewModel();
        var ruleToRemove = vm.Rules[0];

        vm.RemoveRuleCommand.Execute(ruleToRemove);

        Assert.Empty(vm.Rules);
    }

    [Fact]
    public void Save_PersistsAppRulesAndCloses()
    {
        var vm = CreateViewModel();
        var closeFired = false;
        vm.RequestClose += () => closeFired = true;

        vm.Rules[0].MuteOnly = true;
        vm.Rules[0].SoundPackIdOverride = "warzone";

        vm.SaveCommand.Execute(null);

        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg =>
            cfg.AppRules.Count == 1 &&
            cfg.AppRules[0].ProcessName == "notepad" &&
            cfg.AppRules[0].MuteOnly == true &&
            cfg.AppRules[0].SoundPackIdOverride == "warzone"
        )), Times.Once);

        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", "App rules saved successfully", BalloonIcon.Info), Times.Once);
        Assert.True(closeFired);
    }
}
