using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class SoundPackImportExportService : ISoundPackImportExportService
{
    private readonly ISoundPackValidator _validator;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SoundPackImportExportService(ISoundPackValidator validator)
    {
        _validator = validator;
    }

    public string InstallFromZip(string zipFilePath, string userPacksRoot)
    {
        if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
        {
            throw new FileNotFoundException($"Zip file not found: {zipFilePath}");
        }

        if (string.IsNullOrWhiteSpace(userPacksRoot))
        {
            throw new ArgumentException("User packs directory cannot be empty.", nameof(userPacksRoot));
        }

        var tempExtractDir = Path.Combine(Path.GetTempPath(), "SK_PackInstall_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempExtractDir);

        try
        {
            ZipFile.ExtractToDirectory(zipFilePath, tempExtractDir);

            var packJsonFiles = Directory.GetFiles(tempExtractDir, "pack.json", SearchOption.AllDirectories);
            if (packJsonFiles.Length != 1)
            {
                throw new InvalidDataException("Sound pack zip must contain exactly one pack.json file.");
            }

            var packDir = Path.GetDirectoryName(packJsonFiles[0])!;
            var validation = _validator.ValidatePackFolder(packDir);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Issues.Select(i => i.Message));
                throw new InvalidDataException($"Sound pack validation failed: {errors}");
            }

            Directory.CreateDirectory(userPacksRoot);
            var sanitizedId = SanitizePackId(validation.PackId);
            var targetDir = Path.Combine(userPacksRoot, sanitizedId);

            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

            CopyDirectory(packDir, targetDir);
            return validation.PackId;
        }
        finally
        {
            if (Directory.Exists(tempExtractDir))
            {
                try { Directory.Delete(tempExtractDir, true); } catch { }
            }
        }
    }

    public void ExportToZip(SoundPack pack, string zipFilePath)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (string.IsNullOrWhiteSpace(zipFilePath)) throw new ArgumentException("Zip output path cannot be empty.", nameof(zipFilePath));

        var outputDir = Path.GetDirectoryName(zipFilePath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var tempExportDir = Path.Combine(Path.GetTempPath(), "SK_PackExport_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempExportDir);

        try
        {
            // Build a relative pack.json and copy audio files
            var relativePack = new SoundPack
            {
                Id = pack.Id,
                Name = pack.Name,
                Author = pack.Author,
                Description = pack.Description,
                Defaults = pack.Defaults != null ? new PackDefaults
                {
                    Volume = pack.Defaults.Volume,
                    ComboWindowMs = pack.Defaults.ComboWindowMs
                } : null
            };

            foreach (var sound in pack.Sounds)
            {
                var relativeFile = Path.GetFileName(sound.File);
                var exportedEntry = new SoundEntry
                {
                    Id = sound.Id,
                    DisplayName = sound.DisplayName,
                    File = relativeFile,
                    Group = sound.Group,
                    Volume = sound.Volume,
                    IsComboVariant = sound.IsComboVariant,
                    ComboTier = sound.ComboTier
                };

                if (File.Exists(sound.File))
                {
                    var destAudio = Path.Combine(tempExportDir, relativeFile);
                    File.Copy(sound.File, destAudio, true);
                }

                if (sound.Variants != null && sound.Variants.Count > 0)
                {
                    foreach (var variantPath in sound.Variants)
                    {
                        var variantFileName = Path.GetFileName(variantPath);
                        exportedEntry.Variants.Add(variantFileName);

                        if (File.Exists(variantPath))
                        {
                            var destVariant = Path.Combine(tempExportDir, variantFileName);
                            File.Copy(variantPath, destVariant, true);
                        }
                    }
                }

                relativePack.Sounds.Add(exportedEntry);
            }

            var packJsonPath = Path.Combine(tempExportDir, "pack.json");
            var json = JsonSerializer.Serialize(relativePack, _jsonOptions);
            File.WriteAllText(packJsonPath, json);

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            ZipFile.CreateFromDirectory(tempExportDir, zipFilePath);
        }
        finally
        {
            if (Directory.Exists(tempExportDir))
            {
                try { Directory.Delete(tempExportDir, true); } catch { }
            }
        }
    }

    private static string SanitizePackId(string packId)
    {
        var clean = new string(packId.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "pack_" + Guid.NewGuid().ToString("N")[..6] : clean;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }
}
