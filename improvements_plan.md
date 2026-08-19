# Shooting Keyboard Improvements Implementation Plan

> **For agentic workers:** Start by reading `AGENTS.md`, then implement this plan phase-by-phase. Each phase is designed to be a separate Antigravity CLI agent task/branch. Steps use checkbox (`- [ ]`) syntax for tracking. Do not start a later phase until the previous phase builds and its tests pass.

**Goal:** Improve Shooting Keyboard into a more reliable, customizable, premium-feeling Windows typing sound app without breaking the existing keyboard-hook/audio core.

**Architecture:** Keep the current MVVM + services architecture. Add focused services for diagnostics, validation, playback filtering, profiles, app rules, import/export, overlay settings, and audio devices. Preserve `MainViewModel` as the runtime orchestrator, but move new decision logic into services so it does not become a large untestable file.

**Tech Stack:** C# 12, .NET 8 WPF, CommunityToolkit.Mvvm, NAudio, H.NotifyIcon.Wpf, System.Text.Json, System.IO.Compression, xUnit, Moq.

**Spec:** `AGENTS.md` plus this phased roadmap. User-requested feature set: diagnostics, per-app rules, sound randomization, per-key/per-group volume, repeat-key filtering, sound-pack validation, live settings apply, profiles, import/export, overlay customization, audio output selector, quiet hours, and tray quick controls.

## Global Constraints

- Keep Windows-only behavior explicit; this project depends on WPF, Win32 keyboard hooks, registry startup, and NAudio Windows output APIs.
- Keep the keyboard hook callback non-blocking; never add file I/O, config writes, audio decoding, UI work, or long locks inside `KeyboardHookService.HookCallback`.
- Avoid per-keypress disk I/O. Hot path should use loaded config, cached sounds, and in-memory services.
- Preserve existing config files by adding defaults/migration code instead of requiring users to delete `%AppData%/ShootingKeyboard/config.json`.
- For each phase, add or update tests in `src/ShootingKeyboard.Tests` before or alongside production changes.
- After each phase, run `dotnet test` on a Windows machine with .NET 8 SDK installed.
- Use small commits. Recommended commit pattern: `feat: add diagnostics panel`, `test: cover repeat key filter`, `refactor: extract playback filtering`.

---

## Phase overview

1. **Phase 1 — Reliability and diagnostics:** Add runtime diagnostics panel and sound-pack validator.
2. **Phase 2 — Playback control basics:** Add repeat-key filtering/cooldowns and live settings apply.
3. **Phase 3 — Sound variety and volume control:** Add sound variants/randomization plus per-group/per-key volume overrides.
4. **Phase 4 — Profiles:** Add named profiles for work/gaming/streaming settings.
5. **Phase 5 — Per-app rules:** Add foreground-app detection and rules for mute/disable/profile/pack overrides.
6. **Phase 6 — Import/export and pack install:** Export/import profiles and zipped sound packs.
7. **Phase 7 — Overlay and tray polish:** Add overlay customization and richer tray quick menu.
8. **Phase 8 — Audio device selection and release hardening:** Select audio output device, improve installer/release checks.

---

# Phase 1 — Reliability and Diagnostics

## Outcome

Users and future agents can see why sound is or is not working. Custom sound packs can be validated before activation.

## Task 1.1: Add sound-pack validation service

**Files:**
- Create: `src/ShootingKeyboard/Models/SoundPackValidation.cs`
- Create: `src/ShootingKeyboard/Services/ISoundPackValidator.cs`
- Create: `src/ShootingKeyboard/Services/SoundPackValidator.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Test: `src/ShootingKeyboard.Tests/SoundPackValidatorTests.cs`

**Interfaces:**

```csharp
namespace ShootingKeyboard.Models;

public enum SoundPackValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed class SoundPackValidationIssue
{
    public SoundPackValidationSeverity Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SoundId { get; set; }
    public string? FilePath { get; set; }
}

public sealed class SoundPackValidationResult
{
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public List<SoundPackValidationIssue> Issues { get; set; } = new();
    public bool IsValid => Issues.All(i => i.Severity != SoundPackValidationSeverity.Error);
}
```

```csharp
namespace ShootingKeyboard.Services;

