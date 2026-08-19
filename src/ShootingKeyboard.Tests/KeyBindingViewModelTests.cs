using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using ShootingKeyboard.ViewModels;
using Xunit;

namespace ShootingKeyboard.Tests;

public class KeyBindingViewModelTests
{
    private readonly Mock<IConfigService> _configServiceMock = new();
    private readonly Mock<ISoundPackManager> _soundPackManagerMock = new();
    private readonly Mock<IAudioEngine> _audioEngineMock = new();
    private readonly Mock<IKeyboardHook> _keyboardHookMock = new();

    private readonly SoundPack _testPack;

    public KeyBindingViewModelTests()
    {
        _testPack = new SoundPack
        {
            Id = "warzone",
            Name = "Warzone",
            Sounds = new List<SoundEntry>
            {
                new SoundEntry { Id = "shot_default", DisplayName = "Default Shot" },
                new SoundEntry { Id = "shot_laser", DisplayName = "Laser Shot" },
                new SoundEntry { Id = "shot_heavy", DisplayName = "Heavy Shot" }
            }
        };

        _soundPackManagerMock.SetupGet(m => m.ActivePack).Returns(_testPack);
    }

    private KeyBindingViewModel CreateViewModel(AppConfig? config = null)
    {
        var cfg = config ?? new AppConfig
        {
            KeyBindings = new Dictionary<int, string>
            {
                { 0x41, "shot_laser" } // 'A'
            },
            KeyVolumeOverrides = new Dictionary<int, float>
            {
                { 0x41, 0.6f }
            },
            GroupBindings = new Dictionary<string, string>
            {
                { KeyGroups.WASD, "shot_heavy" }
            },
            GroupVolumeOverrides = new Dictionary<string, float>
            {
                { KeyGroups.WASD, 0.8f }
            }
        };

        _configServiceMock.Setup(c => c.Load()).Returns(cfg);

        return new KeyBindingViewModel(
            _configServiceMock.Object,
            _soundPackManagerMock.Object,
            _audioEngineMock.Object,
            _keyboardHookMock.Object);
    }

    [Fact]
    public void LoadBindings_PopulatesAvailableSounds_GroupBindings_AndCustomKeyBindings()
    {
        var vm = CreateViewModel();

        Assert.Equal(3, vm.AvailableSounds.Count);
        Assert.Equal(KeyGroups.All.Count, vm.GroupBindings.Count);
        Assert.Single(vm.CustomKeyBindings);

        var customItem = vm.CustomKeyBindings.First();
        Assert.Equal(0x41, customItem.KeyCode);
        Assert.Equal("A", customItem.KeyName);
        Assert.NotNull(customItem.SelectedSound);
        Assert.Equal("shot_laser", customItem.SelectedSound.Id);
        Assert.Equal(0.6f, customItem.Volume);

        var wasdGroup = vm.GroupBindings.First(g => g.GroupName == KeyGroups.WASD);
        Assert.NotNull(wasdGroup.SelectedSound);
        Assert.Equal("shot_heavy", wasdGroup.SelectedSound.Id);
        Assert.Equal(0.8f, wasdGroup.Volume);
    }

