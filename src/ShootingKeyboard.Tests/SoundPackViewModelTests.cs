using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using ShootingKeyboard.ViewModels;
using Xunit;

namespace ShootingKeyboard.Tests;

public class SoundPackViewModelTests
{
    private readonly Mock<ISoundPackManager> _soundPackManagerMock = new();
    private readonly Mock<IAudioEngine> _audioEngineMock = new();
    private readonly Mock<IConfigService> _configServiceMock = new();
    private readonly Mock<ISoundPackValidator> _soundPackValidatorMock = new();
    private readonly Mock<ISoundPackImportExportService> _packImportExportServiceMock = new();
    private readonly Mock<ITrayIconManager> _trayIconManagerMock = new();

    private readonly List<SoundPack> _testPacks;

    public SoundPackViewModelTests()
    {
        _testPacks = new List<SoundPack>
        {
            new SoundPack
            {
                Id = "warzone",
                Name = "Warzone",
                Author = "Dev",
                Description = "Tactical military firearms",
                Sounds = new List<SoundEntry>
                {
                    new SoundEntry { Id = "shot_default", DisplayName = "Assault Rifle", Volume = 0.8f, File = "shot.wav" },
                    new SoundEntry { Id = "shot_space", DisplayName = "Shotgun", Volume = 1.0f, File = "shotgun.wav" }
                }
            },
            new SoundPack
            {
                Id = "scifi",
                Name = "Sci-Fi",
                Author = "Dev",
                Description = "Futuristic laser weapons",
                Sounds = new List<SoundEntry>
                {
                    new SoundEntry { Id = "laser_blaster", DisplayName = "Laser Blaster", Volume = 0.9f, File = "laser.wav" }
                }
            }
        };

        _soundPackManagerMock.Setup(m => m.GetPacks()).Returns(_testPacks);
        _soundPackValidatorMock.Setup(v => v.Validate(It.IsAny<SoundPack>()))
            .Returns((SoundPack pack) => new SoundPackValidationResult
            {
                PackId = pack?.Id ?? "",
                PackName = pack?.Name ?? "",
                Issues = new List<SoundPackValidationIssue>()
            });
    }

    private SoundPackViewModel CreateViewModel(AppConfig? config = null)
    {
        var cfg = config ?? new AppConfig { ActivePackId = "warzone" };
        _configServiceMock.Setup(c => c.Load()).Returns(cfg);

        return new SoundPackViewModel(
            _soundPackManagerMock.Object,
            _audioEngineMock.Object,
            _configServiceMock.Object,
            _soundPackValidatorMock.Object,
            _packImportExportServiceMock.Object,
            _trayIconManagerMock.Object);
    }

    [Fact]
    public void LoadPacks_PopulatesPacksAndSelectsActivePack()
    {
        var vm = CreateViewModel();

        _soundPackManagerMock.Verify(m => m.Refresh(), Times.Once);
        Assert.Equal(2, vm.Packs.Count);
        Assert.Equal("warzone", vm.ActivePackId);
        Assert.NotNull(vm.SelectedPack);
        Assert.Equal("warzone", vm.SelectedPack.Id);
        Assert.True(vm.SelectedPackIsValid);
        Assert.Equal("Pack is valid", vm.SelectedPackValidationSummary);
    }