public interface ISoundPackValidator
{
    SoundPackValidationResult Validate(SoundPack pack);
}
```

**Validation rules:**

- Error `pack.id.empty` when `SoundPack.Id` is blank.
- Error `pack.name.empty` when `SoundPack.Name` is blank.
- Error `sounds.empty` when `Sounds.Count == 0`.
- Error `sound.id.empty` when a sound ID is blank.
- Error `sound.id.duplicate` for duplicate sound IDs, case-insensitive.
- Error `sound.file.empty` when a sound has no file.
- Error `sound.file.missing` when `SoundEntry.File` does not exist.
- Error `sound.volume.outOfRange` when volume is less than 0 or greater than 1.
- Error `sound.group.invalid` when `Group` is not blank and not in `KeyGroups.All`.
- Error `sound.comboTier.outOfRange` when `IsComboVariant == true` and `ComboTier` is outside 1-4.
- Warning `pack.defaultVolume.outOfRange` when `Defaults.Volume` is outside 0-1.
- Warning `pack.comboWindow.outOfRange` when `Defaults.ComboWindowMs` is outside 50-2000.

**Steps:**

- [ ] Write `SoundPackValidatorTests` covering one valid pack and each error rule above.
- [ ] Run: `dotnet test --filter SoundPackValidatorTests`. Expected first run: fails because service/models do not exist.
- [ ] Add validation models and `ISoundPackValidator`.
- [ ] Implement `SoundPackValidator.Validate(SoundPack pack)` using deterministic issue codes listed above.
- [ ] Register `ISoundPackValidator` as singleton in `App.ConfigureServices`.
- [ ] Run: `dotnet test --filter SoundPackValidatorTests`. Expected: pass.
- [ ] Commit: `feat: add sound pack validator`.

## Task 1.2: Surface validation in Sound Pack Manager UI

**Files:**
- Modify: `src/ShootingKeyboard/ViewModels/SoundPackViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SoundPackWindow.xaml`
- Test: `src/ShootingKeyboard.Tests/SoundPackViewModelTests.cs`

**Interfaces:**

Add to `SoundPackViewModel`:

```csharp
public ObservableCollection<SoundPackValidationIssue> SelectedPackIssues { get; }
public bool SelectedPackIsValid { get; }
public string SelectedPackValidationSummary { get; }
public IRelayCommand ValidateSelectedPackCommand { get; }
```

**UI behavior:**

- Show summary text under selected pack description.
- Summary format when valid: `Pack is valid`.
- Summary format when invalid: `{errorCount} error(s), {warningCount} warning(s)`.
- Show issue rows with severity, code, message, and sound ID.
- Disable `Set as Active Sound Pack` when selected pack has validation errors.

**Steps:**

- [ ] Add tests: selecting an invalid pack populates `SelectedPackIssues`, summary includes `error(s)`, and `SetActiveCommand.CanExecute(null)` is false.
- [ ] Inject `ISoundPackValidator` into `SoundPackViewModel`.
- [ ] Validate selected pack in `LoadPacks()` and whenever `SelectedPack` changes. Use CommunityToolkit partial method `partial void OnSelectedPackChanged(SoundPack? value)`.
- [ ] Replace `[RelayCommand] public void SetActive()` with `[RelayCommand(CanExecute = nameof(CanSetActive))]` and call `SetActiveCommand.NotifyCanExecuteChanged()` after validation.
- [ ] Add validation summary and issue list to `SoundPackWindow.xaml`.
- [ ] Run: `dotnet test --filter "SoundPackViewModelTests|SoundPackValidatorTests"`.
- [ ] Commit: `feat: show sound pack validation in manager`.

## Task 1.3: Add runtime diagnostics service and diagnostics window

**Files:**
- Create: `src/ShootingKeyboard/Models/RuntimeDiagnostics.cs`
- Create: `src/ShootingKeyboard/Services/IRuntimeDiagnosticsService.cs`
- Create: `src/ShootingKeyboard/Services/RuntimeDiagnosticsService.cs`
- Create: `src/ShootingKeyboard/ViewModels/DiagnosticsViewModel.cs`
- Create: `src/ShootingKeyboard/Views/DiagnosticsWindow.xaml`
- Create: `src/ShootingKeyboard/Views/DiagnosticsWindow.xaml.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Modify: `src/ShootingKeyboard/Services/ITrayIconManager.cs`
- Modify: `src/ShootingKeyboard/Services/TrayIconManager.cs`
- Test: `src/ShootingKeyboard.Tests/RuntimeDiagnosticsServiceTests.cs`
- Test: `src/ShootingKeyboard.Tests/MainViewModelTests.cs`

**Interfaces:**

```csharp
namespace ShootingKeyboard.Models;

public sealed class RuntimeDiagnosticsSnapshot
{
    public DateTimeOffset CreatedAt { get; set; }
    public bool KeyboardHookRunning { get; set; }
    public bool AppEnabled { get; set; }
    public bool Muted { get; set; }
    public string ActivePackId { get; set; } = string.Empty;
    public string ActivePackName { get; set; } = string.Empty;
    public int AvailablePackCount { get; set; }
    public int LoadedSoundCount { get; set; }
    public string ConfigPath { get; set; } = string.Empty;
    public string LastKey { get; set; } = string.Empty;
    public string LastResolvedSoundId { get; set; } = string.Empty;
    public string LastPlayedSoundId { get; set; } = string.Empty;
    public string LastPlaybackResult { get; set; } = string.Empty;
    public DateTimeOffset? LastEventAt { get; set; }
}
```

```csharp
namespace ShootingKeyboard.Services;

public interface IRuntimeDiagnosticsService
{
    void RecordKeyEvent(KeyEvent keyEvent);
    void RecordResolvedSound(int keyCode, string? soundId);
    void RecordPlayback(string soundId, bool played, string reason);
    RuntimeDiagnosticsSnapshot CreateSnapshot(
        AppConfig config,
        IKeyboardHook keyboardHook,
        IAudioEngine audioEngine,
        ISoundPackManager soundPackManager,
        string configPath);
}
```

Also add to `IConfigService`:

```csharp
string ConfigPath { get; }
```

**Steps:**

- [ ] Write `RuntimeDiagnosticsServiceTests` verifying records are reflected in `CreateSnapshot()`.
- [ ] Add `ConfigPath` getter to `IConfigService` and `ConfigService`.
- [ ] Implement runtime diagnostics models/service.
- [ ] Register `IRuntimeDiagnosticsService` and `DiagnosticsViewModel` in DI.
- [ ] Update `MainViewModel.OnKeyPressed` to call diagnostics methods for ignored key-up, disabled/muted, no active pack, no resolved sound, unloaded file missing, and successful playback.
- [ ] Add `DiagnosticsViewModel.RefreshCommand` that reloads config and exposes a `Snapshot` property.
- [ ] Add `DiagnosticsWindow` showing all snapshot fields and a Refresh button.
- [ ] Add tray event `DiagnosticsRequested` to `ITrayIconManager`; add menu item `Diagnostics` in `TrayIconManager`.
- [ ] Add `MainViewModel.ShowDiagnosticsWindow()` and wire tray event.
- [ ] Run: `dotnet test --filter "RuntimeDiagnosticsServiceTests|MainViewModelTests"`.
- [ ] Manual Windows check: launch app, open tray menu, click Diagnostics, press keys, refresh, verify last key and sound update.
- [ ] Commit: `feat: add runtime diagnostics panel`.

