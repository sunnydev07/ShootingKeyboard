using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class SoundPackValidator : ISoundPackValidator
{
    public SoundPackValidationResult Validate(SoundPack pack)
    {
        var result = new SoundPackValidationResult
        {
            PackId = pack?.Id ?? string.Empty,
            PackName = pack?.Name ?? string.Empty
        };

        if (pack == null)
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "pack.null",
                Message = "Sound pack cannot be null."
            });
            return result;
        }

        // Pack ID
        if (string.IsNullOrWhiteSpace(pack.Id))
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "pack.id.empty",
                Message = "Sound pack ID cannot be empty."
            });
        }

        // Pack Name
        if (string.IsNullOrWhiteSpace(pack.Name))
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "pack.name.empty",
                Message = "Sound pack Name cannot be empty."
            });
        }

        // Defaults warnings
        if (pack.Defaults != null)
        {
            if (pack.Defaults.Volume < 0f || pack.Defaults.Volume > 1f)
            {
                result.Issues.Add(new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Warning,
                    Code = "pack.defaultVolume.outOfRange",
                    Message = $"Default volume {pack.Defaults.Volume} is outside valid range (0.0 to 1.0)."
                });
            }

            if (pack.Defaults.ComboWindowMs < 50 || pack.Defaults.ComboWindowMs > 2000)
            {
                result.Issues.Add(new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Warning,
                    Code = "pack.comboWindow.outOfRange",
                    Message = $"Default combo window {pack.Defaults.ComboWindowMs}ms is outside valid range (50ms to 2000ms)."
                });
            }
        }

        // Sounds list
        if (pack.Sounds == null || pack.Sounds.Count == 0)
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "sounds.empty",
                Message = "Sound pack must contain at least one sound entry."
            });
            return result;
        }

        var seenSoundIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sound in pack.Sounds)
        {
            if (sound == null) continue;

            // Sound ID empty
            if (string.IsNullOrWhiteSpace(sound.Id))
            {
                result.Issues.Add(new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Error,
                    Code = "sound.id.empty",
                    Message = "Sound ID cannot be empty.",
                    SoundId = sound.Id
                });
            }
            else
            {
                // Sound ID duplicate
                if (!seenSoundIds.Add(sound.Id))
                {
                    result.Issues.Add(new SoundPackValidationIssue
                    {
                        Severity = SoundPackValidationSeverity.Error,
                        Code = "sound.id.duplicate",
                        Message = $"Duplicate sound ID '{sound.Id}'.",
                        SoundId = sound.Id
                    });
                }
            }

            // Sound file empty
            if (string.IsNullOrWhiteSpace(sound.File))
            {
                result.Issues.Add(new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Error,
                    Code = "sound.file.empty",
                    Message = $"Sound '{sound.Id}' has no file path specified.",
                    SoundId = sound.Id
                });
            }
            else
            {
                // Sound file missing
                if (!File.Exists(sound.File))
                {
                    result.Issues.Add(new SoundPackValidationIssue
                    {
                        Severity = SoundPackValidationSeverity.Error,
                        Code = "sound.file.missing",
                        Message = $"Sound file not found: {sound.File}",
                        SoundId = sound.Id,
                        FilePath = sound.File
                    });
                }
            }

            // Sound variants missing
            if (sound.Variants != null)
            {
                foreach (var variant in sound.Variants)
                {
                    if (string.IsNullOrWhiteSpace(variant))
                    {
                        result.Issues.Add(new SoundPackValidationIssue
                        {
                            Severity = SoundPackValidationSeverity.Error,
                            Code = "sound.variant.empty",
                            Message = $"Sound '{sound.Id}' has an empty variant file path.",
                            SoundId = sound.Id
                        });
                    }
                    else if (!File.Exists(variant))
                    {
                        result.Issues.Add(new SoundPackValidationIssue
                        {
                            Severity = SoundPackValidationSeverity.Error,
                            Code = "sound.variant.missing",
                            Message = $"Sound variant file not found: {variant}",
                            SoundId = sound.Id,
                            FilePath = variant
                        });
                    }
                }
            }

            // Sound volume
            if (sound.Volume < 0f || sound.Volume > 1f)
            {
                result.Issues.Add(new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Error,
                    Code = "sound.volume.outOfRange",
                    Message = $"Sound '{sound.Id}' volume {sound.Volume} is outside valid range (0.0 to 1.0).",
                    SoundId = sound.Id
                });
            }

            // Sound group
            if (!string.IsNullOrWhiteSpace(sound.Group) && !KeyGroups.All.Contains(sound.Group))
            {
                result.Issues.Add(new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Error,
                    Code = "sound.group.invalid",
                    Message = $"Sound '{sound.Id}' has invalid group '{sound.Group}'.",
                    SoundId = sound.Id
                });
            }

            // Combo tier
            if (sound.IsComboVariant && (sound.ComboTier < 1 || sound.ComboTier > 4))
            {
                result.Issues.Add(new SoundPackValidationIssue
                {
                    Severity = SoundPackValidationSeverity.Error,
                    Code = "sound.comboTier.outOfRange",
                    Message = $"Combo variant sound '{sound.Id}' has combo tier {sound.ComboTier} outside valid range (1 to 4).",
                    SoundId = sound.Id
                });
            }
        }

        return result;
    }

    public SoundPackValidationResult ValidatePackFolder(string packDirectory)
    {
        var result = new SoundPackValidationResult();
        if (string.IsNullOrWhiteSpace(packDirectory) || !Directory.Exists(packDirectory))
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "directory.missing",
                Message = $"Pack directory does not exist: {packDirectory}"
            });
            return result;
        }

        var jsonPath = Path.Combine(packDirectory, "pack.json");
        if (!File.Exists(jsonPath))
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "pack.json.missing",
                Message = "pack.json not found in pack directory."
            });
            return result;
        }

        SoundPack? pack;
        try
        {
            var json = File.ReadAllText(jsonPath);
            pack = System.Text.Json.JsonSerializer.Deserialize<SoundPack>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "pack.json.invalid",
                Message = $"Failed to parse pack.json: {ex.Message}"
            });
            return result;
        }

        if (pack == null)
        {
            result.Issues.Add(new SoundPackValidationIssue
            {
                Severity = SoundPackValidationSeverity.Error,
                Code = "pack.null",
                Message = "Deserialized sound pack is null."
            });
            return result;
        }

        // Resolve paths
        foreach (var sound in pack.Sounds)
        {
            if (!string.IsNullOrWhiteSpace(sound.File) && !Path.IsPathRooted(sound.File))
            {
                sound.File = Path.Combine(packDirectory, sound.File);
            }
            for (int i = 0; i < sound.Variants.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(sound.Variants[i]) && !Path.IsPathRooted(sound.Variants[i]))
                {
                    sound.Variants[i] = Path.Combine(packDirectory, sound.Variants[i]);
                }
            }
        }

        return Validate(pack);
    }
}
