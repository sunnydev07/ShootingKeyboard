using System;
using System.Linq;
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
}
