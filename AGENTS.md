# Shooting Keyboard Agent Guide

Use this file as the first-stop project map for coding agents. It explains what this repository is, where behavior lives, and how to safely make changes.

## Project in one sentence

Shooting Keyboard is a Windows-only .NET 8 WPF desktop app that runs in the system tray, hooks system-wide keyboard events with Win32 `WH_KEYBOARD_LL`, resolves each key press to a sound in the active sound pack, plays it through NAudio with low latency, and optionally shows a click-through combo/ripple overlay.

## Tech stack

- Language/framework: C# 12, .NET 8, WPF (`net8.0-windows`).
- UI pattern: MVVM using `CommunityToolkit.Mvvm` source generators (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- Audio: `NAudio` with cached PCM samples and mixer playback.
- Tray: `H.NotifyIcon.Wpf`.
- DI: `Microsoft.Extensions.DependencyInjection` configured in `App.xaml.cs`.
- Tests: xUnit + Moq in `src/ShootingKeyboard.Tests`.
- Installer: WixSharp script in `installer/BuildInstaller.csx`.

## Repository layout

```text
ShootingKeyboard.sln
src/
  ShootingKeyboard/                 Main WPF app
    App.xaml(.cs)                   Startup, single-instance guard, DI registration
    MainWindow.xaml(.cs)            Hidden tray-host shell window
    Models/                         Config, sound-pack, key-event, and key-group models
    Services/                       Keyboard hook, audio, config, packs, bindings, tray, startup, overlay
    ViewModels/                     Main orchestration and window view-models
    Views/                          Settings, key binding, and sound-pack windows
    Overlay/                        Transparent click-through combo/ripple overlay
    Resources/                      App icon and bundled default sound packs
  ShootingKeyboard.Tests/           Unit/integration-style tests
sound-packs/                        Standalone bundled sound packs used in dev/distribution
docs/                               User docs and sound-pack schema
installer/                          WixSharp MSI build script
dist/                               Release artifact(s); do not edit generated output
```

## Runtime flow

1. `App.Application_Startup` creates a named mutex (`ShootingKeyboard_SingleInstance_Mutex`), loads config, applies startup registration, resolves `MainViewModel`, calls `Initialize()`, opens the settings window, and shows a tray notification.
2. `MainViewModel.Initialize()` applies config to audio/combo/overlay services, refreshes sound packs, selects the active pack, preloads active pack sounds, subscribes to hook/combo/tray events, and starts `KeyboardHookService` when enabled.
3. `KeyboardHookService` runs a dedicated STA thread with a Win32 message pump and installs `SetWindowsHookEx(WH_KEYBOARD_LL)`. It converts native messages into `Models.KeyEvent` and raises `KeyPressed` asynchronously via the thread pool so the hook callback returns quickly.
4. `MainViewModel.OnKeyPressed` ignores key-up events and disabled/muted states, registers combo progress, resolves a sound ID through `BindingResolver`, swaps in combo variants when applicable, calculates pitch boost, lazy-loads the sound if needed, plays via `IAudioEngine.PlayWithPitch`, and updates the overlay on the WPF dispatcher.
5. Settings/key-binding/sound-pack windows mutate `AppConfig` through `ConfigService`; runtime changes are applied to services after save.

## Core domain terms

- **Sound pack**: A folder containing `pack.json` and audio files. Packs can live in `%AppData%/ShootingKeyboard/packs`, beside the executable under `sound-packs`, or under `Resources/DefaultSounds`.
- **Sound entry**: One item in `SoundPack.Sounds`; has `id`, `file`, `displayName`, optional `group`, `volume`, and combo metadata.
- **Active pack**: The pack selected by `AppConfig.ActivePackId` and held by `SoundPackManager.ActivePack`.
- **Key group**: Logical group from `KeyGroups` such as `WASD`, `Letters`, `Space`, `Enter`, `FKeys`, `Numpad`, etc.
- **Binding resolution**: The precedence chain that chooses a sound for a key: explicit key binding -> explicit group binding -> pack group sound -> pack default/base sound.
- **Combo tier**: Tier from `ComboTracker` based on rapid keypress count. Thresholds are 5, 10, 20, and 40, yielding tiers 1-4.
- **Performance mode**: Config mode that disables overlay visuals and combo pitch/variant effects in the main keypress path.

## Important files and responsibilities

### Startup/composition

- `src/ShootingKeyboard/App.xaml.cs`
  - Owns DI setup, global exception handlers, single-instance mutex, startup path, and app shutdown cleanup.
  - Register new services/view-models here.
- `src/ShootingKeyboard/ViewModels/MainViewModel.cs`
  - Central coordinator. Wires services together and handles keyboard events, tray commands, active pack audio loading, and child windows.
  - Changes to runtime behavior usually pass through this file.

### Models

- `src/ShootingKeyboard/Models/AppConfig.cs`
  - `AppConfig`, `SoundPack`, `SoundEntry`, `PackDefaults`, and `KeyGroups` live here.
  - Config JSON is `%AppData%/ShootingKeyboard/config.json` by default.
  - `AppConfig.Validate()` clamps `MasterVolume` and `ComboWindowMs`; extend validation when adding persisted settings.
- `src/ShootingKeyboard/Models/KeyEvent.cs`
  - Immutable translated keyboard-hook event.
- `src/ShootingKeyboard/Models/KeyBinding.cs`
  - Simple key-to-sound binding model.

### Services

- `KeyboardHookService.cs`
  - Native low-level keyboard hook. Keep hook callback non-blocking. Heavy work belongs outside the callback.
  - Has `SimulateKeyEvent` for tests.
- `AudioEngineService.cs`
  - Loads audio files into `float[]` sample cache, normalizes to 44.1 kHz stereo, and adds cached providers to an NAudio mixer for playback.
  - Output fallback order: `DirectSoundOut`, `WasapiOut`, `WaveOutEvent`.
- `SoundPackManager.cs`
  - Discovers packs, deserializes `pack.json`, resolves relative sound file paths to absolute paths, de-duplicates by pack ID, and tracks active pack.
- `BuiltInSoundPackFactory.cs`
  - Generates default packs into AppData when missing. This is synthesis code, not the primary pack loader.
- `BindingResolver.cs`
  - Encodes the sound-resolution precedence chain. Update this and its tests together when binding rules change.
- `ConfigService.cs`
  - Loads/saves JSON config, caches loaded config, writes atomically via `.tmp` + `File.Move(..., overwrite: true)`, falls back to defaults on corrupt files.
- `ComboTracker.cs`
  - Timer-backed combo state machine. Emits `ComboChanged` and `TierChanged`.
- `OverlayManager.cs` + `Overlay/OverlayWindow.xaml(.cs)`
  - Creates and controls a transparent topmost click-through overlay on the WPF dispatcher.
- `TrayIconManager.cs`
  - H.NotifyIcon tray icon, context menu, and notifications.
- `StartupManager.cs`
  - HKCU `Software\Microsoft\Windows\CurrentVersion\Run` integration.

### UI/view-models

- `SettingsViewModel.cs` + `Views/SettingsWindow.xaml(.cs)`
  - Main settings dashboard: volume, mute/enabled, active pack, combo window, overlay, startup, performance mode.
- `KeyBindingViewModel.cs` + `Views/KeyBindingWindow.xaml(.cs)`
  - Group bindings and custom per-key capture. Capture subscribes temporarily to `IKeyboardHook.KeyPressed`.
- `SoundPackViewModel.cs` + `Views/SoundPackWindow.xaml(.cs)`
  - Pack browsing, preview, active-pack selection, and opening the user packs folder.

## Sound-pack format

Read `docs/SOUND_PACK_FORMAT.md` for the full schema. The important shape is:

```json
{
  "id": "warzone",
  "name": "Warzone",
  "author": "ShootingKeyboard",
  "description": "...",
  "defaults": { "volume": 0.85, "comboWindowMs": 400 },
  "sounds": [
    { "id": "shot_default", "file": "shot_default.wav", "displayName": "Assault Rifle Shot", "group": null, "volume": 1.0, "isComboVariant": false, "comboTier": 0 },
    { "id": "shot_space", "file": "shot_space.wav", "displayName": "Heavy Shotgun Blast", "group": "Space", "volume": 1.0, "isComboVariant": false, "comboTier": 0 },
    { "id": "shot_combo1", "file": "shot_combo1.wav", "displayName": "Enhanced Fire (Tier 1)", "isComboVariant": true, "comboTier": 1 }
  ]
}
```

Existing bundled packs use the convention:

- one base/default sound with `group: null`
- optional `Space`, `Enter`, and `WASD` group sounds
- four combo variants with `comboTier` 1-4

When adding/changing bundled packs, keep `sound-packs/<PackName>` and `src/ShootingKeyboard/Resources/DefaultSounds/<PackName>` in sync unless deliberately changing distribution behavior.

## Build, test, and run commands

From repo root on a Windows machine with .NET 8 SDK installed:

```powershell
dotnet restore
dotnet build -c Release
dotnet test
dotnet run --project src/ShootingKeyboard/ShootingKeyboard.csproj
```

Publish portable self-contained executable:

```powershell
dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true -o dist/ShootingKeyboard-v1.0.0-win-x64
```

Build MSI after publishing:

```powershell
dotnet-script installer/BuildInstaller.csx
```

Current environment note from discovery: `dotnet test --no-restore` failed because no .NET SDK is installed/visible in this shell. Use a Windows/.NET 8 SDK environment for verification.

## Testing map

Tests are organized by production responsibility:

- `AudioEngineTests.cs`: volume clamping, load/unload, safe playback.
- `BindingResolverTests.cs`: precedence chain and fallback behavior.
- `ComboTrackerTests.cs`: combo increments, tier transitions, reset, timeout.
- `ConfigServiceTests.cs`: defaults, round-trip save/load, corrupt-file fallback, reset.
- `KeyboardHookTests.cs`: key event models, simulated hook events, start/stop/dispose behavior.
- `KeyGroupsTests.cs`: virtual-key-code group classification.
- `MainViewModelTests.cs`: initialization, keypress orchestration, tray toggles, disposal.
- `SettingsViewModelTests.cs`: load/save settings and runtime side effects.
- `KeyBindingViewModelTests.cs`: capture lifecycle, binding save, preview.
- `SoundPackManagerTests.cs`: discovery and active-pack selection.
- `SoundPackViewModelTests.cs`: pack list, activation, preview, opening folder.
- `StartupManagerTests.cs`: registry operations are safe.
- `AudioGenerator.cs`: generates all sound packs; treat as a utility-style test because it writes pack files.

When fixing behavior, add or update the closest test file. For hook/audio/UI behavior that cannot be fully automated, document the manual Windows verification you performed.

## Common change recipes

### Add a persisted setting

1. Add property and JSON name to `AppConfig` in `Models/AppConfig.cs`.
2. Extend `Validate()` if the value has bounds.
3. Add observable property in the relevant ViewModel, usually `SettingsViewModel`.
4. Load it in `LoadFromConfig()` and save/apply it in `Save()`.
5. Add the WPF control in the matching `.xaml` view.
6. Update tests for config round-trip and settings save/apply behavior.

### Change key-to-sound behavior

1. Update `KeyGroups.GetGroupForKey()` if group membership changes.
2. Update `BindingResolver.ResolveSound()` if precedence/fallback changes.
3. Update `MainViewModel.OnKeyPressed()` only if the runtime playback path changes.
4. Update `KeyGroupsTests`, `BindingResolverTests`, and relevant `MainViewModelTests`.

### Add or modify a sound pack

1. Update `sound-packs/<PackName>/pack.json` and audio files.
2. Mirror the same files under `src/ShootingKeyboard/Resources/DefaultSounds/<PackName>` for bundled app resources.
3. If the pack is generated by default at runtime, update `BuiltInSoundPackFactory` too.
4. Run sound-pack discovery tests and manually preview sounds in the Sound Pack window.

### Change keyboard hook behavior

1. Keep `HookCallback` fast and non-blocking.
2. Preserve `CallNextHookEx` so normal keyboard input continues to other applications.
3. Marshal native data into `KeyEvent`; do downstream work in subscribers, not in the hook callback.
4. Add tests using `KeyboardHookService.SimulateKeyEvent` where possible.
5. Manually test in ordinary and elevated windows if the change touches Windows input behavior.

### Change audio behavior

1. Avoid per-key file I/O on the hot path; sounds should be cached before playback or lazy-loaded once.
2. Keep sample format assumptions consistent with `WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)`.
3. Test load/unload/volume behavior and manually type rapidly to check for clipping, latency, and exceptions.

## Gotchas and cautions

- This is Windows-specific. WPF, Win32 keyboard hooks, registry startup, and H.NotifyIcon behavior are not cross-platform.
- Standard-user processes may not intercept keys typed into elevated/admin windows because of Windows UIPI. Running the app as administrator is the workaround.
- `ConfigService.Load()` returns a cached `AppConfig` instance. Code often mutates that instance and calls `Save()`. Be mindful of shared mutable config state.
- `SoundPackManager.Refresh()` calls `BuiltInSoundPackFactory.EnsureDefaultPacks()` and can create files under `%AppData%/ShootingKeyboard/packs`.
- `SoundPackManager` mutates deserialized `SoundEntry.File` values to absolute paths. UI/preview code expects absolute paths after discovery.
- `MainViewModel.Dispose()` disposes singleton services; avoid double-disposal surprises when changing app shutdown.
- `AudioEngineService` creates real audio outputs in its constructor. Unit tests may be environment-sensitive if audio devices are unavailable.
- `installer/BuildInstaller.csx` references `LICENSE.txt`, but this repository snapshot did not show that file at the root. Check before relying on the installer build.
- `dist/` contains generated release artifacts. Do not treat it as source of truth.

## Documentation pointers

- User-facing install and usage guide: `docs/USER_GUIDE.md`.
- Custom sound-pack schema and recommendations: `docs/SOUND_PACK_FORMAT.md`.
- Original feature plan/spec list: `project_plan.md`.
- Public summary and project tree: `README.md`.
