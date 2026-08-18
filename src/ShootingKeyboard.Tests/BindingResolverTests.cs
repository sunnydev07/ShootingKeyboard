using System.Collections.Generic;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class BindingResolverTests
{
    private readonly BindingResolver _resolver = new();

    private SoundPack CreateTestPack()
    {
        return new SoundPack
        {
            Id = "test-pack",
            Name = "Test Pack",
            Sounds = new List<SoundEntry>
            {
                new SoundEntry { Id = "gun_default", DisplayName = "Default Shot", Group = null },
                new SoundEntry { Id = "gun_space", DisplayName = "Space Shot", Group = KeyGroups.Space },
                new SoundEntry { Id = "gun_enter", DisplayName = "Enter Explosion", Group = KeyGroups.Enter },
                new SoundEntry { Id = "gun_wasd", DisplayName = "WASD Shot", Group = KeyGroups.WASD },
                new SoundEntry { Id = "gun_custom", DisplayName = "Custom Laser", Group = null },
                new SoundEntry { Id = "gun_combo_t2", DisplayName = "Combo Tier 2", IsComboVariant = true, ComboTier = 2 }
            }
        };
    }

    [Fact]
    public void ResolveSound_NullPackOrEmpty_ReturnsNull()
    {
        var config = AppConfig.CreateDefault();
        Assert.Null(_resolver.ResolveSound(0x41, null, config));

        var emptyPack = new SoundPack { Id = "empty", Sounds = new List<SoundEntry>() };
        Assert.Null(_resolver.ResolveSound(0x41, emptyPack, config));
    }

    [Fact]
    public void ResolveSound_ExplicitKeyBinding_TakesHighestPrecedence()
    {
        var pack = CreateTestPack();
        var config = AppConfig.CreateDefault();
        config.KeyBindings[0x41] = "gun_custom"; // 'A' mapped explicitly to custom sound

        var soundId = _resolver.ResolveSound(0x41, pack, config);
        Assert.Equal("gun_custom", soundId);
    }

    [Fact]
    public void ResolveSound_GroupBinding_TakesSecondPrecedence()
    {
        var pack = CreateTestPack();
        var config = AppConfig.CreateDefault();
        config.GroupBindings[KeyGroups.Letters] = "gun_custom";

        var soundId = _resolver.ResolveSound(0x42, pack, config); // 'B' is Letters group
        Assert.Equal("gun_custom", soundId);
    }

    [Fact]
    public void ResolveSound_PackGroupSound_TakesPrecedenceOverDefault()
    {
        var pack = CreateTestPack();
        var config = AppConfig.CreateDefault();

        // Space key (0x20)
        var spaceSound = _resolver.ResolveSound(0x20, pack, config);
        Assert.Equal("gun_space", spaceSound);

        // Enter key (0x0D)
        var enterSound = _resolver.ResolveSound(0x0D, pack, config);
        Assert.Equal("gun_enter", enterSound);

        // 'W' key (0x57) -> WASD group
        var wasdSound = _resolver.ResolveSound(0x57, pack, config);
        Assert.Equal("gun_wasd", wasdSound);
    }

    [Fact]
    public void ResolveSound_NormalLetter_FallsBackToDefaultSound()
    {
        var pack = CreateTestPack();
        var config = AppConfig.CreateDefault();

        // 'Z' key (0x5A)
        var soundId = _resolver.ResolveSound(0x5A, pack, config);
        Assert.Equal("gun_default", soundId);
    }
}