---

# Phase 2 — Playback Control Basics

## Outcome

Users can prevent sound spam from held keys and hear settings changes immediately while editing.

## Task 2.1: Add repeat-key filtering and cooldown service

**Files:**
- Create: `src/ShootingKeyboard/Models/PlaybackFilterConfig.cs`
- Create: `src/ShootingKeyboard/Services/IKeyPressFilter.cs`
- Create: `src/ShootingKeyboard/Services/KeyPressFilter.cs`
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Test: `src/ShootingKeyboard.Tests/KeyPressFilterTests.cs`
- Test: `src/ShootingKeyboard.Tests/MainViewModelTests.cs`

**Config model:**

```csharp
public sealed class PlaybackFilterConfig
{
    public bool IgnoreKeyRepeats { get; set; } = true;
    public int GlobalCooldownMs { get; set; } = 20;
    public Dictionary<string, int> GroupCooldownMs { get; set; } = new();
    public Dictionary<int, int> KeyCooldownMs { get; set; } = new();
}
```

Add to `AppConfig`:

```csharp
[JsonPropertyName("playbackFilter")]
public PlaybackFilterConfig PlaybackFilter { get; set; } = new();
```

Extend `Validate()`:

- Clamp `GlobalCooldownMs` to 0-1000.
- Clamp every group/key cooldown to 0-5000.
- Remove group cooldown entries whose key is not in `KeyGroups.All`.

**Service interface:**

```csharp
public interface IKeyPressFilter
{
    bool ShouldProcess(KeyEvent keyEvent, AppConfig config);
    void Reset();
}
```

**Behavior:**

- On key-up: remove key from pressed set and return false.
- If `IgnoreKeyRepeats` is true and the same key is already pressed: return false.
- Cooldown precedence: key cooldown -> group cooldown -> global cooldown.
- If cooldown is 0, allow immediate repeats after a new accepted keydown.

**Steps:**

- [ ] Write `KeyPressFilterTests` for first keydown allowed, repeated keydown blocked, keyup resets pressed state, global cooldown blocks rapid accepted keydowns, key cooldown overrides global cooldown, group cooldown overrides global cooldown.
- [ ] Add model and config property.
- [ ] Implement `KeyPressFilter` using `DateTimeOffset.UtcNow`; keep time source simple in production and allow a constructor-injected `Func<DateTimeOffset>` for tests.
- [ ] Register `IKeyPressFilter` in DI.
- [ ] Update `MainViewModel.OnKeyPressed` so it calls `ShouldProcess(e.KeyEvent, config)` before combo/audio work. Remove the current early `if (!e.IsPressed) return;` and let the filter observe key-up events.
- [ ] Record blocked events in diagnostics with reason `filtered`.
- [ ] Run: `dotnet test --filter "KeyPressFilterTests|MainViewModelTests|ConfigServiceTests"`.
- [ ] Commit: `feat: add repeat key filtering and cooldowns`.

## Task 2.2: Add playback filter controls to Settings

**Files:**
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SettingsWindow.xaml`
- Test: `src/ShootingKeyboard.Tests/SettingsViewModelTests.cs`

**ViewModel properties:**

```csharp
[ObservableProperty] private bool _ignoreKeyRepeats;
[ObservableProperty] private int _globalCooldownMs;
```

**UI controls:**

- Checkbox: `Ignore held-key repeat events`.
- Slider: `Global Sound Cooldown`, minimum 0, maximum 250, tick 5, display `{0}ms`.

**Steps:**

- [ ] Add test verifying `LoadFromConfig()` reads `PlaybackFilter.IgnoreKeyRepeats` and `GlobalCooldownMs`.
- [ ] Add test verifying `Save()` writes these values to config.
- [ ] Add properties and load/save mapping.
- [ ] Add controls to `SettingsWindow.xaml` under audio/options.
- [ ] Run: `dotnet test --filter SettingsViewModelTests`.
- [ ] Commit: `feat: expose playback filtering settings`.

## Task 2.3: Make simple settings apply live

**Files:**
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Test: `src/ShootingKeyboard.Tests/SettingsViewModelTests.cs`

**Live-apply behavior:**

- `MasterVolume` immediately calls `_audioEngine.SetMasterVolume(value)`.
- `IsMuted` immediately calls `_audioEngine.SetMuted(value)`.
- `ComboWindowMs` immediately updates `_comboTracker.ComboWindowMs`.
- `OverlayEnabled` and `PerformanceMode` immediately update `_overlayManager.IsEnabled = OverlayEnabled && !PerformanceMode`.
- `StartWithWindows` does not apply until Save, because it writes registry.
- `SelectedPack` applies on Save only in this phase, because active-pack changes reload audio and should stay transactional.
- `Cancel` should close without saving. Because runtime-only changes may already have applied, `SettingsWindow` closing via Cancel should call a new `SettingsViewModel.RevertRuntimeChanges()` before closing.

**Steps:**

- [ ] Add tests verifying property changes call runtime services before Save.
- [ ] Add tests verifying `RevertRuntimeChanges()` restores runtime service state from persisted config.
- [ ] Implement CommunityToolkit partial property methods: `OnMasterVolumeChanged`, `OnIsMutedChanged`, `OnComboWindowMsChanged`, `OnOverlayEnabledChanged`, `OnPerformanceModeChanged`.
- [ ] Guard live-apply during initial load with a private `_isLoading` bool.
- [ ] Update `SettingsWindow.CancelButton_Click` to call `_viewModel.RevertRuntimeChanges()` before `Close()`.
- [ ] Run: `dotnet test --filter SettingsViewModelTests`.
- [ ] Manual Windows check: open settings, move volume slider, press Test, hear immediate volume change; press Cancel and verify persisted config is unchanged after reopening.
- [ ] Commit: `feat: apply settings changes live`.

---

# Phase 3 — Sound Variety and Volume Control

## Outcome

Typing sounds less repetitive and users can balance loud/quiet keys.

## Task 3.1: Extend sound pack schema with variants

**Files:**
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs` (`SoundEntry` lives here)
- Modify: `src/ShootingKeyboard/Services/SoundPackManager.cs`
- Modify: `docs/SOUND_PACK_FORMAT.md`
- Test: `src/ShootingKeyboard.Tests/SoundPackManagerTests.cs`
- Test: `src/ShootingKeyboard.Tests/SoundPackValidatorTests.cs`

