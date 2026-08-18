using System;
using System.IO;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class AudioGenerator
{
    [Fact]
    public void GenerateAllSoundPacks()
    {
        var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var soundPacksDir = Path.Combine(solutionDir, "sound-packs");
        var defaultSoundsDir = Path.Combine(solutionDir, "src", "ShootingKeyboard", "Resources", "DefaultSounds");

        // 1. Warzone
        BuiltInSoundPackFactory.GenerateWarzonePack(Path.Combine(soundPacksDir, "Warzone"));
        // 2. Tactical Pistol
        BuiltInSoundPackFactory.GeneratePistolPack(Path.Combine(soundPacksDir, "Pistol"));
        // 3. Assault Rifle
        BuiltInSoundPackFactory.GenerateRiflePack(Path.Combine(soundPacksDir, "AssaultRifle"));
        // 4. Heavy Shotguns & Sniper
        BuiltInSoundPackFactory.GenerateHeavyGunshotPack(Path.Combine(soundPacksDir, "HeavyGunshot"));
        // 5. Mechanical Keyboard
        BuiltInSoundPackFactory.GenerateMechanicalKeyboardPack(Path.Combine(soundPacksDir, "MechanicalKeyboard"));
        // 6. Sci-Fi
        BuiltInSoundPackFactory.GenerateSciFiPack(Path.Combine(soundPacksDir, "SciFi"));
        // 7. Retro Arcade
        BuiltInSoundPackFactory.GenerateRetroArcadePack(Path.Combine(soundPacksDir, "RetroArcade"));

        // Copy to DefaultSounds resources
        Directory.CreateDirectory(defaultSoundsDir);
        foreach (var packName in new[] { "Warzone", "Pistol", "AssaultRifle", "HeavyGunshot", "MechanicalKeyboard", "SciFi", "RetroArcade" })
        {
            CopyDirectory(Path.Combine(soundPacksDir, packName), Path.Combine(defaultSoundsDir, packName));
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
    }
}
