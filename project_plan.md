# Shooting Keyboard — Implementation Plan

## Context
Build a Windows desktop application ("Shooting Keyboard") that plays customizable sound effects (gunshots, fire, explosions, laser, etc.) for every keystroke system-wide. The app runs in the background, intercepts all keyboard input via low-level Windows hooks, and plays audio with minimal latency. Features include sound packs, per-key customization, combo multiplier, on-screen overlay, system tray, and Windows startup integration.

---

## Recommended Tech Stack: C# WPF (.NET 8) + NAudio + H.NotifyIcon + WixSharp

| Criterion | C# WPF + NAudio | Tauri + React | Electron + React | Python + PyQt |
|---|---|---|---|---|
| Keyboard: every key, any app | Native `WH_KEYBOARD_LL` (best) | `global-shortcut` only fires on combos; rdev raw hooks needed | uIOhook works but inside Chromium | pynput works but slower |
| Audio latency | WasapiOut ~10–20 ms (`WithLowLatency()`) | rodio/cpal ~15–30 ms (bridged) | Howler.js/WebAudio ~20–40 ms | pygame.mixer ~30–80 ms |
| Idle CPU/RAM | **20–50 MB**, near-zero CPU | ~80 MB | ~150–300 MB | ~60–120 MB |
| Installer | WixSharp MSI / InnoSetup EXE | Tauri bundler | electron-builder | PyInstaller + NSIS |
| Windows integration | Registry startup, tray, notifications all native | Tray plugin OK | OK but heavier | Manual registry writes |

**Why WPF wins:** The core requirement is system-wide per-keystroke interception with imperceptible latency. Tauri's `global-shortcut` plugin only registers named combos — not every keystroke. Electron ships Chromium for a native hook + audio player. C# gives direct `SetWindowsHookEx(WH_KEYBOARD_LL)` (same API rdev uses internally) and NAudio's `WasapiOut` with event-driven low-latency mode — the lowest achievable latency on Windows without ASIO.

---

## Project Structure

```
ShootingKeyboard/
├── ShootingKeyboard.sln
├── src/
│   ├── ShootingKeyboard/                 # Main WPF app
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── MainWindow.xaml / MainWindow.xaml.cs
│   │   ├── Models/
│   │   │   ├── KeyBinding.cs
│   │   │   ├── SoundPack.cs
│   │   │   ├── KeyGroup.cs
│   │   │   └── AppConfig.cs
│   │   ├── Services/
│   │   │   ├── IKeyboardHook.cs
│   │   │   ├── KeyboardHookService.cs
│   │   │   ├── IAudioEngine.cs
│   │   │   ├── AudioEngineService.cs
│   │   │   ├── SoundPackManager.cs
│   │   │   ├── ComboTracker.cs
│   │   │   ├── OverlayManager.cs
│   │   │   ├── TrayIconManager.cs
│   │   │   ├── ConfigService.cs
│   │   │   └── StartupManager.cs
│   │   ├── ViewModels/
│   │   │   ├── MainViewModel.cs
│   │   │   ├── SettingsViewModel.cs
│   │   │   ├── KeyBindingViewModel.cs
│   │   │   └── SoundPackViewModel.cs
│   │   ├── Views/
│   │   │   ├── SettingsWindow.xaml(.cs)
│   │   │   ├── KeyBindingWindow.xaml(.cs)
│   │   │   └── SoundPackWindow.xaml(.cs)
│   │   ├── Overlay/
│   │   │   ├── OverlayWindow.xaml(.cs)
│   │   ├── Resources/
│   │   │   ├── Icons/
│   │   │   └── DefaultSounds/
│   │   ├── Converters/
│   │   └── AssemblyInfo.cs
│   └── ShootingKeyboard.Tests/           # xUnit tests
├── sound-packs/ # User-addable packs
│   ├── Warzone/
│   ├── SciFi/
│   └── RetroArcade/
├── installer/
│   └── BuildInstaller.csx                # WixSharp build script
├── docs/
│   └── SOUND_PACK_FORMAT.md
└── README.md
```

---

## Feature Specs (SPEC-01 → SPEC-15)

### SPEC-01: Project Scaffolding & Application Shell
- Create `ShootingKeyboard.sln`, .NET 8 WPF project, single-instance guard (named `Mutex`).
- `App.xaml.cs` bootstraps `MainWindow` (hidden/minimized to tray) and DI container.
- **Test**: launch asserts single instance; DI resolves all singletons.

