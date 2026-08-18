using System;
using System.IO;
using System.Text.Json;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Provides guaranteed built-in sound pack synthesis on any machine
/// </summary>
public static class BuiltInSoundPackFactory
{
    private const int SampleRate = 44100;

    public static void EnsureDefaultPacks(string packsRootDir)
    {
        try
        {
            Directory.CreateDirectory(packsRootDir);

            // 1. Warzone
            var warzoneDir = Path.Combine(packsRootDir, "Warzone");
            if (!File.Exists(Path.Combine(warzoneDir, "pack.json")))
                GenerateWarzonePack(warzoneDir);

            // 2. Tactical Pistol
            var pistolDir = Path.Combine(packsRootDir, "Pistol");
            if (!File.Exists(Path.Combine(pistolDir, "pack.json")))
                GeneratePistolPack(pistolDir);

            // 3. Assault Rifle
            var rifleDir = Path.Combine(packsRootDir, "AssaultRifle");
            if (!File.Exists(Path.Combine(rifleDir, "pack.json")))
                GenerateRiflePack(rifleDir);

            // 4. Heavy Shotguns & Sniper
            var heavyDir = Path.Combine(packsRootDir, "HeavyGunshot");
            if (!File.Exists(Path.Combine(heavyDir, "pack.json")))
                GenerateHeavyGunshotPack(heavyDir);

            // 5. Mechanical Keyboard (Thock & Clicky)
            var mechDir = Path.Combine(packsRootDir, "MechanicalKeyboard");
            if (!File.Exists(Path.Combine(mechDir, "pack.json")))
                GenerateMechanicalKeyboardPack(mechDir);

            // 6. Sci-Fi
            var scifiDir = Path.Combine(packsRootDir, "SciFi");
            if (!File.Exists(Path.Combine(scifiDir, "pack.json")))
                GenerateSciFiPack(scifiDir);

            // 7. Retro Arcade
            var retroDir = Path.Combine(packsRootDir, "RetroArcade");
            if (!File.Exists(Path.Combine(retroDir, "pack.json")))
                GenerateRetroArcadePack(retroDir);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to auto-generate default sound packs: {ex.Message}");
        }
    }

    #region Pack Generators