    [Fact]
    public void SetActive_UpdatesConfigAndSoundPackManager()
    {
        var config = new AppConfig { ActivePackId = "warzone" };
        var vm = CreateViewModel(config);

        // Select Sci-Fi pack
        vm.SelectedPack = _testPacks[1]; // scifi

        // Act
        vm.SetActive();

        // Verify config updated & saved
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.ActivePackId == "scifi")), Times.Once);
        _soundPackManagerMock.Verify(m => m.SetActivePack("scifi"), Times.Once);
        Assert.Equal("scifi", vm.ActivePackId);
    }

    [Fact]
    public void SelectInvalidPack_PopulatesIssuesSummaryAndDisablesSetActiveCommand()
    {
        var invalidPack = new SoundPack
        {
            Id = "broken",
            Name = "Broken Pack",
            Sounds = new List<SoundEntry>()
        };
        _testPacks.Add(invalidPack);

        _soundPackValidatorMock.Setup(v => v.Validate(invalidPack)).Returns(new SoundPackValidationResult
        {
            PackId = "broken",
            PackName = "Broken Pack",
            Issues = new List<SoundPackValidationIssue>
            {
                new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Error,
                    Code = "sounds.empty",
                    Message = "No sounds found."
                }
            }
        });

        var vm = CreateViewModel();
        vm.SelectedPack = invalidPack;

        Assert.False(vm.SelectedPackIsValid);
        Assert.Contains("1 error(s)", vm.SelectedPackValidationSummary);
        Assert.Single(vm.SelectedPackIssues);
        Assert.Equal("sounds.empty", vm.SelectedPackIssues[0].Code);
        Assert.False(vm.SetActiveCommand.CanExecute(null));
    }

    [Fact]
    public void SelectValidPack_EnablesSetActiveCommand()
    {
        var vm = CreateViewModel();
        vm.SelectedPack = _testPacks[0];

        Assert.True(vm.SelectedPackIsValid);
        Assert.Equal("Pack is valid", vm.SelectedPackValidationSummary);
        Assert.Empty(vm.SelectedPackIssues);
        Assert.True(vm.SetActiveCommand.CanExecute(null));
    }

    [Fact]
    public void ValidateSelectedPackCommand_TriggersValidation()
    {
        var vm = CreateViewModel();
        vm.ValidateSelectedPackCommand.Execute(null);

        _soundPackValidatorMock.Verify(v => v.Validate(It.IsAny<SoundPack>()), Times.AtLeast(2));
    }

    [Fact]
    public void PlayPreview_WhenFileExists_LoadsAndPlaysAudio()
    {
        // Create a temporary wav file for the test
        var tempWav = Path.GetTempFileName();
        try
        {
            var testPack = new SoundPack
            {
                Id = "custom",
                Name = "Custom",
                Sounds = new List<SoundEntry>
                {
                    new SoundEntry { Id = "test_sound", DisplayName = "Test", Volume = 0.75f, File = tempWav }
                }
            };
            _soundPackManagerMock.Setup(m => m.GetPacks()).Returns(new List<SoundPack> { testPack });

            var vm = CreateViewModel(new AppConfig { ActivePackId = "custom" });
            vm.SelectedPack = testPack;

            _audioEngineMock.Setup(a => a.IsSoundLoaded("test_sound")).Returns(false);

            // Act
            vm.PlayPreview("test_sound");

            // Assert
            _audioEngineMock.Verify(a => a.LoadSound("test_sound", tempWav), Times.Once);
            _audioEngineMock.Verify(a => a.Play("test_sound", 0.75f), Times.Once);
        }
        finally
        {
            if (File.Exists(tempWav))
            {
                File.Delete(tempWav);
            }
        }
    }

    [Fact]
    public void PlayPreview_NullOrEmptySoundId_DoesNotPlay()
    {
        var vm = CreateViewModel();

        vm.PlayPreview(null);
        vm.PlayPreview("");

        _audioEngineMock.Verify(a => a.Play(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void OpenPacksFolder_DoesNotThrow()
    {
        var vm = CreateViewModel();

        // Act (should handle execution gracefully)
        var ex = Record.Exception(() => vm.OpenPacksFolder());
        Assert.Null(ex);
    }

    [Fact]
    public void InstallPackZip_InstallsAndRefreshesPacks()
    {
        var vm = CreateViewModel();
        _packImportExportServiceMock.Setup(p => p.InstallFromZip("C:\\test\\pack.zip", It.IsAny<string>()))
            .Returns("scifi");

        var result = vm.InstallPackZip("C:\\test\\pack.zip");

        Assert.True(result);
        _soundPackManagerMock.Verify(m => m.Refresh(), Times.AtLeast(2)); // Initial load + after install
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", It.Is<string>(s => s.Contains("scifi")), BalloonIcon.Info), Times.Once);
    }

    [Fact]
    public void ExportSelectedPackToZip_ExportsSelectedPack()
    {
        var vm = CreateViewModel();
        vm.SelectedPack = _testPacks[0]; // warzone

        var result = vm.ExportSelectedPackToZip("C:\\test\\warzone.zip");

        Assert.True(result);
        _packImportExportServiceMock.Verify(p => p.ExportToZip(_testPacks[0], "C:\\test\\warzone.zip"), Times.Once);
        _trayIconManagerMock.Verify(t => t.ShowNotification("Shooting Keyboard", It.Is<string>(s => s.Contains("Warzone")), BalloonIcon.Info), Times.Once);
    }
}
