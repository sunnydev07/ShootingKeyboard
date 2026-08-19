using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class SoundPackImportExportServiceTests : IDisposable
{
    private readonly SoundPackImportExportService _service;
    private readonly SoundPackValidator _validator = new();
    private readonly string _tempDir;
    private readonly string _userPacksDir;

    public SoundPackImportExportServiceTests()
    {
        _service = new SoundPackImportExportService(_validator);
        _tempDir = Path.Combine(Path.GetTempPath(), "SK_PackIOTests_" + Guid.NewGuid().ToString("N"));
        _userPacksDir = Path.Combine(_tempDir, "user_packs");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_userPacksDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private string CreateSampleZip(string packId, bool includeSoundFile = true, bool includePackJson = true)
    {
        var packFolder = Path.Combine(_tempDir, "pack_src_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packFolder);

        if (includeSoundFile)
        {
            File.WriteAllText(Path.Combine(packFolder, "test.wav"), "RIFFdummywavdata");
        }

        if (includePackJson)
        {
            var pack = new SoundPack
            {
                Id = packId,
                Name = "Test Pack",
                Sounds = new List<SoundEntry>
                {
                    new SoundEntry { Id = "test_s", DisplayName = "Test", File = "test.wav" }
                }
            };
            File.WriteAllText(Path.Combine(packFolder, "pack.json"), JsonSerializer.Serialize(pack));
        }

        var zipPath = Path.Combine(_tempDir, $"{packId}.zip");
        ZipFile.CreateFromDirectory(packFolder, zipPath);
        Directory.Delete(packFolder, true);
        return zipPath;
    }

    [Fact]
    public void InstallFromZip_ValidPack_ExtractsAndInstallsToUserPacksDir()
    {
        var zipPath = CreateSampleZip("my_awesome_pack");

        var installedId = _service.InstallFromZip(zipPath, _userPacksDir);

        Assert.Equal("my_awesome_pack", installedId);
        var targetDir = Path.Combine(_userPacksDir, "my_awesome_pack");
        Assert.True(Directory.Exists(targetDir));
        Assert.True(File.Exists(Path.Combine(targetDir, "pack.json")));
        Assert.True(File.Exists(Path.Combine(targetDir, "test.wav")));
    }

    [Fact]
    public void InstallFromZip_MissingPackJson_ThrowsInvalidDataException()
    {
        var zipPath = CreateSampleZip("no_json_pack", includeSoundFile: true, includePackJson: false);

        Assert.Throws<InvalidDataException>(() => _service.InstallFromZip(zipPath, _userPacksDir));
    }

    [Fact]
    public void InstallFromZip_MissingAudioFile_ThrowsInvalidDataException()
    {
        var zipPath = CreateSampleZip("missing_audio_pack", includeSoundFile: false, includePackJson: true);

        Assert.Throws<InvalidDataException>(() => _service.InstallFromZip(zipPath, _userPacksDir));
    }

    [Fact]
    public void ExportToZip_CreatesValidZipContainingPackJsonAndAudio()
    {
        var soundPath = Path.Combine(_tempDir, "laser.wav");
        File.WriteAllText(soundPath, "RIFFaudio");

        var pack = new SoundPack
        {
            Id = "export_pack",
            Name = "Export Pack",
            Sounds = new List<SoundEntry>
            {
                new SoundEntry { Id = "laser_id", DisplayName = "Laser", File = soundPath }
            }
        };

        var zipOutput = Path.Combine(_tempDir, "exported.zip");

        // Export
        _service.ExportToZip(pack, zipOutput);

        Assert.True(File.Exists(zipOutput));

        // Verify inside zip
        using var archive = ZipFile.OpenRead(zipOutput);
        Assert.NotNull(archive.GetEntry("pack.json"));
        Assert.NotNull(archive.GetEntry("laser.wav"));
    }
}