### SPEC-02: System-Wide Keyboard Hook (`KeyboardHookService`)
- P/Invoke `SetWindowsHookEx(WH_KEYBOARD_LL)`, `CallNextHookEx`, `UnhookWindowsHookEx` in `user32.dll`.
- Translate `VirtualKeyCode` + `LLKHF_UP` → `KeyEvent(KeyCode, IsPressed)`.
- Dispatch on dedicated thread; raise `KeyPressed` event. **Never block hook callback**.
- **Test**: simulate via `SendInput`; verify install/uninstall cleanly.

### SPEC-03: Low-Latency Audio Engine (`AudioEngineService`)
- Single NAudio `WasapiOut` (EventSync, `WithLowLatency()`) as mixer.
- Pre-decode all sounds (`AudioFileReader` → PCM `ISampleProvider`) into `Dictionary<string, float[]>`.
- `Play(soundId, volume)`: create `WaveProvider` from cached PCM, push to output via `MixingSampleProvider`.
- Per-sound voice pool to avoid clipping.
- **Test**: mock `IWavePlayer`; assert cache hit on repeated plays.

### SPEC-04: Configuration Model & Persistence (`ConfigService`)
- `AppConfig` (JSON): master volume, mute, global-enabled, active pack id, per-group bindings, overlay on/off, performance mode, startup flag.
- Atomic write (temp file + rename), defaults on first run.
- Path: `%AppData%/ShootingKeyboard/config.json`.
- **Test**: round-trip serialize/deserialize; corrupt-file fallback.

### SPEC-05: Key-to-Sound Binding Resolution (`BindingResolver`)
- Given `VirtualKeyCode`, resolve `soundId` via: explicit key map → group membership → pack default.
- `KeyGroup` presets: Letters, Numbers, WASD, Arrows, F-Keys, Space, Enter, Modifiers, Punctuation.
- **Test**: table-driven tests for each group; fallback chain verified.

### SPEC-06: System Tray (`TrayIconManager`)
- Use **H.NotifyIcon** for tray icon with context menu: Show Settings, Mute/Unmute, Pause/Resume, Exit.
- Balloon notifications for pack changes/errors.
- **Test**: icon shows/hides; commands fire.

### SPEC-07: Sound Pack Manager & Bundled Packs (`SoundPackManager`)
- Discover packs from `{AppDir}/Resources/DefaultSounds` and `%AppData%/ShootingKeyboard/packs`.
- `pack.json`: `{ id, name, author, sounds: [{id, file, group?}], defaults: {volume} }`.
- Bundle 3 packs: **Warzone** (gunshots, explosions), **Sci-Fi** (laser, phaser), **Retro Arcade** (8-bit blips).
- **Test**: loader parses valid packs; rejects malformed JSON.

### SPEC-08: Main Settings UI (`SettingsViewModel` + `SettingsWindow`)
- Controls: master volume slider, mute toggle, global enable/pause, active pack selector, "Start with Windows" checkbox, overlay toggle, performance mode checkbox.
- Two-way binding via `INotifyPropertyChanged`; persists via `ConfigService`.
- **Test**: VM changes propagate to config save; no UI thread leaks.

### SPEC-09: Key Binding Customization UI (`KeyBindingWindow`)
- Grid of key groups + "capture key" mode (press a key to assign).
- Map group or individual key to any sound in active pack.
- Drag-and-drop or dropdown sound selection.
- **Test**: assignment updates `AppConfig`; resolution reflects new mapping.

### SPEC-10: Sound Preview
- "Play" button next to each sound in `SoundPackWindow` and binding UI.
- Routes through `AudioEngineService.Play` (no special path).
- **Test**: preview triggers `Play` with correct soundId/volume.

### SPEC-11: Combo Multiplier System (`ComboTracker`)
- State machine: keypress within `comboWindowMs` (default 400 ms) increments combo; resets after silence.
- Combo tiers (1–4 base, 5–9 +pitch, 10+ +extra layer) select alternate `soundId` or apply pitch/volume boost.
- Exposes combo count for overlay.
- **Test**: simulated rapid presses increment; timeout resets; tier transitions correct.

### SPEC-12: On-Screen Overlay (`OverlayManager` + `OverlayWindow`)
- Transparent, `Topmost`, `WS_EX_TRANSPARENT | WS_EX_LAYERED` WPF window, click-through.
- Renders ripple/flash at last keypress coordinates; shows combo counter.
- Only active when enabled and not in performance mode.
- **Test**: `Show/Update/Hide` lifecycle; `IsHitTestVisible=false`.

### SPEC-13: Start with Windows (`StartupManager`)
- Write/remove `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ShootingKeyboard` → exe path.
- Toggle mirrors SPEC-08 checkbox.
- **Test**: registry key added/removed; path points to running exe.