    public static void GeneratePistolPack(string outDir)
    {
        Directory.CreateDirectory(outDir);

        WriteWavFile(Path.Combine(outDir, "pistol_default.wav"), SynthesizePistol(750, 140, 0.16f, 0.55f));
        WriteWavFile(Path.Combine(outDir, "pistol_space.wav"), SynthesizePistol(480, 80, 0.28f, 0.70f, true));
        WriteWavFile(Path.Combine(outDir, "pistol_enter.wav"), SynthesizeExplosion(0.55f));
        WriteWavFile(Path.Combine(outDir, "pistol_wasd.wav"), SynthesizePistol(820, 160, 0.12f, 0.45f));
        WriteWavFile(Path.Combine(outDir, "pistol_combo1.wav"), SynthesizePistol(850, 130, 0.18f, 0.50f));
        WriteWavFile(Path.Combine(outDir, "pistol_combo2.wav"), SynthesizePistol(920, 110, 0.22f, 0.60f));
        WriteWavFile(Path.Combine(outDir, "pistol_combo3.wav"), SynthesizePistol(520, 85, 0.26f, 0.75f, true));
        WriteWavFile(Path.Combine(outDir, "pistol_combo4.wav"), SynthesizePistol(1000, 70, 0.32f, 0.80f, true));

        var pack = new SoundPack
        {
            Id = "pistol",
            Name = "Tactical Pistol",
            Author = "ShootingKeyboard",
            Description = "Crisp 9mm semi-auto handgun shots, .50 Desert Eagle blasts, and tactical sidearm fire.",
            Defaults = new PackDefaults { Volume = 0.85f, ComboWindowMs = 380 },
            Sounds = new()
            {
                new SoundEntry { Id = "pistol_default", DisplayName = "9mm Pistol Shot", File = "pistol_default.wav", Volume = 1.0f },
                new SoundEntry { Id = "pistol_space", DisplayName = ".50 Desert Eagle Hand Cannon", File = "pistol_space.wav", Group = KeyGroups.Space, Volume = 1.0f },
                new SoundEntry { Id = "pistol_enter", DisplayName = "Tactical Flashbang Breach", File = "pistol_enter.wav", Group = KeyGroups.Enter, Volume = 1.0f },
                new SoundEntry { Id = "pistol_wasd", DisplayName = "Suppressed Tactical Tap", File = "pistol_wasd.wav", Group = KeyGroups.WASD, Volume = 0.9f },
                new SoundEntry { Id = "pistol_combo1", DisplayName = "Double Tap (Tier 1)", File = "pistol_combo1.wav", IsComboVariant = true, ComboTier = 1 },
                new SoundEntry { Id = "pistol_combo2", DisplayName = "Revolver Fan-Fire (Tier 2)", File = "pistol_combo2.wav", IsComboVariant = true, ComboTier = 2 },
                new SoundEntry { Id = "pistol_combo3", DisplayName = "Magnum Rapid Fire (Tier 3)", File = "pistol_combo3.wav", IsComboVariant = true, ComboTier = 3 },
                new SoundEntry { Id = "pistol_combo4", DisplayName = "Akimbo Pistol Frenzy (Tier 4)", File = "pistol_combo4.wav", IsComboVariant = true, ComboTier = 4 }
            }
        };

        File.WriteAllText(Path.Combine(outDir, "pack.json"), JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void GenerateRiflePack(string outDir)
    {
        Directory.CreateDirectory(outDir);

        WriteWavFile(Path.Combine(outDir, "rifle_default.wav"), SynthesizeRifle(620, 110, 0.19f, 0.60f));
        WriteWavFile(Path.Combine(outDir, "rifle_space.wav"), SynthesizeRifle(420, 70, 0.32f, 0.75f, true));
        WriteWavFile(Path.Combine(outDir, "rifle_enter.wav"), SynthesizeExplosion(0.70f));
        WriteWavFile(Path.Combine(outDir, "rifle_wasd.wav"), SynthesizeRifle(700, 130, 0.15f, 0.55f));
        WriteWavFile(Path.Combine(outDir, "rifle_combo1.wav"), SynthesizeRifle(680, 100, 0.22f, 0.65f));
        WriteWavFile(Path.Combine(outDir, "rifle_combo2.wav"), SynthesizeRifle(750, 90, 0.25f, 0.70f));
        WriteWavFile(Path.Combine(outDir, "rifle_combo3.wav"), SynthesizeRifle(820, 75, 0.28f, 0.75f, true));
        WriteWavFile(Path.Combine(outDir, "rifle_combo4.wav"), SynthesizeExplosion(0.90f));

        var pack = new SoundPack
        {
            Id = "assault-rifle",
            Name = "Assault Rifle",
            Author = "ShootingKeyboard",
            Description = "5.56mm and 7.62mm military assault rifles, rapid bursts, and heavy battle rifle gunfire.",
            Defaults = new PackDefaults { Volume = 0.85f, ComboWindowMs = 380 },
            Sounds = new()
            {
                new SoundEntry { Id = "rifle_default", DisplayName = "5.56mm M4A1 Round", File = "rifle_default.wav", Volume = 1.0f },
                new SoundEntry { Id = "rifle_space", DisplayName = "7.62mm AK-47 Heavy Shot", File = "rifle_space.wav", Group = KeyGroups.Space, Volume = 1.0f },
                new SoundEntry { Id = "rifle_enter", DisplayName = "40mm Grenade Launcher", File = "rifle_enter.wav", Group = KeyGroups.Enter, Volume = 1.0f },
                new SoundEntry { Id = "rifle_wasd", DisplayName = "3-Round Burst Fire", File = "rifle_wasd.wav", Group = KeyGroups.WASD, Volume = 0.9f },
                new SoundEntry { Id = "rifle_combo1", DisplayName = "Tactical Burst (Tier 1)", File = "rifle_combo1.wav", IsComboVariant = true, ComboTier = 1 },
                new SoundEntry { Id = "rifle_combo2", DisplayName = "Sustained Fire (Tier 2)", File = "rifle_combo2.wav", IsComboVariant = true, ComboTier = 2 },
                new SoundEntry { Id = "rifle_combo3", DisplayName = "Heavy Battle Rifle (Tier 3)", File = "rifle_combo3.wav", IsComboVariant = true, ComboTier = 3 },
                new SoundEntry { Id = "rifle_combo4", DisplayName = "Minigun Barrage (Tier 4)", File = "rifle_combo4.wav", IsComboVariant = true, ComboTier = 4 }
            }
        };

        File.WriteAllText(Path.Combine(outDir, "pack.json"), JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void GenerateHeavyGunshotPack(string outDir)
    {
        Directory.CreateDirectory(outDir);

        WriteWavFile(Path.Combine(outDir, "heavy_default.wav"), SynthesizeHeavyShotgun(0.32f));
        WriteWavFile(Path.Combine(outDir, "heavy_space.wav"), SynthesizeHeavyShotgun(0.45f, true));
        WriteWavFile(Path.Combine(outDir, "heavy_enter.wav"), SynthesizeExplosion(0.85f));
        WriteWavFile(Path.Combine(outDir, "heavy_wasd.wav"), SynthesizeGunshot(420, 65, 0.22f, 0.70f));
        WriteWavFile(Path.Combine(outDir, "heavy_combo1.wav"), SynthesizeHeavyShotgun(0.34f));
        WriteWavFile(Path.Combine(outDir, "heavy_combo2.wav"), SynthesizeGunshot(350, 40, 0.38f, 0.85f));
        WriteWavFile(Path.Combine(outDir, "heavy_combo3.wav"), SynthesizeHeavyShotgun(0.42f, true));
        WriteWavFile(Path.Combine(outDir, "heavy_combo4.wav"), SynthesizeExplosion(1.05f));

        var pack = new SoundPack
        {
            Id = "heavy-gunshot",
            Name = "Heavy Gunshot & Shotguns",
            Author = "ShootingKeyboard",
            Description = "High-impact 12-gauge pump action shotguns, .50 Cal sniper rifles, and explosive ballistic detonations.",
            Defaults = new PackDefaults { Volume = 0.85f, ComboWindowMs = 420 },
            Sounds = new()
            {
                new SoundEntry { Id = "heavy_default", DisplayName = "12-Gauge Pump Shotgun", File = "heavy_default.wav", Volume = 1.0f },
                new SoundEntry { Id = "heavy_space", DisplayName = "Double-Barrel Elephant Gun", File = "heavy_space.wav", Group = KeyGroups.Space, Volume = 1.0f },
                new SoundEntry { Id = "heavy_enter", DisplayName = "C4 Explosive Breach", File = "heavy_enter.wav", Group = KeyGroups.Enter, Volume = 1.0f },
                new SoundEntry { Id = "heavy_wasd", DisplayName = "Combat Shotgun Slug", File = "heavy_wasd.wav", Group = KeyGroups.WASD, Volume = 0.9f },
                new SoundEntry { Id = "heavy_combo1", DisplayName = "Dual Shotgun Blast (Tier 1)", File = "heavy_combo1.wav", IsComboVariant = true, ComboTier = 1 },
                new SoundEntry { Id = "heavy_combo2", DisplayName = "Barrett .50 Sniper (Tier 2)", File = "heavy_combo2.wav", IsComboVariant = true, ComboTier = 2 },
                new SoundEntry { Id = "heavy_combo3", DisplayName = "AA-12 Drum Barrage (Tier 3)", File = "heavy_combo3.wav", IsComboVariant = true, ComboTier = 3 },
                new SoundEntry { Id = "heavy_combo4", DisplayName = "105mm Artillery Strike (Tier 4)", File = "heavy_combo4.wav", IsComboVariant = true, ComboTier = 4 }
            }
        };

        File.WriteAllText(Path.Combine(outDir, "pack.json"), JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void GenerateMechanicalKeyboardPack(string outDir)
    {
        Directory.CreateDirectory(outDir);

        WriteWavFile(Path.Combine(outDir, "mech_default.wav"), SynthesizeMechSwitch(2800, 520, 0.08f));
        WriteWavFile(Path.Combine(outDir, "mech_space.wav"), SynthesizeMechSpacebar(160, 0.14f));
        WriteWavFile(Path.Combine(outDir, "mech_enter.wav"), SynthesizeMechSwitch(2200, 380, 0.11f, true));
        WriteWavFile(Path.Combine(outDir, "mech_wasd.wav"), SynthesizeMechSwitch(3100, 600, 0.06f));
        WriteWavFile(Path.Combine(outDir, "mech_combo1.wav"), SynthesizeMechSwitch(3000, 540, 0.09f));
        WriteWavFile(Path.Combine(outDir, "mech_combo2.wav"), SynthesizeMechSwitch(3300, 580, 0.10f));
        WriteWavFile(Path.Combine(outDir, "mech_combo3.wav"), SynthesizeMechSwitch(3600, 620, 0.12f, true));
        WriteWavFile(Path.Combine(outDir, "mech_combo4.wav"), SynthesizeMechSpacebar(130, 0.18f));

        var pack = new SoundPack
        {
            Id = "mechanical-keyboard",
            Name = "Mechanical Keyboard (Thock & Clicky)",
            Author = "ShootingKeyboard",
            Description = "Satisfying tactile switch clicks, lubricated deep spacebar thocks, and crisp bottom-out clacks.",
            Defaults = new PackDefaults { Volume = 0.90f, ComboWindowMs = 350 },
            Sounds = new()
            {
                new SoundEntry { Id = "mech_default", DisplayName = "Tactical Switch Click & Clack", File = "mech_default.wav", Volume = 1.0f },
                new SoundEntry { Id = "mech_space", DisplayName = "Stabilized Spacebar Thock", File = "mech_space.wav", Group = KeyGroups.Space, Volume = 1.0f },
                new SoundEntry { Id = "mech_enter", DisplayName = "Return Key Heavy Clack", File = "mech_enter.wav", Group = KeyGroups.Enter, Volume = 1.0f },
                new SoundEntry { Id = "mech_wasd", DisplayName = "Linear Gaming Switch Tap", File = "mech_wasd.wav", Group = KeyGroups.WASD, Volume = 0.9f },
                new SoundEntry { Id = "mech_combo1", DisplayName = "Fast Typing Rhythm (Tier 1)", File = "mech_combo1.wav", IsComboVariant = true, ComboTier = 1 },
                new SoundEntry { Id = "mech_combo2", DisplayName = "Rapid Clack Cascade (Tier 2)", File = "mech_combo2.wav", IsComboVariant = true, ComboTier = 2 },
                new SoundEntry { Id = "mech_combo3", DisplayName = "120 WPM Flurry (Tier 3)", File = "mech_combo3.wav", IsComboVariant = true, ComboTier = 3 },
                new SoundEntry { Id = "mech_combo4", DisplayName = "Mechanical Symphony (Tier 4)", File = "mech_combo4.wav", IsComboVariant = true, ComboTier = 4 }
            }
        };

        File.WriteAllText(Path.Combine(outDir, "pack.json"), JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void GenerateWarzonePack(string outDir)
    {
        Directory.CreateDirectory(outDir);

        WriteWavFile(Path.Combine(outDir, "shot_default.wav"), SynthesizeGunshot(450, 60, 0.22f, 0.65f));
        WriteWavFile(Path.Combine(outDir, "shot_space.wav"), SynthesizeGunshot(320, 45, 0.35f, 0.8f));
        WriteWavFile(Path.Combine(outDir, "shot_enter.wav"), SynthesizeExplosion(0.65f));
        WriteWavFile(Path.Combine(outDir, "shot_wasd.wav"), SynthesizeGunshot(520, 80, 0.16f, 0.55f));
        WriteWavFile(Path.Combine(outDir, "shot_combo1.wav"), SynthesizeGunshot(550, 70, 0.24f, 0.6f));
        WriteWavFile(Path.Combine(outDir, "shot_combo2.wav"), SynthesizeGunshot(620, 50, 0.28f, 0.7f));
        WriteWavFile(Path.Combine(outDir, "shot_combo3.wav"), SynthesizeGunshot(700, 45, 0.32f, 0.75f));
        WriteWavFile(Path.Combine(outDir, "shot_combo4.wav"), SynthesizeExplosion(0.85f));

        var pack = new SoundPack
        {
            Id = "warzone",
            Name = "Warzone",
            Author = "ShootingKeyboard",
            Description = "Tactical gunfire, artillery explosions, and military combat sound effects.",
            Defaults = new PackDefaults { Volume = 0.85f, ComboWindowMs = 400 },
            Sounds = new()
            {
                new SoundEntry { Id = "shot_default", DisplayName = "Assault Rifle Shot", File = "shot_default.wav", Volume = 1.0f },
                new SoundEntry { Id = "shot_space", DisplayName = "Heavy Shotgun Blast", File = "shot_space.wav", Group = KeyGroups.Space, Volume = 1.0f },
                new SoundEntry { Id = "shot_enter", DisplayName = "Grenade Explosion", File = "shot_enter.wav", Group = KeyGroups.Enter, Volume = 1.0f },
                new SoundEntry { Id = "shot_wasd", DisplayName = "Tactical Burst", File = "shot_wasd.wav", Group = KeyGroups.WASD, Volume = 0.9f },
                new SoundEntry { Id = "shot_combo1", DisplayName = "Enhanced Fire (Tier 1)", File = "shot_combo1.wav", IsComboVariant = true, ComboTier = 1 },
                new SoundEntry { Id = "shot_combo2", DisplayName = "Sniper Fire (Tier 2)", File = "shot_combo2.wav", IsComboVariant = true, ComboTier = 2 },
                new SoundEntry { Id = "shot_combo3", DisplayName = "Minigun Barrage (Tier 3)", File = "shot_combo3.wav", IsComboVariant = true, ComboTier = 3 },
                new SoundEntry { Id = "shot_combo4", DisplayName = "Artillery Strike (Tier 4)", File = "shot_combo4.wav", IsComboVariant = true, ComboTier = 4 }
            }
        };

        File.WriteAllText(Path.Combine(outDir, "pack.json"), JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void GenerateSciFiPack(string outDir)
    {
        Directory.CreateDirectory(outDir);

        WriteWavFile(Path.Combine(outDir, "laser_default.wav"), SynthesizeLaser(1800, 250, 0.18f));
        WriteWavFile(Path.Combine(outDir, "laser_space.wav"), SynthesizeLaser(1200, 120, 0.32f, true));
        WriteWavFile(Path.Combine(outDir, "laser_enter.wav"), SynthesizeSciFiExplosion(0.6f));
        WriteWavFile(Path.Combine(outDir, "laser_wasd.wav"), SynthesizeLaser(2200, 400, 0.14f));
        WriteWavFile(Path.Combine(outDir, "laser_combo1.wav"), SynthesizeLaser(2400, 300, 0.20f));
        WriteWavFile(Path.Combine(outDir, "laser_combo2.wav"), SynthesizeLaser(2800, 200, 0.25f, true));
        WriteWavFile(Path.Combine(outDir, "laser_combo3.wav"), SynthesizeLaser(3200, 180, 0.30f, true));
        WriteWavFile(Path.Combine(outDir, "laser_combo4.wav"), SynthesizeSciFiExplosion(0.85f));

        var pack = new SoundPack
        {
            Id = "scifi",
            Name = "Sci-Fi",
            Author = "ShootingKeyboard",
            Description = "Futuristic laser blasters, plasma cannons, and cosmic energy discharge.",
            Defaults = new PackDefaults { Volume = 0.8f, ComboWindowMs = 380 },
            Sounds = new()
            {
                new SoundEntry { Id = "laser_default", DisplayName = "Laser Blaster", File = "laser_default.wav", Volume = 1.0f },
                new SoundEntry { Id = "laser_space", DisplayName = "Plasma Cannon", File = "laser_space.wav", Group = KeyGroups.Space, Volume = 1.0f },
                new SoundEntry { Id = "laser_enter", DisplayName = "Warp Explosion", File = "laser_enter.wav", Group = KeyGroups.Enter, Volume = 1.0f },
                new SoundEntry { Id = "laser_wasd", DisplayName = "Phaser Pulse", File = "laser_wasd.wav", Group = KeyGroups.WASD, Volume = 0.9f },
                new SoundEntry { Id = "laser_combo1", DisplayName = "Twin Blaster (Tier 1)", File = "laser_combo1.wav", IsComboVariant = true, ComboTier = 1 },
                new SoundEntry { Id = "laser_combo2", DisplayName = "Ion Surge (Tier 2)", File = "laser_combo2.wav", IsComboVariant = true, ComboTier = 2 },
                new SoundEntry { Id = "laser_combo3", DisplayName = "Quantum Beam (Tier 3)", File = "laser_combo3.wav", IsComboVariant = true, ComboTier = 3 },
                new SoundEntry { Id = "laser_combo4", DisplayName = "Supernova Strike (Tier 4)", File = "laser_combo4.wav", IsComboVariant = true, ComboTier = 4 }
            }
        };

        File.WriteAllText(Path.Combine(outDir, "pack.json"), JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void GenerateRetroArcadePack(string outDir)
    {
        Directory.CreateDirectory(outDir);

        WriteWavFile(Path.Combine(outDir, "blip_default.wav"), Synthesize8BitChirp(440, 880, 0.10f));
        WriteWavFile(Path.Combine(outDir, "blip_space.wav"), Synthesize8BitJump(0.22f));
        WriteWavFile(Path.Combine(outDir, "blip_enter.wav"), Synthesize8BitExplosion(0.45f));
        WriteWavFile(Path.Combine(outDir, "blip_wasd.wav"), Synthesize8BitChirp(600, 300, 0.08f));
        WriteWavFile(Path.Combine(outDir, "blip_combo1.wav"), Synthesize8BitCoin(0.22f));
        WriteWavFile(Path.Combine(outDir, "blip_combo2.wav"), Synthesize8BitPowerUp(0.30f));
        WriteWavFile(Path.Combine(outDir, "blip_combo3.wav"), Synthesize8BitFanfare(0.40f));
        WriteWavFile(Path.Combine(outDir, "blip_combo4.wav"), Synthesize8BitExplosion(0.70f));

        var pack = new SoundPack
        {
            Id = "retro-arcade",
            Name = "Retro Arcade",
            Author = "ShootingKeyboard",
            Description = "Nostalgic 8-bit chiptune blips, jumps, coins, and arcade sound effects.",
            Defaults = new PackDefaults { Volume = 0.75f, ComboWindowMs = 450 },
            Sounds = new()
            {
                new SoundEntry { Id = "blip_default", DisplayName = "8-Bit Key Blip", File = "blip_default.wav", Volume = 1.0f },
                new SoundEntry { Id = "blip_space", DisplayName = "8-Bit Jump", File = "blip_space.wav", Group = KeyGroups.Space, Volume = 1.0f },
                new SoundEntry { Id = "blip_enter", DisplayName = "8-Bit Crash Explosion", File = "blip_enter.wav", Group = KeyGroups.Enter, Volume = 1.0f },
                new SoundEntry { Id = "blip_wasd", DisplayName = "8-Bit Step Chirp", File = "blip_wasd.wav", Group = KeyGroups.WASD, Volume = 0.9f },
                new SoundEntry { Id = "blip_combo1", DisplayName = "Coin Collect (Tier 1)", File = "blip_combo1.wav", IsComboVariant = true, ComboTier = 1 },
                new SoundEntry { Id = "blip_combo2", DisplayName = "Power-Up (Tier 2)", File = "blip_combo2.wav", IsComboVariant = true, ComboTier = 2 },
                new SoundEntry { Id = "blip_combo3", DisplayName = "1-UP Fanfare (Tier 3)", File = "blip_combo3.wav", IsComboVariant = true, ComboTier = 3 },
                new SoundEntry { Id = "blip_combo4", DisplayName = "Boss Defeated (Tier 4)", File = "blip_combo4.wav", IsComboVariant = true, ComboTier = 4 }
            }
        };

        File.WriteAllText(Path.Combine(outDir, "pack.json"), JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true }));
    }

    #endregion

    #region Audio Synthesis Algorithms

    private static float[] SynthesizePistol(float freqStart, float freqEnd, float durationSec, float noiseMix, bool isHeavy = false)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(55);

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var currentFreq = freqStart * Math.Pow(freqEnd / freqStart, t * 1.6);
            phase += 2.0 * Math.PI * currentFreq / SampleRate;

            var sine = (float)Math.Sin(phase);
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0);

            // Fast metallic click transient at start (slide recoil)
            var click = (float)(Math.Sin(2.0 * Math.PI * 2400.0 * i / SampleRate) * Math.Exp(-t * 80.0));

            var env = (float)Math.Exp(-t * (isHeavy ? 10.0 : 16.0));
            var mixed = (sine * (1.0f - noiseMix) + noise * noiseMix + click * 0.35f) * env;

            samples[i] = Math.Clamp(mixed * 0.95f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeRifle(float freqStart, float freqEnd, float durationSec, float noiseMix, bool isHeavy = false)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(88);

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var currentFreq = freqStart * Math.Pow(freqEnd / freqStart, t * 1.3);
            phase += 2.0 * Math.PI * currentFreq / SampleRate;

            var sine = (float)Math.Sin(phase);
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0);

            // Supersonic ballistic crack + metallic chamber resonance
            var crack = (float)(Math.Sin(2.0 * Math.PI * 3400.0 * i / SampleRate) * Math.Exp(-t * 60.0));

            var env = (float)Math.Exp(-t * (isHeavy ? 9.0 : 13.0));
            var mixed = (sine * (1.0f - noiseMix) + noise * noiseMix + crack * 0.4f) * env;

            samples[i] = Math.Clamp(mixed * 0.95f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeHeavyShotgun(float durationSec, bool isDoubleBarrel = false)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(123);

        double rumblePhase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            rumblePhase += 2.0 * Math.PI * (50.0 * (1.0 - t * 0.3)) / SampleRate;

            var rumble = (float)Math.Sin(rumblePhase);
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0);

            // Dual blast impact wave
            var secondBlast = isDoubleBarrel && t > 0.06f ? (float)(rand.NextDouble() * 1.5 - 0.75) * (float)Math.Exp(-(t - 0.06f) * 12.0) : 0f;

            var env = (float)Math.Exp(-t * 7.0);
            var mixed = (rumble * 0.4f + noise * 0.6f + secondBlast) * env;

            samples[i] = Math.Clamp(mixed * 0.95f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeMechSwitch(float clickFreq, float bottomOutFreq, float durationSec, bool heavy = false)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(33);

        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;

            // 1. Tactile click leaf (high frequency snap at t=0)
            var click = (float)(Math.Sin(2.0 * Math.PI * clickFreq * i / SampleRate) * Math.Exp(-t * 90.0));

            // 2. Bottom-out plastic clack (lower resonant frequency at t=0.015)
            var bottomOutTime = heavy ? 0.012f : 0.018f;
            var bottomOut = t >= bottomOutTime
                ? (float)(Math.Sin(2.0 * Math.PI * bottomOutFreq * (i - bottomOutTime * SampleRate) / SampleRate) * Math.Exp(-(t - bottomOutTime) * 35.0))
                : 0f;

            // 3. Acoustic switch housing texture (subtle white noise impact)
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0) * (float)Math.Exp(-t * 45.0);

            var mixed = click * 0.5f + bottomOut * 0.65f + noise * 0.25f;
            samples[i] = Math.Clamp(mixed * 0.95f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeMechSpacebar(float baseThumpFreq, float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(66);

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            phase += 2.0 * Math.PI * (baseThumpFreq * (1.0 - t * 0.2)) / SampleRate;

            // Deep thocky housing resonance
            var thump = (float)Math.Sin(phase);
            var harmonic = (float)(Math.Sin(phase * 2.0) * 0.3);

            // Stabilizer wire wire-clack
            var wireClick = (float)(Math.Sin(2.0 * Math.PI * 1800.0 * i / SampleRate) * Math.Exp(-t * 70.0));
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0) * (float)Math.Exp(-t * 30.0);

            var env = (float)Math.Exp(-t * 14.0);
            var mixed = ((thump + harmonic) * 0.65f + wireClick * 0.35f + noise * 0.2f) * env;

            samples[i] = Math.Clamp(mixed * 0.95f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeGunshot(float freqStart, float freqEnd, float durationSec, float noiseMix)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(42);

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var currentFreq = freqStart * Math.Pow(freqEnd / freqStart, t);
            phase += 2.0 * Math.PI * currentFreq / SampleRate;

            var sine = (float)Math.Sin(phase);
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0);
            var env = (float)Math.Exp(-t * 12.0);
            var mixed = (sine * (1.0f - noiseMix) + noise * noiseMix) * env;

            samples[i] = Math.Clamp(mixed * 0.95f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeExplosion(float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(101);

        double rumblePhase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            rumblePhase += 2.0 * Math.PI * (55.0 * (1.0 - t * 0.4)) / SampleRate;

            var rumble = (float)Math.Sin(rumblePhase);
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0);
            var env = (float)Math.Exp(-t * 5.5);
            var mixed = (rumble * 0.45f + noise * 0.55f) * env;

            samples[i] = Math.Clamp(mixed * 0.95f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeLaser(float freqStart, float freqEnd, float durationSec, bool modulation = false)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var freq = freqStart * Math.Pow(freqEnd / freqStart, t * 1.5);
            if (modulation)
            {
                freq += 150.0 * Math.Sin(2.0 * Math.PI * 45.0 * t);
            }

            phase += 2.0 * Math.PI * freq / SampleRate;

            var wave = (float)(Math.Sin(phase) + 0.3 * Math.Sin(phase * 2.0) + 0.15 * Math.Sin(phase * 3.0));
            var env = (float)Math.Exp(-t * 4.0);

            samples[i] = Math.Clamp(wave * env * 0.85f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] SynthesizeSciFiExplosion(float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(77);

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var freq = 800.0 * Math.Pow(40.0 / 800.0, t);
            phase += 2.0 * Math.PI * freq / SampleRate;

            var sweep = (float)Math.Sin(phase);
            var noise = (float)(rand.NextDouble() * 2.0 - 1.0);
            var env = (float)Math.Exp(-t * 4.5);

            samples[i] = Math.Clamp((sweep * 0.5f + noise * 0.5f) * env * 0.9f, -1.0f, 1.0f);
        }

        return samples;
    }

    private static float[] Synthesize8BitChirp(float freqStart, float freqEnd, float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var freq = freqStart + (freqEnd - freqStart) * t;
            phase += 2.0 * Math.PI * freq / SampleRate;

            var square = Math.Sin(phase) >= 0 ? 0.7f : -0.7f;
            var env = 1.0f - t;

            samples[i] = square * env;
        }

        return samples;
    }

    private static float[] Synthesize8BitJump(float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var freq = 150.0 + 650.0 * Math.Pow(t, 0.7);
            phase += 2.0 * Math.PI * freq / SampleRate;

            var square = Math.Sin(phase) >= 0 ? 0.75f : -0.75f;
            var env = (float)Math.Exp(-t * 3.5);

            samples[i] = square * env;
        }

        return samples;
    }

    private static float[] Synthesize8BitCoin(float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var freq = t < 0.3f ? 987.77 : 1318.51;
            phase += 2.0 * Math.PI * freq / SampleRate;

            var square = Math.Sin(phase) >= 0 ? 0.7f : -0.7f;
            var env = (float)Math.Exp(-t * 4.0);

            samples[i] = square * env;
        }

        return samples;
    }

    private static float[] Synthesize8BitPowerUp(float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        double[] notes = { 330, 392, 659, 523, 587, 784 };

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var noteIndex = Math.Clamp((int)(t * notes.Length), 0, notes.Length - 1);
            var freq = notes[noteIndex];
            phase += 2.0 * Math.PI * freq / SampleRate;

            var square = Math.Sin(phase) >= 0 ? 0.7f : -0.7f;
            var env = (float)Math.Exp(-t * 2.0);

            samples[i] = square * env;
        }

        return samples;
    }

    private static float[] Synthesize8BitFanfare(float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        double[] notes = { 523.25, 659.25, 783.99, 1046.50 };

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            var noteIndex = Math.Clamp((int)(t * notes.Length), 0, notes.Length - 1);
            var freq = notes[noteIndex];
            phase += 2.0 * Math.PI * freq / SampleRate;

            var square = Math.Sin(phase) >= 0 ? 0.7f : -0.7f;
            var env = 1.0f - (t * 0.5f);

            samples[i] = square * env;
        }

        return samples;
    }

    private static float[] Synthesize8BitExplosion(float durationSec)
    {
        var numSamples = (int)(SampleRate * durationSec);
        var samples = new float[numSamples];
        var rand = new Random(99);

        float currentSample = 0.5f;
        for (int i = 0; i < numSamples; i++)
        {
            var t = (float)i / numSamples;
            if (i % 8 == 0)
            {
                currentSample = (float)(rand.Next(2) * 2 - 1) * 0.8f;
            }

            var env = (float)Math.Exp(-t * 5.0);
            samples[i] = currentSample * env;
        }

        return samples;
    }

    private static void WriteWavFile(string filePath, float[] samples)
    {
        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        short channels = 1;
        short bitsPerSample = 16;
        int byteRate = SampleRate * channels * (bitsPerSample / 8);
        short blockAlign = (short)(channels * (bitsPerSample / 8));
        int dataSize = samples.Length * (bitsPerSample / 8);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());

        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1.0f, 1.0f);
            var intSample = (short)(clamped * short.MaxValue);
            writer.Write(intSample);
        }
    }

    #endregion
}