**Model change:**

Add to `SoundEntry`:

```csharp
[JsonPropertyName("variants")]
public List<string> Variants { get; set; } = new();
```

**Loader behavior:**

- `SoundPackManager` resolves every `Variants` entry to an absolute path exactly like `SoundEntry.File`.
- Existing packs with no `variants` continue to load unchanged.
- Validator adds error `sound.variant.missing` when a variant file path does not exist.

**Steps:**

- [ ] Add test with a temporary pack whose sound has `variants: ["a.wav", "b.wav"]`; assert loaded variant paths are absolute.
- [ ] Add validator test for missing variant file.
- [ ] Update `SoundEntry`, loader path resolution, and validator.
- [ ] Update `docs/SOUND_PACK_FORMAT.md` with `variants` example.
- [ ] Run: `dotnet test --filter "SoundPackManagerTests|SoundPackValidatorTests"`.
- [ ] Commit: `feat: support sound entry variant files`.

## Task 3.2: Add sound variant selector and load variant audio

**Files:**
- Create: `src/ShootingKeyboard/Services/ISoundVariantSelector.cs`
- Create: `src/ShootingKeyboard/Services/SoundVariantSelector.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Test: `src/ShootingKeyboard.Tests/SoundVariantSelectorTests.cs`
- Test: `src/ShootingKeyboard.Tests/MainViewModelTests.cs`

**Interface:**

```csharp
public sealed class SelectedSoundClip
{
    public string AudioId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public float Volume { get; set; } = 1.0f;
}

public interface ISoundVariantSelector
{
    string GetVariantAudioId(string soundId, int variantIndex);
    SelectedSoundClip SelectClip(SoundEntry soundEntry);
}
```

**Behavior:**

- For base file: `AudioId == soundEntry.Id`, `FilePath == soundEntry.File`.
- For variant index N: `AudioId == $"{soundEntry.Id}::variant::{N}"`.
- `SelectClip` randomly chooses base file plus variants, all with equal probability.
- Use injectable `Random` or `Func<int, int>` so tests can force selection.

**MainViewModel changes:**

- `LoadActivePackAudio()` loads `sound.Id` for `sound.File` and generated variant IDs for each variant path.
- `OnKeyPressed()` resolves base sound entry, then calls `ISoundVariantSelector.SelectClip(soundEntry)`, then plays `SelectedSoundClip.AudioId`.
- If selected clip is not loaded but file exists, lazy-load it once.

**Steps:**

- [ ] Write selector tests for no variants, deterministic variant ID, and forced random variant selection.
- [ ] Add selector service/interface and DI registration.
- [ ] Update active pack audio loading.
- [ ] Update keypress playback path.
- [ ] Update diagnostics to record final selected `AudioId`.
- [ ] Run: `dotnet test --filter "SoundVariantSelectorTests|MainViewModelTests"`.
- [ ] Manual Windows check: add two variant files to a test pack and verify repeated typing rotates between clips.
- [ ] Commit: `feat: randomize sound variants`.

## Task 3.3: Add per-group and per-key volume overrides

**Files:**
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs`
- Modify: `src/ShootingKeyboard/ViewModels/KeyBindingViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/KeyBindingWindow.xaml`
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Test: `src/ShootingKeyboard.Tests/KeyBindingViewModelTests.cs`
- Test: `src/ShootingKeyboard.Tests/MainViewModelTests.cs`
- Test: `src/ShootingKeyboard.Tests/ConfigServiceTests.cs`

**Config properties:**

```csharp
[JsonPropertyName("groupVolumeOverrides")]
public Dictionary<string, float> GroupVolumeOverrides { get; set; } = new();

[JsonPropertyName("keyVolumeOverrides")]
public Dictionary<int, float> KeyVolumeOverrides { get; set; } = new();
```

**Effective volume formula:**

```text
effectiveVolume = soundEntry.Volume * selectedClip.Volume * groupVolume * keyVolume
```

Where missing `groupVolume` or `keyVolume` defaults to `1.0`.

**ViewModel item changes:**

- `GroupBindingItem` gets `[ObservableProperty] private float _volume = 1.0f;`.
- `KeyBindingItem` gets `[ObservableProperty] private float _volume = 1.0f;`.

**UI:**

- Add volume slider beside each group and custom key binding, min 0, max 1.

**Steps:**

- [ ] Add config round-trip test for both dictionaries.
- [ ] Add key binding tests verifying load/save of group/key volume values.
- [ ] Add main view model test verifying `PlayWithPitch` receives volume multiplied by group/key override.
- [ ] Add config properties and validation clamp to 0-1.
- [ ] Update KeyBindingViewModel load/save.
- [ ] Update KeyBindingWindow XAML sliders.
- [ ] Update MainViewModel effective volume calculation.
- [ ] Run: `dotnet test --filter "KeyBindingViewModelTests|MainViewModelTests|ConfigServiceTests"`.
- [ ] Commit: `feat: add per key and group volume overrides`.

---

# Phase 4 — Profiles

## Outcome

Users can switch complete configurations for work, gaming, streaming, and quiet use.

## Task 4.1: Add profile data model and profile manager

