using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;

namespace ShootingKeyboard.ViewModels;

public sealed partial class KeyBindingViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly ISoundPackManager _soundPackManager;
    private readonly IAudioEngine _audioEngine;
    private readonly IKeyboardHook _keyboardHook;

    public ObservableCollection<GroupBindingItem> GroupBindings { get; } = new();
    public ObservableCollection<KeyBindingItem> CustomKeyBindings { get; } = new();
    public ObservableCollection<SoundEntry> AvailableSounds { get; } = new();

    [ObservableProperty]
    private bool _isCapturingKey;

    [ObservableProperty]
    private string _captureStatusText = "Click 'Add Key' to capture";

    public event Action? RequestClose;

    public KeyBindingViewModel(
        IConfigService configService,
        ISoundPackManager soundPackManager,
        IAudioEngine audioEngine,
        IKeyboardHook keyboardHook)
    {
        _configService = configService;
        _soundPackManager = soundPackManager;
        _audioEngine = audioEngine;
        _keyboardHook = keyboardHook;

        LoadBindings();
    }

    public void LoadBindings()
    {
        var config = _configService.Load();
        var activePack = _soundPackManager.ActivePack;

        AvailableSounds.Clear();
        if (activePack != null)
        {
            foreach (var sound in activePack.Sounds)
            {
                AvailableSounds.Add(sound);
            }
        }

        // Populate Group Bindings
        GroupBindings.Clear();
        foreach (var groupName in KeyGroups.All)
        {
            var currentSoundId = config.GroupBindings.GetValueOrDefault(groupName, string.Empty);
            var selectedSound = AvailableSounds.FirstOrDefault(s => s.Id.Equals(currentSoundId, StringComparison.OrdinalIgnoreCase));

            var item = new GroupBindingItem(groupName, AvailableSounds, selectedSound);
            GroupBindings.Add(item);
        }

        // Populate Custom Key Bindings
        CustomKeyBindings.Clear();
        foreach (var (keyCode, soundId) in config.KeyBindings)
        {
            var keyName = GetKeyName(keyCode);
            var selectedSound = AvailableSounds.FirstOrDefault(s => s.Id.Equals(soundId, StringComparison.OrdinalIgnoreCase));
            CustomKeyBindings.Add(new KeyBindingItem(keyCode, keyName, AvailableSounds, selectedSound));
        }
    }

    [RelayCommand]
    public void StartCapture()
    {
        if (IsCapturingKey)
            return;

        IsCapturingKey = true;
        CaptureStatusText = "Press any key on your keyboard...";
        _keyboardHook.KeyPressed += OnKeyCaptured;
    }

    [RelayCommand]
    public void CancelCapture()
    {
        if (IsCapturingKey)
        {
            _keyboardHook.KeyPressed -= OnKeyCaptured;
            IsCapturingKey = false;
            CaptureStatusText = "Click 'Capture Key' to add key";
        }
    }

    private void OnKeyCaptured(object? sender, KeyPressedEventArgs e)
    {
        if (!e.IsPressed) return;

        _keyboardHook.KeyPressed -= OnKeyCaptured;

        void ApplyKeyCaptured()
        {
            IsCapturingKey = false;
            CaptureStatusText = $"Captured Key: {GetKeyName(e.KeyCode)} (0x{e.KeyCode:X2})";

            // Add or select existing binding
            var existing = CustomKeyBindings.FirstOrDefault(k => k.KeyCode == e.KeyCode);
            if (existing == null)
            {
                var defaultSound = AvailableSounds.FirstOrDefault();
                var newItem = new KeyBindingItem(e.KeyCode, GetKeyName(e.KeyCode), AvailableSounds, defaultSound);
                CustomKeyBindings.Add(newItem);
            }
        }

        if (System.Windows.Application.Current != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(ApplyKeyCaptured);
        }
        else
        {
            ApplyKeyCaptured();
        }
    }

    [RelayCommand]
    public void RemoveKeyBinding(KeyBindingItem? item)
    {
        if (item != null)
        {
            CustomKeyBindings.Remove(item);
        }
    }

    [RelayCommand]
    public void PlayPreview(string? soundId)
    {
        if (!string.IsNullOrEmpty(soundId))
        {
            _audioEngine.Play(soundId, 1.0f);
        }
    }

    [RelayCommand]
    public void Save()
    {
        var config = _configService.Load();

        // Update group bindings
        config.GroupBindings.Clear();
        foreach (var group in GroupBindings)
        {
            if (group.SelectedSound != null && !string.IsNullOrEmpty(group.SelectedSound.Id))
            {
                config.GroupBindings[group.GroupName] = group.SelectedSound.Id;
            }
        }

        // Update key bindings
        config.KeyBindings.Clear();
        foreach (var key in CustomKeyBindings)
        {
            if (key.SelectedSound != null && !string.IsNullOrEmpty(key.SelectedSound.Id))
            {
                config.KeyBindings[key.KeyCode] = key.SelectedSound.Id;
            }
        }

        _configService.Save(config);
        RequestClose?.Invoke();
    }

    private static string GetKeyName(int virtualKeyCode)
    {
        return virtualKeyCode switch
        {
            0x20 => "Space",
            0x0D => "Enter",
            0x09 => "Tab",
            0x08 => "Backspace",
            0x1B => "Escape",
            0x10 => "Shift",
            0x11 => "Ctrl",
            0x12 => "Alt",
            0x25 => "Left Arrow",
            0x26 => "Up Arrow",
            0x27 => "Right Arrow",
            0x28 => "Down Arrow",
            0x2E => "Delete",
            0x2D => "Insert",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "Page Up",
            0x22 => "Page Down",
            >= 0x41 and <= 0x5A => ((char)virtualKeyCode).ToString(),
            >= 0x30 and <= 0x39 => ((char)virtualKeyCode).ToString(),
            >= 0x70 and <= 0x87 => $"F{virtualKeyCode - 0x70 + 1}",
            _ => $"Key (0x{virtualKeyCode:X2})"
        };
    }
}

public sealed partial class GroupBindingItem : ObservableObject
{
    public string GroupName { get; }
    public ObservableCollection<SoundEntry> AvailableSounds { get; }

    [ObservableProperty]
    private SoundEntry? _selectedSound;

    public GroupBindingItem(string groupName, ObservableCollection<SoundEntry> sounds, SoundEntry? selected)
    {
        GroupName = groupName;
        AvailableSounds = sounds;
        SelectedSound = selected;
    }
}

public sealed partial class KeyBindingItem : ObservableObject
{
    public int KeyCode { get; }
    public string KeyName { get; }
    public ObservableCollection<SoundEntry> AvailableSounds { get; }

    [ObservableProperty]
    private SoundEntry? _selectedSound;

    public KeyBindingItem(int keyCode, string keyName, ObservableCollection<SoundEntry> sounds, SoundEntry? selected)
    {
        KeyCode = keyCode;
        KeyName = keyName;
        AvailableSounds = sounds;
        SelectedSound = selected;
    }
}
