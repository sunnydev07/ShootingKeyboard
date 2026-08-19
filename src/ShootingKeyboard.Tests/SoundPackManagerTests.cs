using System;
using System.Linq;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class SoundPackManagerTests
{
    [Fact]
    public void SoundPackManager_Refresh_DiscoversBundledPacks()
    {
        var manager = new SoundPackManager();
        manager.Refresh();

        var packs = manager.GetPacks();
        Assert.NotEmpty(packs);

        var packIds = packs.Select(p => p.Id.ToLowerInvariant()).ToList();
        Assert.Contains("warzone", packIds);
        Assert.Contains("scifi", packIds);
        Assert.Contains("retro-arcade", packIds);
    }

    [Fact]
    public void SoundPackManager_GetPack_ReturnsPackWithSounds()
    {
        var manager = new SoundPackManager();
        manager.Refresh();

        var warzone = manager.GetPack("warzone");
        Assert.NotNull(warzone);
        Assert.Equal("Warzone", warzone.Name);
        Assert.NotEmpty(warzone.Sounds);
    }

    [Fact]
    public void SoundPackManager_SetActivePack_TogglesActivePackAndRaisesEvent()
    {
        var manager = new SoundPackManager();
        manager.Refresh();

        var eventFired = false;
        manager.PacksChanged += (s, e) => eventFired = true;

        var success = manager.SetActivePack("scifi");
        Assert.True(success);
        Assert.NotNull(manager.ActivePack);
        Assert.Equal("scifi", manager.ActivePack.Id);
        Assert.True(eventFired);
    }

    [Fact]
    public void SoundPackManager_SetActivePack_InvalidId_ReturnsFalse()
    {
        var manager = new SoundPackManager();
        manager.Refresh();

        var success = manager.SetActivePack("nonexistent_pack_id");
        Assert.False(success);
    }

    [Fact]
    public void SoundPackManager_ResolvesVariantFilePathsToAbsolute()
    {
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_pack_" + Guid.NewGuid());
        System.IO.Directory.CreateDirectory(tempDir);
        try
        {
            var json = """
            {
                "id": "variant_test",
                "name": "Variant Test",
                "sounds": [
                    {
                        "id": "shot1",
                        "file": "shot1.wav",
                        "variants": ["var1.wav", "var2.wav"]
                    }
                ]
            }
            """;
            var pack = System.Text.Json.JsonSerializer.Deserialize<SoundPack>(json, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })!;
            foreach (var sound in pack.Sounds)
            {
                if (!System.IO.Path.IsPathRooted(sound.File))
                    sound.File = System.IO.Path.Combine(tempDir, sound.File);
                if (sound.Variants != null)
                {
                    for (int i = 0; i < sound.Variants.Count; i++)
                    {
                        if (!System.IO.Path.IsPathRooted(sound.Variants[i]))
                            sound.Variants[i] = System.IO.Path.Combine(tempDir, sound.Variants[i]);
                    }
                }
            }

            Assert.True(System.IO.Path.IsPathRooted(pack.Sounds[0].Variants[0]));
            Assert.True(System.IO.Path.IsPathRooted(pack.Sounds[0].Variants[1]));
            Assert.Contains(tempDir, pack.Sounds[0].Variants[0]);
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir, true);
        }
    }
}