**Files:**
- Create: `src/ShootingKeyboard/Models/AppProfile.cs`
- Create: `src/ShootingKeyboard/Services/IProfileManager.cs`
- Create: `src/ShootingKeyboard/Services/ProfileManager.cs`
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Test: `src/ShootingKeyboard.Tests/ProfileManagerTests.cs`
- Test: `src/ShootingKeyboard.Tests/ConfigServiceTests.cs`

**Models:**

```csharp
public sealed class AppProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float MasterVolume { get; set; } = 0.7f;
    public bool IsMuted { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string ActivePackId { get; set; } = "warzone";
    public bool OverlayEnabled { get; set; } = true;
    public bool PerformanceMode { get; set; }
    public int ComboWindowMs { get; set; } = 400;
    public Dictionary<int, string> KeyBindings { get; set; } = new();
    public Dictionary<string, string> GroupBindings { get; set; } = new();
    public Dictionary<string, float> GroupVolumeOverrides { get; set; } = new();
    public Dictionary<int, float> KeyVolumeOverrides { get; set; } = new();
    public PlaybackFilterConfig PlaybackFilter { get; set; } = new();
}
```

Add to `AppConfig`:

```csharp
[JsonPropertyName("activeProfileId")]
public string ActiveProfileId { get; set; } = "default";

[JsonPropertyName("profiles")]
public List<AppProfile> Profiles { get; set; } = new();
```

**Profile manager interface:**

```csharp
public interface IProfileManager
{
    IReadOnlyList<AppProfile> GetProfiles(AppConfig config);
    AppProfile GetActiveProfile(AppConfig config);
    AppProfile CreateProfile(AppConfig config, string name);
    bool DeleteProfile(AppConfig config, string profileId);
    bool SetActiveProfile(AppConfig config, string profileId);
    void CopyRootSettingsToActiveProfile(AppConfig config);
    void ApplyActiveProfileToRootSettings(AppConfig config);
}
```

**Migration behavior:**

- If `Profiles` is empty, create one profile: `{ Id = "default", Name = "Default" }` using current root config values.
- Keep root config properties as the runtime source of truth for now. Profile activation copies profile settings into root properties, then saves config.

**Steps:**

- [ ] Write tests for migration from old config, create profile, delete non-active profile, reject deleting last profile, activate profile copies settings to root.
- [ ] Add model and service.
- [ ] Register `IProfileManager` in DI.
- [ ] Update `ConfigService.Load()` after deserialization to ensure profiles exist and active profile is applied to root.
- [ ] Update `ConfigService.Save()` or settings save flow to copy root settings back to active profile before writing.
- [ ] Run: `dotnet test --filter "ProfileManagerTests|ConfigServiceTests"`.
- [ ] Commit: `feat: add profile model and manager`.

## Task 4.2: Add profile UI to settings

**Files:**
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SettingsWindow.xaml`
- Test: `src/ShootingKeyboard.Tests/SettingsViewModelTests.cs`

**ViewModel additions:**

```csharp
public ObservableCollection<AppProfile> Profiles { get; }
[ObservableProperty] private AppProfile? _selectedProfile;
[ObservableProperty] private string _newProfileName = string.Empty;
```

Commands:

- `CreateProfileCommand`: creates profile from current saved config when `NewProfileName` is not blank.
- `DeleteSelectedProfileCommand`: deletes selected profile when more than one exists.
- `ActivateSelectedProfileCommand`: activates selected profile and reloads UI/runtime.

**UI:**

- Add `PROFILE` section at top of Settings.
- ComboBox of profiles.
- TextBox for new profile name.
- Buttons: `Create`, `Activate`, `Delete`.

**Steps:**

- [ ] Add tests for loading profiles, creating profile, activating profile updates runtime settings, deleting profile refuses last profile.
- [ ] Inject `IProfileManager`.
- [ ] Implement properties and commands.
- [ ] Add XAML controls.
- [ ] Run: `dotnet test --filter SettingsViewModelTests`.
- [ ] Manual Windows check: create `Gaming`, change pack/volume, save; create `Work`, lower volume, save; switch profiles and verify settings reload.
- [ ] Commit: `feat: add profile management UI`.

---

# Phase 5 — Per-App Rules

## Outcome

Users can automatically mute, disable, switch profile, or switch pack based on the active foreground application.

## Task 5.1: Add foreground app detection and rule evaluator

**Files:**
- Create: `src/ShootingKeyboard/Models/AppRule.cs`
- Create: `src/ShootingKeyboard/Services/IForegroundAppService.cs`
- Create: `src/ShootingKeyboard/Services/ForegroundAppService.cs`
- Create: `src/ShootingKeyboard/Services/IAppRuleEvaluator.cs`
- Create: `src/ShootingKeyboard/Services/AppRuleEvaluator.cs`
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Test: `src/ShootingKeyboard.Tests/AppRuleEvaluatorTests.cs`

**Models:**

```csharp
public sealed class ForegroundAppInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public string MainWindowTitle { get; set; } = string.Empty;
}

public sealed class AppRule
{
    public string ProcessName { get; set; } = string.Empty;
    public bool DisableSounds { get; set; }
    public bool MuteOnly { get; set; }
    public string? ProfileIdOverride { get; set; }
    public string? SoundPackIdOverride { get; set; }
}

