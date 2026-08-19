# Shooting Keyboard 🔫💥

**Shooting Keyboard** is a high-performance Windows desktop application built with C# (.NET 8 WPF) and NAudio that plays realistic tactical audio effects (gunshots, assault rifles, pistols, mechanical keyboard clicks, sci-fi lasers, retro 8-bit chiptunes, and explosions) for every keystroke system-wide with imperceptible audio latency (<15ms).

<p align="center">
  <img src="src/Screenshot/image.png" alt="Shooting Keyboard Preview" width="650" />
</p>

<p align="center">
  <a href="https://github.com/sunnydev07/ShootingKeyboard/releases/latest"><img src="https://img.shields.io/github/v/release/sunnydev07/ShootingKeyboard?color=brightgreen&label=Download%20Latest%20v1.1.0" alt="Download Latest Release" /></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0%20WPF-blueviolet.svg" alt=".NET 8.0 WPF" /></a>
  <a href="https://github.com/sunnydev07/ShootingKeyboard/blob/main/LICENSE.txt"><img src="https://img.shields.io/badge/Platform-Windows%20x64-0078d4.svg" alt="Windows x64" /></a>
  <a href="LICENSE.txt"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT" /></a>
</p>

---

## 🎵 7 Bundled Sound Packs

Shooting Keyboard comes with **7 built-in, studio-crafted sound packs**:

1. 🎯 **Assault Rifle**: Military 5.56mm M4A1 single shots, 7.62mm AK-47 battle rifle spacebar blasts, 40mm grenade launcher, 3-round bursts, and Rotary Minigun combos.
2. 🔫 **Tactical Pistol**: Crisp 9mm semi-auto handgun shots, .50 Desert Eagle hand cannon spacebar, tactical flashbang breach, and Akimbo pistol frenzy combos.
3. 💥 **Heavy Gunshot & Shotguns**: High-impact 12-gauge pump action shotguns, double-barrel elephant gun, C4 explosive breach, and Barrett .50 Cal sniper rifle combos.
4. ⌨️ **Mechanical Keyboard (Thock & Clicky)**: Satisfying tactile switch clicks (Cherry MX Blue style), lubricated deep spacebar "thocks", return key clacks, linear gaming taps, and typing flurry combos.
5. ⚔️ **Warzone**: Tactical military combat gunfire, shotgun blasts, and grenade detonations.
6. ⚡ **Sci-Fi**: Futuristic laser blasters, plasma cannons, phasers, and warp singularity explosions.
7. 🕹️ **Retro Arcade**: Authentic 8-bit chiptune key blips, jump springs, coin collect chimes, and 8-bit explosions.

---

## 🚀 Key Features

- **System-Wide Key Interception**: Uses native Win32 low-level keyboard hooks (`WH_KEYBOARD_LL`) running on a dedicated message-pump thread. Intercepts every keystroke in games, IDEs, browsers, and terminal windows without UI lag.
- **Ultra-Low Latency Audio**: NAudio Wasapi / DirectSound / WaveOut event-driven mixer with pre-cached float PCM memory buffers.
- **Audio Output Device Routing**: Route sound effects directly to specific headphones, speakers, or virtual audio devices without changing Windows default output.
- **Profiles & Quick Switching**: Create, switch, import, and export separate configuration profiles for gaming, coding, streaming, or office work.
- **Per-App & Per-Game Rules**: Automatically mute sounds, disable interception, or activate dedicated sound packs based on the active foreground application.
- **Sound Clip Random Variants**: Support for multiple randomized audio samples per key to eliminate repetition fatigue.
- **Sound Pack Zip Import & Export**: Install community sound packs directly from `.zip` files and export your custom packs with one click.
- **Sound Pack Schema Validator**: Automatic linting and validation of sound packs with actionable error diagnostics.
- **Key Repeat & Spam Filtering**: Optional suppression of held-down key repeats and configurable global key spam cooldowns.
- **Group & Custom Volume Overrides**: Fine-tune volume levels individually per key group (WASD, Space, Enter, etc.) or per virtual key.
- **Combo Multiplier System**: Escalates sound tiers and pitch dynamically during rapid typing streaks.
- **On-Screen Visual Overlay**: Transparent, click-through (`WS_EX_TRANSPARENT | WS_EX_LAYERED`) top-most window displaying customizable key ripples and combo meters (scale, color, position presets).
- **Quiet Hours**: Schedule auto-mute periods for late nights or focused work hours.
- **Tray Quick Controls**: Switch profiles, sound packs, volume levels, and overlays instantly right from the system tray menu.
- **Live Diagnostics Window**: Real-time inspection of hook events, active audio streams, latency stats, and rule evaluations.
- **Performance Mode**: Strips visual effects and optimizes thread priority for competitive gaming sessions.

---

## Project Structure

```
ShootingKeyboard/
├── ShootingKeyboard.sln
├── src/
│   ├── ShootingKeyboard/                 # Main WPF Application
│   │   ├── Models/                      # AppConfig, AppProfile, AppRule, SoundPack, OverlayConfig, QuietHours
│   │   ├── Services/                    # KeyboardHook, AudioEngine, SoundPackManager, ProfileManager,
│   │   │                                # AppRuleEvaluator, SoundPackValidator, ImportExport, QuietHours
│   │   ├── ViewModels/                  # Main, Settings, KeyBinding, SoundPack, Diagnostics, AppRules
│   │   ├── Views/                       # SettingsWindow, KeyBindingWindow, SoundPackWindow, AppRulesWindow
│   │   ├── Overlay/                     # OverlayWindow (transparent click-through visualizer)
│   │   └── Resources/                   # Embedded default sound packs and application icons
│   └── ShootingKeyboard.Tests/           # xUnit Unit & Integration Test Suite (211 tests)
├── sound-packs/                         # Standalone bundled sound packs
│   ├── AssaultRifle/
│   ├── HeavyGunshot/
│   ├── MechanicalKeyboard/
│   ├── Pistol/
│   ├── RetroArcade/
│   ├── SciFi/
│   └── Warzone/
├── installer/                           # WixSharp MSI installer script
├── docs/                                # Sound Pack Format specifications & User Guide
└── README.md
```

---

## Building and Running Locally

### Prerequisites
- Windows 10 / 11 (64-bit)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build and Test
```powershell
# Restore and build the solution
dotnet build -c Release

# Run the full unit test suite (211 tests)
dotnet test
```

### Run the App
```powershell
dotnet run --project src/ShootingKeyboard/ShootingKeyboard.csproj
```

---

## Creating the Single-File Portable Executable & Installer

### 1. Publish Single-File Self-Contained Binary:
```powershell
dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true -o dist/ShootingKeyboard-v1.1.0-win-x64
```

### 2. Build Windows MSI Installer:
```powershell
dotnet-script installer/BuildInstaller.csx
```

---

## License

Shooting Keyboard is open-source software licensed under the [MIT License](LICENSE.txt).
