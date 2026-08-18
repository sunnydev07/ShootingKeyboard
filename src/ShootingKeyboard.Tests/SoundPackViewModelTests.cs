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
    }

    private SoundPackViewModel CreateViewModel(AppConfig? config = null)
    {
        var cfg = config ?? new AppConfig { ActivePackId = "warzone" };
        _configServiceMock.Setup(c => c.Load()).Returns(cfg);

        return new SoundPackViewModel(
            _soundPackManagerMock.Object,
            _audioEngineMock.Object,
            _configServiceMock.Object);
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
}