public sealed class AppRuleDecision
{
    public bool ShouldPlay { get; set; } = true;
    public string? ProfileIdOverride { get; set; }
    public string? SoundPackIdOverride { get; set; }
    public string Reason { get; set; } = "no-rule";
}
```

Add to `AppConfig`:

```csharp
[JsonPropertyName("appRules")]
public List<AppRule> AppRules { get; set; } = new();
```

**Rule matching:**

- Match `ProcessName` case-insensitively after trimming `.exe` from both sides.
- First matching rule wins.
- `DisableSounds` returns `ShouldPlay = false` and reason `disabled-by-app-rule`.
- `MuteOnly` returns `ShouldPlay = false` and reason `muted-by-app-rule`.
- Pack/profile overrides return `ShouldPlay = true` with override IDs.

**Steps:**

- [ ] Write rule evaluator tests for no rule, disabled rule, muted rule, pack override, profile override, and `.exe` suffix normalization.
- [ ] Implement models/services.
- [ ] Register services in DI.
- [ ] Run: `dotnet test --filter AppRuleEvaluatorTests`.
- [ ] Commit: `feat: add foreground app rule evaluator`.

## Task 5.2: Apply app rules on keypress

**Files:**
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Test: `src/ShootingKeyboard.Tests/MainViewModelTests.cs`

**Behavior:**

- At the start of accepted keypress processing, call `IForegroundAppService.GetForegroundApp()`.
- Evaluate rules with current config.
- If rule says not to play, record diagnostics and return before combo/audio.
- If `SoundPackIdOverride` is set and exists, use that pack for this keypress without changing persisted `ActivePackId`.
- Profile override can be added as a second step after Phase 4 is stable: use the profile's settings for pack/bindings/volume during this keypress without saving config.

**Steps:**

- [ ] Add MainViewModel test where foreground app rule disables playback and `PlayWithPitch` is not called.
- [ ] Add MainViewModel test where foreground app rule uses pack override.
- [ ] Inject `IForegroundAppService` and `IAppRuleEvaluator`.
- [ ] Apply app rule decision in `OnKeyPressed()`.
- [ ] Add diagnostics reason for blocked app rule.
- [ ] Run: `dotnet test --filter MainViewModelTests`.
- [ ] Commit: `feat: apply per app playback rules`.

## Task 5.3: Add app rules UI

**Files:**
- Create: `src/ShootingKeyboard/ViewModels/AppRuleItemViewModel.cs`
- Create: `src/ShootingKeyboard/Views/AppRulesWindow.xaml`
- Create: `src/ShootingKeyboard/Views/AppRulesWindow.xaml.cs`
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SettingsWindow.xaml`
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Test: `src/ShootingKeyboard.Tests/SettingsViewModelTests.cs`

**UI behavior:**

- Settings button: `Per-App Rules...`.
- Rules grid fields: Process Name, Disable Sounds, Mute Only, Sound Pack Override, Profile Override.
- Button: `Add Current App` uses `IForegroundAppService.GetForegroundApp()` and fills process name.
- Save writes `AppConfig.AppRules`.

**Steps:**

- [ ] Add tests for adding current app rule and saving app rules.
- [ ] Implement AppRules window and view model using observable collection.
- [ ] Wire from Settings to MainViewModel like KeyBinding and SoundPack windows.
- [ ] Run: `dotnet test --filter SettingsViewModelTests`.
- [ ] Manual Windows check: add `notepad` disabled rule, type in Notepad, verify no sound; type elsewhere, verify sound resumes.
- [ ] Commit: `feat: add per app rules UI`.

---

# Phase 6 — Import/Export and Pack Install

## Outcome

Users can share profiles and install zipped sound packs without manually copying files.

## Task 6.1: Add profile import/export service

**Files:**
- Create: `src/ShootingKeyboard/Services/IProfileImportExportService.cs`
- Create: `src/ShootingKeyboard/Services/ProfileImportExportService.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Test: `src/ShootingKeyboard.Tests/ProfileImportExportServiceTests.cs`

**Interface:**

```csharp
public interface IProfileImportExportService
{
    void ExportProfile(AppProfile profile, string filePath);
    AppProfile ImportProfile(string filePath);
}
```

**Behavior:**

- Export writes indented JSON.
- Import reads JSON, validates non-empty `Id` and `Name`, validates/clamps profile values using the same bounds as `AppConfig.Validate()`.
- If imported profile ID collides with an existing profile, UI layer assigns a new ID before saving.

**Steps:**

- [ ] Write tests for export round-trip, invalid JSON throws `InvalidDataException`, missing name throws `InvalidDataException`.
- [ ] Implement service.
- [ ] Register service.
- [ ] Run: `dotnet test --filter ProfileImportExportServiceTests`.
- [ ] Commit: `feat: add profile import export service`.

## Task 6.2: Add sound pack zip install/export service

**Files:**
- Create: `src/ShootingKeyboard/Services/ISoundPackImportExportService.cs`
- Create: `src/ShootingKeyboard/Services/SoundPackImportExportService.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Test: `src/ShootingKeyboard.Tests/SoundPackImportExportServiceTests.cs`

**Interface:**

```csharp
public interface ISoundPackImportExportService
{
    string InstallFromZip(string zipFilePath, string userPacksRoot);
    void ExportToZip(SoundPack pack, string zipFilePath);
}
```

**Behavior:**

- Install extracts to a temporary folder first.
- Zip must contain exactly one `pack.json` somewhere inside it.
- Validate extracted pack before moving into `%AppData%/ShootingKeyboard/packs/<pack-id>`.
- Destination folder name uses sanitized pack ID: lowercase letters, numbers, hyphen, underscore.
- Existing destination is replaced only after successful validation.
- Export includes `pack.json`, main sound files, and variant files.

**Steps:**

- [ ] Write tests using temporary zip files: valid install, missing pack.json rejected, missing audio rejected, export contains pack.json and audio file.
- [ ] Implement with `System.IO.Compression.ZipFile`.
- [ ] Register service.
- [ ] Run: `dotnet test --filter SoundPackImportExportServiceTests`.
- [ ] Commit: `feat: add sound pack zip import export`.

## Task 6.3: Add import/export buttons to UI

