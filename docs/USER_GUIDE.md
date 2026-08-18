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
   - [Sound Packs & Volume](#sound-packs--volume)
   - [Combo Multiplier System](#combo-multiplier-system)
   - [On-Screen Visual Overlay](#on-screen-visual-overlay)
   - [Performance Mode](#performance-mode)
   - [Start with Windows](#start-with-windows)
6. [Key Binding Customization](#6-key-binding-customization)
   - [Group Presets](#group-presets)
   - [Custom Key Capture Mode](#custom-key-capture-mode)
7. [Adding Custom Sound Packs](#7-adding-custom-sound-packs)
8. [Frequently Asked Questions & Troubleshooting](#8-frequently-asked-questions--troubleshooting)

---

## 1. Overview & System Requirements

**Shooting Keyboard** plays customizable sound effects (gunshots, sci-fi lasers, retro 8-bit arcade blips, and explosions) for every keystroke system-wide with imperceptible audio latency (<15 ms).

### System Requirements:
- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **Audio Output**: Standard Windows audio device (Speakers / Headphones)
- **RAM**: ~30–50 MB idle memory
- **Permissions**: Standard user permissions (Administrator recommended if you type inside elevated / admin windows)

---

## 2. Installation & Getting Started

### Option A: Portable Executable (Fastest)
1. Download or publish the standalone single-file `ShootingKeyboard.exe`.
   - Built output location: `src/ShootingKeyboard/bin/Release/net8.0-windows/win-x64/publish/ShootingKeyboard.exe`
2. Move `ShootingKeyboard.exe` to a folder of your choice (e.g., `C:\Program Files\ShootingKeyboard` or `%LocalAppData%\Programs\ShootingKeyboard`).
3. Double-click `ShootingKeyboard.exe` to launch.
4. The application starts in the background and places an icon in your **Windows System Tray** (near the clock).

---

### Option B: Building from Source
If you have the source repository and [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed:

```powershell
# 1. Open PowerShell in the project root directory
cd "C:\Users\sunny\Desktop\Shooting Keyboard"

# 2. Build the solution
dotnet build -c Release

# 3. Publish a self-contained single-file executable
dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true

# 4. Run the application
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

The app lives quietly in your system tray:

![System Tray](https://via.placeholder.com/16/FF5722/FFFFFF?text=SK)

| Action | Result |
|---|---|
| **Left-Click Tray Icon** | Opens the **Settings** window |
| **Right-Click → Settings** | Opens the **Settings** window |
| **Right-Click → Mute/Unmute** | Instantly toggles sound effects on/off |
| **Right-Click → Pause/Resume** | Pauses or resumes global keyboard interception |
| **Right-Click → Exit** | Cleanly shuts down the application |

---

## 5. Configuring Settings

Open **Settings** from the tray icon to adjust your preferences:

### Sound Packs & Volume
- **Active Sound Pack**: Choose between bundled packs:
  - 💥 **Warzone**: Realistic tactical assault rifle shots, heavy shotgun blasts, and grenade detonations.
  - ⚡ **Sci-Fi**: Futuristic blaster pulses, plasma discharges, and warp explosions.
  - 🕹️ **Retro Arcade**: Authentic 8-bit chiptune key blips, jump chimes, coin collects, and 8-bit explosions.
- **Master Volume**: Adjust audio level from 0% to 100%.
- **Test Button**: Audition the selected pack's primary sound immediately.

### Combo Multiplier System
- As you type rapidly, Shooting Keyboard counts your active typing streak!
- **Combo Window (ms)**: Controls how long a pause can last before the combo resets (default: 400 ms).
- **Escalating Tiers**:
  - **Tier 1 (5+ keys)**: Enhanced fire + pitch boost
  - **Tier 2 (10+ keys)**: Extreme fire + pitch boost
  - **Tier 3 (20+ keys)**: Overload fire / minigun barrage
  - **Tier 4 (40+ keys)**: Maximum artillery strike / boss defeat

### On-Screen Visual Overlay
- **Show On-Screen Ripple & Combo Overlay**: Displays a sleek ripple animation at your cursor position on each keypress and an arcade-style combo badge showing your current multiplier and tier.
- Transparent and 100% click-through (`WS_EX_TRANSPARENT`).

### Performance Mode
- Check **Performance Mode** during competitive gaming or low-resource sessions:
  - Disables visual ripple animations and overlays.
  - Minimizes CPU usage to near 0%.
  - Optimizes audio buffers for sub-10ms latency.

### Start with Windows
- Check **Start Shooting Keyboard with Windows** to automatically launch the app in the background when your PC boots.

---

## 6. Key Binding Customization

Click **Customize Key Bindings...** in the Settings window:

### Group Presets
Easily assign an entire category of keys to any sound in your sound pack:
- **Letters**: Keys A–Z
- **WASD**: Movement keys
- **Space**: Spacebar
- **Enter**: Enter / Return
- **Numbers**: Keys 0–9
- **Arrows**: Navigation arrows
- **Modifiers**: Shift, Ctrl, Alt, Windows
- **F-Keys**: F1 through F24
- **Numpad & Navigation**: Keypad and Ins/Del/Home/End/PgUp/PgDn

### Custom Key Capture Mode
Want a specific sound for just one key (e.g. `BackSpace` or `Escape`)?
1. Switch to the **Custom Per-Key Bindings** tab.
2. Click **Capture Key**.
3. Press the desired key on your keyboard.
4. Select the sound effect from the dropdown.
5. Click **Apply Bindings**.

---

## 7. Adding Custom Sound Packs

You can add your own custom sound packs in minutes:

1. Open the Sound Packs folder:
   - In the **Sound Pack Manager** window, click **Open Packs Folder**, or navigate to:
     ```
     %AppData%\ShootingKeyboard\packs
     ```
2. Create a new subfolder (e.g. `LaserSwords`).
3. Add your sound files (`.wav` or `.mp3`).
4. Create a `pack.json` file inside the folder:

```json
{
  "id": "laserswords",
  "name": "Laser Swords",
  "author": "MyName",
  "description": "Lightsaber swings and plasma hums.",
  "defaults": {
    "volume": 0.85,
    "comboWindowMs": 400
  },
  "sounds": [
    {
      "id": "saber_swing",
      "displayName": "Saber Swing",
      "file": "swing.wav",
      "volume": 1.0
    },
    {
      "id": "saber_clash",
      "displayName": "Saber Clash",
      "file": "clash.wav",
      "group": "Enter",
      "volume": 1.0
    }
  ]
}
```
5. Reopen the **Sound Pack Manager** in Shooting Keyboard — your new pack will appear automatically!

---

## 8. Frequently Asked Questions & Troubleshooting

### Q: Why do I hear no sound when typing?
1. Check that **Enabled** is checked in Settings.
2. Check that **Mute Sound Effects** is unchecked.
3. Check your Windows default playback device and volume mixer.
4. Verify that Shooting Keyboard is running in the system tray.

### Q: Keystrokes are not making sound when I type in Command Prompt or Task Manager (Administrator)?
- Windows security (UIPI) prevents standard user applications from intercepting input in elevated Administrator windows.
- **Solution**: Right-click `ShootingKeyboard.exe` and select **Run as administrator**.

### Q: How do I reset all settings to default?
- Open Settings and click **Reset Defaults** in the bottom-left corner.

### Q: How do I uninstall Shooting Keyboard?
- **Portable version**: Right-click the tray icon → **Exit**, then delete the application folder and `%AppData%\ShootingKeyboard`.
- **MSI version**: Open Windows **Settings → Installed Apps** → Select **Shooting Keyboard** → Click **Uninstall**.

---

*Enjoy typing with Shooting Keyboard! 🚀*
