# Shooting Keyboard — Sound Pack Format Specification

This document details the file structure and schema for creating custom sound packs in Shooting Keyboard.

---

## Directory Structure

Sound packs can be located in either:
1. **User Directory**: `%AppData%/ShootingKeyboard/packs/<PackName>/`
2. **App Bundled Directory**: `<AppDirectory>/Resources/DefaultSounds/<PackName>/`

Each sound pack folder must contain a `pack.json` descriptor file along with the associated audio files (`.wav`, `.mp3`, or `.ogg`).

```
MyCustomPack/
├── pack.json
├── primary_shot.wav
├── space_shot.wav
├── enter_explosion.wav
├── wasd_burst.wav
├── combo_tier1.wav
├── combo_tier2.wav
└── combo_tier3.wav
```

---

## `pack.json` Schema

```json
{
  "id": "unique-pack-id",
  "name": "Human Readable Pack Name",
  "author": "Creator Name",
  "description": "Short description of the theme and audio effects.",
  "defaults": {
    "volume": 0.8,
    "comboWindowMs": 400
  },
  "sounds": [
    {
      "id": "sound_id_1",
      "displayName": "Primary Shot",
      "file": "primary_shot.wav",
      "variants": ["primary_shot_var1.wav", "primary_shot_var2.wav"],
      "group": null,
      "volume": 1.0,
      "isComboVariant": false
    },
    {
      "id": "sound_id_space",
      "displayName": "Heavy Shotgun Blast",
      "file": "space_shot.wav",
      "group": "Space",
      "volume": 1.0,
      "isComboVariant": false
    },
    {
      "id": "sound_id_enter",
      "displayName": "Rocket Explosion",
      "file": "enter_explosion.wav",
      "group": "Enter",
      "volume": 1.0,
      "isComboVariant": false
    },
    {
      "id": "sound_id_combo1",
      "displayName": "Enhanced Fire (Tier 1)",
      "file": "combo_tier1.wav",
      "isComboVariant": true,
      "comboTier": 1
    }
  ]
}
```

---

## Supported Key Groups

When assigning sound entries to logical groups in `group`, use any of the standard logical group names:

| Group Name | Affected Keys |
|---|---|
| `Letters` | Alphabetical keys A-Z (excluding WASD) |
| `WASD` | W, A, S, D |
| `Numbers` | Numeric keys 0-9 |
| `Arrows` | Up, Down, Left, Right arrows |
| `FKeys` | Function keys F1 through F24 |
| `Space` | Spacebar key |
| `Enter` | Enter / Return key |
| `Modifiers` | Shift, Ctrl, Alt, Windows keys |
| `Navigation` | Insert, Delete, Home, End, PageUp, PageDown |
| `Numpad` | Numeric keypad keys |
| `Punctuation` | Punctuation and symbols |

---

## Audio Recommendations

- **Format**: 44.1 kHz, 16-bit PCM WAV (or standard MP3)
- **Channels**: Mono or Stereo (stereo recommended for immersive effects)
- **Duration**: 80ms – 400ms for regular keystrokes; up to 800ms for Enter/Space explosions
- **Attack/Transient**: Sharp attack (< 5ms) for imperceptible latency