**Files:**
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Modify: `src/ShootingKeyboard/ViewModels/SoundPackViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SettingsWindow.xaml`
- Modify: `src/ShootingKeyboard/Views/SoundPackWindow.xaml`
- Test: `src/ShootingKeyboard.Tests/SettingsViewModelTests.cs`
- Test: `src/ShootingKeyboard.Tests/SoundPackViewModelTests.cs`

**UI behavior:**

- Settings profile section buttons: `Import Profile`, `Export Selected Profile`.
- Sound Pack Manager buttons: `Install Pack ZIP`, `Export Selected Pack`.
- Use WPF `OpenFileDialog`/`SaveFileDialog` in view code-behind, not view model, then pass file path to view model command/service method.

**Steps:**

- [ ] Add view-model methods that accept file paths: `ImportProfileFromFile(string path)`, `ExportSelectedProfileToFile(string path)`, `InstallPackZip(string path)`, `ExportSelectedPackToZip(string path)`.
- [ ] Add tests for these methods using mocked services.
- [ ] Add XAML buttons and code-behind file dialogs.
- [ ] Run: `dotnet test --filter "SettingsViewModelTests|SoundPackViewModelTests"`.
- [ ] Manual Windows check: export a profile, import it under a new name, install a pack zip, refresh list.
- [ ] Commit: `feat: add profile and pack import export UI`.

---

# Phase 7 — Overlay and Tray Polish

## Outcome

The app feels more polished and can be controlled mostly from the tray.

## Task 7.1: Add overlay customization settings

**Files:**
- Create: `src/ShootingKeyboard/Models/OverlayConfig.cs`
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs`
- Modify: `src/ShootingKeyboard/Services/IOverlayManager.cs`
- Modify: `src/ShootingKeyboard/Services/OverlayManager.cs`
- Modify: `src/ShootingKeyboard/Overlay/OverlayWindow.xaml.cs`
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SettingsWindow.xaml`
- Test: `src/ShootingKeyboard.Tests/SettingsViewModelTests.cs`

**Config:**

```csharp
public sealed class OverlayConfig
{
    public bool ShowRipple { get; set; } = true;
    public bool ShowCombo { get; set; } = true;
    public string RippleColor { get; set; } = "#FFA500";
    public string ComboPosition { get; set; } = "TopCenter";
    public double Scale { get; set; } = 1.0;
}
```

Add to `AppConfig`:

```csharp
[JsonPropertyName("overlay")]
public OverlayConfig Overlay { get; set; } = new();
```

**Overlay manager API:**

```csharp
void ApplyConfig(OverlayConfig config);
```

**Steps:**

- [ ] Add config validation: accepted `ComboPosition` values are `TopCenter`, `TopLeft`, `TopRight`, `BottomCenter`; invalid values become `TopCenter`; scale clamps to 0.5-2.0; invalid colors become `#FFA500`.
- [ ] Add tests for load/save/apply settings.
- [ ] Implement `ApplyConfig` in overlay manager/window.
- [ ] Add settings controls: ripple checkbox, combo checkbox, color textbox, position combo, scale slider.
- [ ] Run: `dotnet test --filter SettingsViewModelTests`.
- [ ] Manual Windows check: change color and scale, press keys, verify overlay updates.
- [ ] Commit: `feat: add overlay customization`.

## Task 7.2: Add tray quick controls

**Files:**
- Modify: `src/ShootingKeyboard/Services/ITrayIconManager.cs`
- Modify: `src/ShootingKeyboard/Services/TrayIconManager.cs`
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Test: `src/ShootingKeyboard.Tests/MainViewModelTests.cs`

**Tray menu additions:**

- `Profiles` submenu with profile names.
- `Sound Packs` submenu with pack names.
- `Volume` submenu: 25%, 50%, 75%, 100%.
- `Overlay On/Off` item.
- Existing Diagnostics item remains from Phase 1.

**Interface addition:**

```csharp
void RebuildQuickMenus(IReadOnlyList<AppProfile> profiles, string activeProfileId, IReadOnlyList<SoundPack> packs, string activePackId);
event EventHandler<string>? ProfileSelected;
event EventHandler<string>? SoundPackSelected;
event EventHandler<float>? VolumeSelected;
event EventHandler? ToggleOverlayRequested;
```

**Steps:**

- [ ] Add MainViewModel tests for pack selection, volume selection, overlay toggle, and profile selection from tray events.
- [ ] Implement tray submenus.
- [ ] Rebuild menu after packs refresh, profile changes, settings save, and initialization.
- [ ] Run: `dotnet test --filter MainViewModelTests`.
- [ ] Manual Windows check: switch packs and volume from tray without opening settings.
- [ ] Commit: `feat: add tray quick controls`.

## Task 7.3: Add quiet hours

