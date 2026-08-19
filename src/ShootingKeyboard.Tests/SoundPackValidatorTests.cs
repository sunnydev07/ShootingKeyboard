using System;
using System.Collections.Generic;
using System.IO;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class SoundPackValidatorTests
{
    private readonly SoundPackValidator _validator = new();

    private (SoundPack pack, string tempFile) CreateValidPackWithFile()
    {
        var tempFile = Path.GetTempFileName();
        var pack = new SoundPack
        {
            Id = "valid_pack",
            Name = "Valid Pack",
            Author = "Tester",
            Description = "Valid test pack",
            Defaults = new PackDefaults
            {
                Volume = 0.8f,
                ComboWindowMs = 400
            },
            Sounds = new List<SoundEntry>
            {
                new SoundEntry
                {
                    Id = "shot1",
                    DisplayName = "Shot 1",
                    File = tempFile,
                    Volume = 0.9f,
                    Group = KeyGroups.Space,
                    IsComboVariant = false,
                    ComboTier = 0
                },
                new SoundEntry
                {
                    Id = "combo_shot1",
                    DisplayName = "Combo Shot 1",
                    File = tempFile,
                    Volume = 1.0f,
                    Group = null,
                    IsComboVariant = true,
                    ComboTier = 1
                }
            }
        };

        return (pack, tempFile);
    }

    [Fact]
    public void Validate_ValidPack_ReturnsIsValidTrueAndNoErrors()
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            var result = _validator.Validate(pack);

            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
            Assert.Equal("valid_pack", result.PackId);
            Assert.Equal("Valid Pack", result.PackName);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyPackId_ReturnsPackIdEmptyError(string? packId)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Id = packId!;
            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "pack.id.empty");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyPackName_ReturnsPackNameEmptyError(string? packName)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Name = packName!;
            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "pack.name.empty");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_EmptySounds_ReturnsSoundsEmptyError()
    {
        var pack = new SoundPack
        {
            Id = "empty_pack",
            Name = "Empty Pack",
            Sounds = new List<SoundEntry>()
        };

        var result = _validator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sounds.empty");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptySoundId_ReturnsSoundIdEmptyError(string? soundId)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Sounds[0].Id = soundId!;
            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.id.empty");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_DuplicateSoundIdCaseInsensitive_ReturnsSoundIdDuplicateError()
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Sounds.Add(new SoundEntry
            {
                Id = "SHOT1", // Duplicate of "shot1"
                DisplayName = "Shot 1 Duplicate",
                File = tempFile,
                Volume = 1.0f
            });

            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.id.duplicate");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptySoundFile_ReturnsSoundFileEmptyError(string? file)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Sounds[0].File = file!;
            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.file.empty");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_MissingSoundFile_ReturnsSoundFileMissingError()
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            pack.Sounds[0].File = nonExistentPath;

            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.file.missing" && i.FilePath == nonExistentPath);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_SoundVolumeOutOfRange_ReturnsSoundVolumeOutOfRangeError(float volume)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Sounds[0].Volume = volume;
            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.volume.outOfRange");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_InvalidSoundGroup_ReturnsSoundGroupInvalidError()
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Sounds[0].Group = "NonExistentGroup";
            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.group.invalid");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void Validate_ComboVariantComboTierOutOfRange_ReturnsComboTierOutOfRangeError(int comboTier)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Sounds[1].IsComboVariant = true;
            pack.Sounds[1].ComboTier = comboTier;
            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.comboTier.outOfRange");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.5f)]
    public void Validate_DefaultVolumeOutOfRange_ReturnsPackDefaultVolumeOutOfRangeWarning(float volume)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Defaults.Volume = volume;
            var result = _validator.Validate(pack);

            Assert.True(result.IsValid); // Warnings do not make IsValid false
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Warning && i.Code == "pack.defaultVolume.outOfRange");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(49)]
    [InlineData(2001)]
    public void Validate_DefaultComboWindowOutOfRange_ReturnsPackComboWindowOutOfRangeWarning(int comboWindowMs)
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            pack.Defaults.ComboWindowMs = comboWindowMs;
            var result = _validator.Validate(pack);

            Assert.True(result.IsValid); // Warnings do not make IsValid false
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Warning && i.Code == "pack.comboWindow.outOfRange");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_MissingSoundVariantFile_ReturnsSoundVariantMissingError()
    {
        var (pack, tempFile) = CreateValidPackWithFile();
        try
        {
            var missingVariantPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            pack.Sounds[0].Variants.Add(missingVariantPath);

            var result = _validator.Validate(pack);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Severity == SoundPackValidationSeverity.Error && i.Code == "sound.variant.missing" && i.FilePath == missingVariantPath);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