### SPEC-14: Performance Mode
- When enabled: disable overlay, reduce audio voice pool, skip combo visuals, lower hook thread priority, disable logging.
- Central `PerformanceConfig` consumed by all services.
- **Test**: toggling mid-session disposes overlay/shrinks pool without dropping audio.

### SPEC-15: Windows Installer (WixSharp)
- `BuildInstaller.csx` produces MSI: installs to `ProgramFiles\ShootingKeyboard`, Start Menu shortcut, writes startup if checked, bundles .NET 8 runtime or self-contained publish.
- Digital-signing step (optional, `signtool`).
- **Test**: install on clean VM, verify exe runs, tray appears, uninstall cleans registry.

---

## Dependencies / Libraries

| Library | Purpose | Version |
|---|---|---|
| **.NET 8 SDK** | Runtime/framework | LTS |
| **NAudio** | Low-latency audio (WasapiOut) | ~2.2.x |
| **H.NotifyIcon.Wpf** | System tray | Latest |
| **Microsoft.Extensions.DependencyInjection** | Lightweight DI | Latest |
| **CommunityToolkit.Mvvm** | `ObservableObject`, `RelayCommand` | Latest |
| **System.Text.Json** | Config serialization | Built-in |
| **WixSharp** | MSI installer build | Latest (dev-only) |
| **xUnit** + **Moq** | Unit testing | Latest |

No third-party keyboard library — native `WH_KEYBOARD_LL` P/Invoke is more reliable and lower-latency.

---

## Development Order (Bottom-Up)

1. **SPEC-01** Project shell + DI
2. **SPEC-02** Keyboard hook (verify events fire in any app)
3. **SPEC-03** Audio engine (verify sound plays on test key)
4. **SPEC-04** Config model + persistence
5. **SPEC-05** Binding resolution (wire SPEC-02 → SPEC-03 through SPEC-05)
6. **SPEC-07** Sound packs + bundled audio
7. **SPEC-06** System tray (app becomes background-capable)
8. **SPEC-08** Settings UI
9. **SPEC-09** Key binding UI
10. **SPEC-10** Sound preview
11. **SPEC-11** Combo multiplier
12. **SPEC-12** Overlay
13. **SPEC-13** Start with Windows
14. **SPEC-14** Performance mode
15. **SPEC-15** Installer

**Milestone after SPEC-07**: Working background app playing themed sounds on every keypress. Everything after is polish/customization.

---

## Creating the Windows Installer

**Option A — WixSharp (recommended, MSI):**
```csharp
// installer/BuildInstaller.csx
var project = new Project("ShootingKeyboard",
    new Dir(@"%ProgramFiles%\ShootingKeyboard",
        new File("src/ShootingKeyboard/bin/Release/net8.0-windows/publish/ShootingKeyboard.exe"),
        new Dir("Resources", new DirFiles(@"src/ShootingKeyboard/Resources/DefaultSounds/*.*")),
        new WixSharp.Shortcut("ShootingKeyboard", "INSTALLDIR")),
    new RegValue(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run",
        "ShootingKeyboard", "[INSTALLDIR]ShootingKeyboard.exe", new WixSharp.Condition("STARTWITHWINDOWS=\"1\"")));
project.GUID = new Guid("...");
project.Version = new Version("1.0.0");
Compiler.BuildMsi(project, "ShootingKeyboard-1.0.0.msi");
```
Pre-step: `dotnet publish -c Release -r win-x64 --self-contained true`

**Option B — InnoSetup (EXE):** Single `.iss` script producing setup.exe with same features.

---

## Critical Files to Create First

1. `src/ShootingKeyboard/Services/KeyboardHookService.cs` — Core native `WH_KEYBOARD_LL` interception
2. `src/ShootingKeyboard/Services/AudioEngineService.cs` — NAudio WasapiOut low-latency playback
3. `src/ShootingKeyboard/Models/AppConfig.cs` — Serialized configuration root
4. `src/ShootingKeyboard/ViewModels/MainViewModel.cs` — Composition root connecting all services
5. `installer/BuildInstaller.csx` — WixSharp installer definition

---

## Verification / Testing Strategy

- **Unit tests**: xUnit + Moq for Services (SPEC-02 through SPEC-05, SPEC-11, SPEC-13)
- **Integration test**: Manual verification after SPEC-07 — run app, type in Notepad, hear sounds
- **Latency check**: High-speed camera or audio recording to measure keypress-to-sound delay (<20ms target)
- **Stress test**: Rapid typing for 60s, verify no dropped events, no memory leaks, CPU <2%
- **Installer test**: Clean VM install/uninstall cycle, verify registry cleanup