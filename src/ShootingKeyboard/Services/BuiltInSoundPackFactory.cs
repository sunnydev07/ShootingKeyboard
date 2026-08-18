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

            var warzoneDir = Path.Combine(packsRootDir, "Warzone");
            if (!File.Exists(Path.Combine(warzoneDir, "pack.json")))
            {
                GenerateWarzonePack(warzoneDir);
            }

            var scifiDir = Path.Combine(packsRootDir, "SciFi");
            if (!File.Exists(Path.Combine(scifiDir, "pack.json")))
            {
                GenerateSciFiPack(scifiDir);
            }

            var retroDir = Path.Combine(packsRootDir, "RetroArcade");
            if (!File.Exists(Path.Combine(retroDir, "pack.json")))
            {
                GenerateRetroArcadePack(retroDir);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to auto-generate default sound packs: {ex.Message}");
        }
    }

    private static void GenerateWarzonePack(string outDir)
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

    private static void GenerateSciFiPack(string outDir)
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

    private static void GenerateRetroArcadePack(string outDir)
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

    #region Audio Synthesis

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
