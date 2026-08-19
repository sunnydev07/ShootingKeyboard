using System;
using System.Collections.Generic;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class SoundVariantSelectorTests
{
    [Fact]
    public void GetVariantAudioId_ReturnsFormattedVariantId()
    {
        var selector = new SoundVariantSelector();
        var id = selector.GetVariantAudioId("shot_default", 2);

        Assert.Equal("shot_default::variant::2", id);
    }

    [Fact]
    public void SelectClip_NoVariants_ReturnsBaseSoundEntryClip()
    {
        var selector = new SoundVariantSelector();
        var entry = new SoundEntry
        {
            Id = "shot1",
            File = "C:\\sounds\\shot1.wav",
            Volume = 0.85f,
            Variants = new List<string>()
        };

        var clip = selector.SelectClip(entry);

        Assert.Equal("shot1", clip.AudioId);
        Assert.Equal("C:\\sounds\\shot1.wav", clip.FilePath);
        Assert.Equal(0.85f, clip.Volume);
    }

    [Fact]
    public void SelectClip_WithVariants_DeterministicIndex0_ReturnsBaseClip()
    {
        // Force random picker to return 0 (base file)
        var selector = new SoundVariantSelector(max => 0);
        var entry = new SoundEntry
        {
            Id = "shot1",
            File = "C:\\sounds\\shot1.wav",
            Volume = 0.9f,
            Variants = new List<string> { "C:\\sounds\\var1.wav", "C:\\sounds\\var2.wav" }
        };

        var clip = selector.SelectClip(entry);

        Assert.Equal("shot1", clip.AudioId);
        Assert.Equal("C:\\sounds\\shot1.wav", clip.FilePath);
        Assert.Equal(0.9f, clip.Volume);
    }

    [Fact]
    public void SelectClip_WithVariants_DeterministicIndex1_ReturnsFirstVariant()
    {
        // Force random picker to return 1 (first variant)
        var selector = new SoundVariantSelector(max => 1);
        var entry = new SoundEntry
        {
            Id = "shot1",
            File = "C:\\sounds\\shot1.wav",
            Volume = 0.9f,
            Variants = new List<string> { "C:\\sounds\\var1.wav", "C:\\sounds\\var2.wav" }
        };

        var clip = selector.SelectClip(entry);

        Assert.Equal("shot1::variant::0", clip.AudioId);
        Assert.Equal("C:\\sounds\\var1.wav", clip.FilePath);
        Assert.Equal(0.9f, clip.Volume);
    }

    [Fact]
    public void SelectClip_WithVariants_DeterministicIndex2_ReturnsSecondVariant()
    {
        // Force random picker to return 2 (second variant)
        var selector = new SoundVariantSelector(max => 2);
        var entry = new SoundEntry
        {
            Id = "shot1",
            File = "C:\\sounds\\shot1.wav",
            Volume = 0.9f,
            Variants = new List<string> { "C:\\sounds\\var1.wav", "C:\\sounds\\var2.wav" }
        };

        var clip = selector.SelectClip(entry);

        Assert.Equal("shot1::variant::1", clip.AudioId);
        Assert.Equal("C:\\sounds\\var2.wav", clip.FilePath);
        Assert.Equal(0.9f, clip.Volume);
    }
}