    [Fact]
    public void StartCapture_SetsIsCapturingKeyAndSubscribesToHook()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsCapturingKey);
        vm.StartCapture();

        Assert.True(vm.IsCapturingKey);
        Assert.Contains("Press any key", vm.CaptureStatusText);
    }

    [Fact]
    public void CancelCapture_ResetsIsCapturingKeyAndUnsubscribes()
    {
        var vm = CreateViewModel();

        vm.StartCapture();
        Assert.True(vm.IsCapturingKey);

        vm.CancelCapture();
        Assert.False(vm.IsCapturingKey);
        Assert.Contains("Click 'Capture Key'", vm.CaptureStatusText);
    }

    [Fact]
    public void KeyPressed_WhenCapturing_CapturesKeyAndAddsItem()
    {
        var vm = CreateViewModel();

        vm.StartCapture();

        // Simulate pressing 'B' (0x42)
        var keyEvent = new KeyEvent(0x42, true);
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(keyEvent));

        Assert.False(vm.IsCapturingKey);
        Assert.Contains("Captured Key: B", vm.CaptureStatusText);
        Assert.Equal(2, vm.CustomKeyBindings.Count);

        var addedItem = vm.CustomKeyBindings.FirstOrDefault(k => k.KeyCode == 0x42);
        Assert.NotNull(addedItem);
        Assert.Equal("B", addedItem.KeyName);
        Assert.Equal("shot_default", addedItem.SelectedSound?.Id);
        Assert.Equal(1.0f, addedItem.Volume);
    }

    [Fact]
    public void KeyPressed_WhenCapturingKeyUp_IgnoresKeyUp()
    {
        var vm = CreateViewModel();

        vm.StartCapture();

        // Simulate key up event
        var keyEvent = new KeyEvent(0x42, false);
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(keyEvent));

        // Should still be capturing
        Assert.True(vm.IsCapturingKey);
        Assert.Single(vm.CustomKeyBindings);
    }

    [Fact]
    public void KeyPressed_WhenKeyAlreadyExists_DoesNotAddDuplicate()
    {
        var vm = CreateViewModel(); // Has 0x41 ('A') already

        vm.StartCapture();

        // Simulate pressing 'A' (0x41)
        var keyEvent = new KeyEvent(0x41, true);
        _keyboardHookMock.Raise(k => k.KeyPressed += null, new KeyPressedEventArgs(keyEvent));

        Assert.False(vm.IsCapturingKey);
        Assert.Single(vm.CustomKeyBindings); // Still only 1
    }

    [Fact]
    public void RemoveKeyBinding_RemovesSpecifiedItem()
    {
        var vm = CreateViewModel();
        var itemToRemove = vm.CustomKeyBindings.First();

        vm.RemoveKeyBinding(itemToRemove);

        Assert.Empty(vm.CustomKeyBindings);
    }

    [Fact]
    public void PlayPreview_CallsAudioEnginePlay()
    {
        var vm = CreateViewModel();

        vm.PlayPreview("shot_laser");

        _audioEngineMock.Verify(a => a.Play("shot_laser", 1.0f), Times.Once);
    }

    [Fact]
    public void PlayPreview_NullOrEmpty_DoesNotCallAudioEngine()
    {
        var vm = CreateViewModel();

        vm.PlayPreview(null);
        vm.PlayPreview("");

        _audioEngineMock.Verify(a => a.Play(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void Save_PersistsBindingsAndVolumeOverridesAndInvokesRequestClose()
    {
        var config = new AppConfig
        {
            KeyBindings = new Dictionary<int, string>
            {
                { 0x41, "shot_default" }
            }
        };
        var vm = CreateViewModel(config);

        var closeRequested = false;
        vm.RequestClose += () => closeRequested = true;

        // Change a group binding & volume
        var spaceGroup = vm.GroupBindings.First(g => g.GroupName == KeyGroups.Space);
        spaceGroup.SelectedSound = _testPack.Sounds[1]; // shot_laser
        spaceGroup.Volume = 0.75f;

        // Change a custom key binding & volume
        var customKey = vm.CustomKeyBindings.First();
        customKey.SelectedSound = _testPack.Sounds[2]; // shot_heavy
        customKey.Volume = 0.5f;

        // Act
        vm.Save();

        // Verify config saved
        _configServiceMock.Verify(c => c.Save(It.Is<AppConfig>(cfg =>
            cfg.GroupBindings[KeyGroups.Space] == "shot_laser" &&
            cfg.GroupVolumeOverrides[KeyGroups.Space] == 0.75f &&
            cfg.KeyBindings[0x41] == "shot_heavy" &&
            cfg.KeyVolumeOverrides[0x41] == 0.5f
        )), Times.Once);

        Assert.True(closeRequested);
    }
}
