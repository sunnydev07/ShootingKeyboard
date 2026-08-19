# 🔫 Shooting Keyboard — User & Installation Guide

Welcome to **Shooting Keyboard**! This guide walks you through downloading, installing, configuring, and getting the most out of your shooting sound effects while typing.

---

## Table of Contents

1. [Overview & System Requirements](#1-overview--system-requirements)
2. [Installation & Getting Started](#2-installation--getting-started)
   - [Option A: Portable Executable (Fastest)](#option-a-portable-executable-fastest)
   - [Option B: Building from Source](#option-b-building-from-source)
   - [Option C: Building the MSI Installer](#option-c-building-the-msi-installer)
3. [Quick Start Walkthrough](#3-quick-start-walkthrough)
4. [System Tray Controls](#4-system-tray-controls)
5. [Configuring Settings](#5-configuring-settings)
   - [Profiles Management & Import/Export](#profiles-management--importexport)
   - [Sound Packs, Audio Routing & Volume](#sound-packs-audio-routing--volume)
   - [Key Repeat & Cooldown Filtering](#key-repeat--cooldown-filtering)
   - [Visual Overlay Customization](#visual-overlay-customization)
   - [Quiet Hours (Auto-Mute)](#quiet-hours-auto-mute)
   - [Performance Mode & Windows Startup](#performance-mode--windows-startup)
6. [Per-Application & Game Rules](#6-per-application--game-rules)
7. [Key Binding & Volume Customization](#7-key-binding--volume-customization)
8. [Managing Sound Packs (.ZIP Import & Export)](#8-managing-sound-packs-zip-import--export)
9. [Runtime Diagnostics Dashboard](#9-runtime-diagnostics-dashboard)
10. [Frequently Asked Questions & Troubleshooting](#10-frequently-asked-questions--troubleshooting)

---

## 1. Overview & System Requirements

**Shooting Keyboard** plays customizable sound effects (gunshots, sci-fi lasers, retro 8-bit arcade blips, and explosions) for every keystroke system-wide with imperceptible audio latency (<15 ms).

### System Requirements:
- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **Audio Output**: Standard Windows audio device (Speakers / Headphones / Virtual Audio Cable)
- **RAM**: ~30–50 MB idle memory
- **Permissions**: Standard user permissions (Administrator recommended if you type inside elevated / admin windows)

---

## 2. Installation & Getting Started

### Option A: Portable Executable (Fastest)
1. Download or publish the standalone single-file `ShootingKeyboard.exe`.
   - Built output location: `dist/ShootingKeyboard-v1.0.0-win-x64/ShootingKeyboard.exe`
2. Move `ShootingKeyboard.exe` to a folder of your choice (e.g., `C:\Program Files\ShootingKeyboard` or `%LocalAppData%\Programs\ShootingKeyboard`).
3. Double-click `ShootingKeyboard.exe` to launch.
4. The application starts in the background and places an icon in your **Windows System Tray** (near the clock).

---

### Option B: Building from Source
If you have the source repository and [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed:

```powershell
# 1. Build the solution
dotnet build -c Release

# 2. Publish a self-contained single-file executable
dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true -o dist/ShootingKeyboard-v1.0.0-win-x64

# 3. Run the application
dotnet run --project src/ShootingKeyboard/ShootingKeyboard.csproj
```

---

### Option C: Building the MSI Installer
To build a standard Windows `.msi` setup package:

```powershell
# 1. Publish the release binary
dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true

# 2. Build the installer using WixSharp script
dotnet-script installer/BuildInstaller.csx
```
This produces `ShootingKeyboard-1.0.0.msi` for clean single-click installation and Start Menu shortcuts.

---

## 3. Quick Start Walkthrough

1. **Launch the App**: Run `ShootingKeyboard.exe`.
2. **Start Typing**: Open any text editor (Notepad, Word, Discord, Browser, or Game) and start typing.
   - Letters trigger primary gunshot/laser sounds.
   - **Spacebar** triggers heavy weapon fire.
   - **Enter** triggers explosions / special blasts.
   - **WASD** triggers tactical movement bursts.
3. **Open Settings**: Click the **Shooting Keyboard** icon in your system tray to open the Settings dashboard.

---

## 4. System Tray Controls

The app lives quietly in your system tray with rich quick-action submenus:

| Tray Item | Description |
|---|---|
| **Settings & Dashboard** | Opens the main configuration dashboard |
| **Diagnostics** | Opens the live diagnostic window (hook latency, audio events, rules) |
| **Profiles ▶** | Quickly switch between your saved profiles |
| **Sound Packs ▶** | Switch the active sound pack on the fly |
| **Volume ▶** | Quick presets: 25%, 50%, 75%, 100% |
| **Toggle Overlay** | Instantly show or hide the visual overlay |
| **Mute/Unmute Sounds** | Instantly silence or restore audio |
| **Pause/Resume Interception** | Temporarily detach the global keyboard hook |
| **Exit Application** | Cleanly shuts down Shooting Keyboard |

---

## 5. Configuring Settings

Open **Settings** from the tray icon to adjust your preferences:

### Profiles Management & Import/Export
- Create distinct profiles for **Gaming**, **Streaming**, **Coding**, or **Office**.
- Switch profiles instantly with live setting application.
- Export profiles to `.json` files to share with friends or backup.
- Import profiles with automatic safety validation.

### Sound Packs, Audio Routing & Volume
- **Active Sound Pack**: Select between 7 built-in packs or custom packs.
- **Audio Output Device**: Choose **System Default** or route audio directly to headphones, specific sound cards, or streaming audio channels (e.g. Voicemeeter).
- **Master Volume**: Smooth slider for global gain from 0% to 100%.

### Key Repeat & Cooldown Filtering
- **Ignore held-key repeat events**: Suppress machine-gun sound spam when holding down keys like Backspace or arrow keys.
- **Global Sound Cooldown (ms)**: Enforce a minimum interval between consecutive sounds (0–200 ms).

### Visual Overlay Customization
- **Key Ripple Effect**: Toggle visual ripple circle on keystroke.
- **Combo Counter**: Toggle combo counter multiplier badge.
- **Ripple Hex Color**: Customize the ripple color (e.g., `#FFA500`, `#00FFFF`, `#FF0055`).
- **Combo Position**: Choose placement on screen: `TopCenter`, `TopLeft`, `TopRight`, `BottomCenter`.
- **Overlay Scale**: Resize overlay from 0.5x to 2.0x.

### Quiet Hours (Auto-Mute)
- Automatically mute keyboard sounds during specific hours of the day (e.g., 22:00 to 08:00) across midnight or during custom ranges.

### Performance Mode & Windows Startup
- **Performance Mode**: Strips visual effects and optimizes thread priority for gaming.
- **Start with Windows**: Automatically start minimized in the tray on boot.

---

## 6. Per-Application & Game Rules

Click **Per-App Rules...** in Settings to customize audio behavior per application:

- **Target Process Name** (e.g., `Discord.exe`, `VALORANT-Win64-Shipping.exe`, `devenv.exe`) or Window Title.
- **Action**:
  - **Mute Audio**: Keep interception running silently.
  - **Disable Interception**: Stop hook while app is in focus.
  - **Override Sound Pack**: Switch to a specific sound pack automatically when that app is focused.

---

## 7. Key Binding & Volume Customization

Click **Customize Key Bindings...** in Settings:

### Group Bindings & Volume
- Assign sounds and volume multipliers per key group: `WASD`, `Letters`, `Space`, `Enter`, `Numbers`, `Arrows`, `Modifiers`, `F-Keys`, `Numpad`.

### Custom Per-Key Capture
1. Switch to the **Custom Key Bindings** tab.
2. Click **Capture Key** and press any physical key.
3. Assign a specific sound and individual volume multiplier.

---

## 8. Managing Sound Packs (.ZIP Import & Export)

Click **Manage Sound Packs...** in Settings:

- **Browse & Preview**: Audition any sound and review combo tiers.
- **Install from ZIP**: Click **Install Pack (.zip)...** to install a packaged community pack instantly.
- **Export to ZIP**: Select any pack and click **Export Pack (.zip)...** to package it for distribution.
- **Validation Alerts**: Clear error indicators if audio files are missing or metadata is invalid.

---

## 9. Runtime Diagnostics Dashboard

Access **Diagnostics** from the tray context menu:
- Live hook latency (ms) and event timestamps.
- Audio engine device status and active sample cache size.
- Last key pressed, virtual key code, resolved sound ID, and playback outcome.
- Evaluated foreground application rule results.

---

## 10. Frequently Asked Questions & Troubleshooting

### Q: Why do I hear no sound when typing?
1. Check that **Enabled** is checked in Settings.
2. Check that **Mute Sound Effects** is unchecked.
3. Check that **Quiet Hours** is not active.
4. Verify your selected **Audio Output Device** in Settings.

### Q: Keystrokes are not making sound in elevated Administrator windows?
- Windows security (UIPI) isolates elevated windows from standard user hooks.
- **Solution**: Right-click `ShootingKeyboard.exe` and select **Run as administrator**.

---

*Enjoy typing with Shooting Keyboard! 🚀*