**Files:**
- Create: `src/ShootingKeyboard/Models/QuietHoursConfig.cs`
- Create: `src/ShootingKeyboard/Services/IQuietHoursService.cs`
- Create: `src/ShootingKeyboard/Services/QuietHoursService.cs`
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs`
- Modify: `src/ShootingKeyboard/App.xaml.cs`
- Modify: `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SettingsWindow.xaml`
- Test: `src/ShootingKeyboard.Tests/QuietHoursServiceTests.cs`
- Test: `src/ShootingKeyboard.Tests/MainViewModelTests.cs`

**Config:**

```csharp
public sealed class QuietHoursConfig
{
    public bool Enabled { get; set; }
    public TimeSpan Start { get; set; } = new(22, 0, 0);
    public TimeSpan End { get; set; } = new(8, 0, 0);
}
```

**Service:**

```csharp
public interface IQuietHoursService
{
    bool IsQuietNow(QuietHoursConfig config, DateTimeOffset now);
}
```

**Behavior:**

- If disabled, return false.
- If start < end, quiet when `time >= start && time < end`.
- If start > end, quiet across midnight when `time >= start || time < end`.
- If start == end, quiet all day.

**Steps:**

- [ ] Write tests for disabled, same-day range, across-midnight range, and all-day range.
- [ ] Implement model/service and DI registration.
- [ ] Apply in `MainViewModel.OnKeyPressed()` before combo/audio.
- [ ] Add settings controls for enabled/start/end.
- [ ] Run: `dotnet test --filter "QuietHoursServiceTests|MainViewModelTests|SettingsViewModelTests"`.
- [ ] Commit: `feat: add quiet hours`.

---

# Phase 8 — Audio Device Selection and Release Hardening

## Outcome

Power users can route sounds to a selected output device, and release packaging is less fragile.

## Task 8.1: Add audio output device selection

**Files:**
- Create: `src/ShootingKeyboard/Models/AudioDeviceInfo.cs`
- Modify: `src/ShootingKeyboard/Services/IAudioEngine.cs`
- Modify: `src/ShootingKeyboard/Services/AudioEngineService.cs`
- Modify: `src/ShootingKeyboard/Models/AppConfig.cs`
- Modify: `src/ShootingKeyboard/ViewModels/SettingsViewModel.cs`
- Modify: `src/ShootingKeyboard/Views/SettingsWindow.xaml`
- Test: `src/ShootingKeyboard.Tests/AudioEngineTests.cs`
- Test: `src/ShootingKeyboard.Tests/SettingsViewModelTests.cs`

**Model:**

```csharp
public sealed class AudioDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
```

**IAudioEngine additions:**

```csharp
IReadOnlyList<AudioDeviceInfo> GetOutputDevices();
string? CurrentOutputDeviceId { get; }
bool SetOutputDevice(string? deviceId);
```

**Config addition:**

```csharp
[JsonPropertyName("audioOutputDeviceId")]
public string? AudioOutputDeviceId { get; set; }
```

**Audio behavior:**

- `null` or empty device ID means Windows default output.
- When changing device, dispose current output, create new output, reattach mixer, continue using existing sample cache.
- If selected device no longer exists, fall back to default and return false from `SetOutputDevice`.

**Steps:**

- [ ] Add tests for default/null device behavior using an abstraction if hardware-specific code prevents deterministic unit tests.
- [ ] Add `GetOutputDevices()` with `MMDeviceEnumerator`.
- [ ] Update `InitializeOutput` to prefer selected device for Wasapi when available, then fallback to existing DirectSound/Wasapi/WaveOut strategy.
- [ ] Add settings ComboBox with `System Default` plus device list.
- [ ] Save selected device ID in config.
- [ ] Run: `dotnet test --filter "AudioEngineTests|SettingsViewModelTests"`.
- [ ] Manual Windows check: switch between speakers/headphones or default device and verify playback.
- [ ] Commit: `feat: add audio output device selector`.

## Task 8.2: Harden installer and release docs

**Files:**
- Modify: `installer/BuildInstaller.csx`
- Modify: `README.md`
- Modify: `docs/USER_GUIDE.md`
- Create if missing: `LICENSE.txt`

**Known issue from current repo scan:**

- `installer/BuildInstaller.csx` references `LICENSE.txt`, but the root did not contain that file during discovery.

**Steps:**

- [ ] Add `LICENSE.txt` containing the same license named in README, or update installer script to omit `LicenceFile` when `LICENSE.txt` is missing.
- [ ] Make installer script verify published exe exists before creating MSI. If missing, print the exact publish command and exit with non-zero status.
- [ ] Update README with new features: diagnostics, profiles, per-app rules, import/export, variants, audio device selector, quiet hours.
- [ ] Update User Guide with screenshots or text walkthroughs for new settings sections.
- [ ] Run: `dotnet build -c Release`.
- [ ] Run: `dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true`.
- [ ] Run: `dotnet-script installer/BuildInstaller.csx`.
- [ ] Manual Windows check: install MSI, launch app, verify tray icon, settings, diagnostics, pack list, and uninstall cleanup.
- [ ] Commit: `chore: harden installer and release docs`.

---

# Recommended Antigravity CLI execution strategy

Run each phase in a fresh branch or worktree. Give the agent only one phase at a time.

Example prompts:

```text
Read AGENTS.md and improvements_plan.md. Implement Phase 1 only. Follow the tests-first steps, keep commits small, and stop after dotnet test passes or after reporting the exact blocker.
```

```text
Read AGENTS.md and improvements_plan.md. Implement Phase 2 only. Do not change profile/app-rule/import-export code. Add/update tests listed in Phase 2 and run dotnet test.
```

After each phase:

- [ ] Review `git diff`.
- [ ] Run `dotnet test` yourself in a Windows .NET 8 SDK shell.
- [ ] Launch the app manually for phases that affect runtime UI/audio.
- [ ] Merge only when the phase is stable.

# Final acceptance checklist

- [ ] Diagnostics window explains hook/audio/config/pack status and last key/sound.
- [ ] Sound Pack Manager blocks invalid packs and shows useful validation messages.
- [ ] Held-key repeats and rapid spam can be filtered.
- [ ] Volume/mute/combo/overlay settings apply live.
- [ ] Sound packs support random variants without breaking existing packs.
- [ ] Group and custom key bindings support volume overrides.
- [ ] Profiles can be created, activated, deleted safely, imported, and exported.
- [ ] Per-app rules can mute/disable/switch behavior for foreground apps.
- [ ] Zipped sound packs can be installed and exported from the UI.
- [ ] Overlay can be customized.
- [ ] Tray menu supports quick profile/pack/volume/overlay changes.
- [ ] Quiet hours mute playback on schedule.
- [ ] Audio can be routed to default or a selected output device.
- [ ] Installer build no longer fails due to missing license/publish artifact assumptions.
- [ ] `dotnet test` passes on Windows with .NET 8 SDK.
