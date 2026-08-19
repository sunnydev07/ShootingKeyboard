using System;
using System.IO;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class ProfileImportExportServiceTests : IDisposable
{
    private readonly ProfileImportExportService _service = new();
    private readonly string _tempDir;

    public ProfileImportExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SK_ProfileTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public void ExportAndImport_RoundTrip_Succeeds()
    {
        var profile = new AppProfile
        {
            Id = "custom_profile",
            Name = "Custom Profile",
            MasterVolume = 0.65f,
            ActivePackId = "scifi",
            ComboWindowMs = 350,
            KeyBindings = { [0x41] = "laser" },
            GroupBindings = { ["WASD"] = "move_sound" },
            GroupVolumeOverrides = { ["WASD"] = 0.5f },
            KeyVolumeOverrides = { [0x41] = 0.8f },
            PlaybackFilter = new PlaybackFilterConfig
            {
                IgnoreKeyRepeats = false,
                GlobalCooldownMs = 15
            }
        };

        var filePath = Path.Combine(_tempDir, "exported_profile.json");

        // Export
        _service.ExportProfile(profile, filePath);
        Assert.True(File.Exists(filePath));

        // Import
        var imported = _service.ImportProfile(filePath);

        Assert.NotNull(imported);
        Assert.Equal("custom_profile", imported.Id);
        Assert.Equal("Custom Profile", imported.Name);
        Assert.Equal(0.65f, imported.MasterVolume);
        Assert.Equal("scifi", imported.ActivePackId);
        Assert.Equal(350, imported.ComboWindowMs);
        Assert.Equal("laser", imported.KeyBindings[0x41]);
        Assert.Equal("move_sound", imported.GroupBindings["WASD"]);
        Assert.Equal(0.5f, imported.GroupVolumeOverrides["WASD"]);
        Assert.Equal(0.8f, imported.KeyVolumeOverrides[0x41]);
        Assert.False(imported.PlaybackFilter.IgnoreKeyRepeats);
        Assert.Equal(15, imported.PlaybackFilter.GlobalCooldownMs);
    }

    [Fact]
    public void ImportProfile_InvalidJson_ThrowsInvalidDataException()
    {
        var filePath = Path.Combine(_tempDir, "invalid.json");
        File.WriteAllText(filePath, "{ not valid json ... }");

        Assert.Throws<InvalidDataException>(() => _service.ImportProfile(filePath));
    }

    [Fact]
    public void ImportProfile_MissingNameOrId_ThrowsInvalidDataException()
    {
        var filePath = Path.Combine(_tempDir, "noname.json");
        File.WriteAllText(filePath, "{\"masterVolume\": 0.8}");

        Assert.Throws<InvalidDataException>(() => _service.ImportProfile(filePath));
    }

    [Fact]
    public void ImportProfile_ClampsOutOfRangeValues()
    {
        var filePath = Path.Combine(_tempDir, "clamped.json");
        var json = """
        {
            "id": "test_id",
            "name": "Clamped Test",
            "masterVolume": 2.5,
            "comboWindowMs": 99999,
            "playbackFilter": {
                "globalCooldownMs": 50000
            }
        }
        """;
        File.WriteAllText(filePath, json);

        var imported = _service.ImportProfile(filePath);

        Assert.Equal(1.0f, imported.MasterVolume);
        Assert.Equal(2000, imported.ComboWindowMs);
        Assert.Equal(1000, imported.PlaybackFilter.GlobalCooldownMs);
    }
}
