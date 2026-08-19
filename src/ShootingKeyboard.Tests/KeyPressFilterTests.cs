using System;
using System.Collections.Generic;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class KeyPressFilterTests
{
    private DateTimeOffset _currentTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private KeyPressFilter CreateFilter()
    {
        return new KeyPressFilter(() => _currentTime);
    }

    [Fact]
    public void ShouldProcess_FirstKeyDown_ReturnsTrue()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        var keyEvent = new KeyEvent(0x41, true); // 'A' down

        var result = filter.ShouldProcess(keyEvent, config);

        Assert.True(result);
    }

    [Fact]
    public void ShouldProcess_KeyUp_ReturnsFalseAndClearsPressed()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        var keyDown = new KeyEvent(0x41, true);
        var keyUp = new KeyEvent(0x41, false);

        filter.ShouldProcess(keyDown, config);
        var upResult = filter.ShouldProcess(keyUp, config);

        Assert.False(upResult);

        // After key up, pressing again at later time should be allowed
        _currentTime = _currentTime.AddMilliseconds(50);
        var secondDown = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.True(secondDown);
    }

    [Fact]
    public void ShouldProcess_HeldRepeatKeyDown_WhenIgnoreKeyRepeatsTrue_ReturnsFalse()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        config.PlaybackFilter.IgnoreKeyRepeats = true;

        var firstDown = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.True(firstDown);

        _currentTime = _currentTime.AddMilliseconds(50);
        var repeatDown = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.False(repeatDown);
    }

    [Fact]
    public void ShouldProcess_HeldRepeatKeyDown_WhenIgnoreKeyRepeatsFalse_AllowedAfterCooldown()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        config.PlaybackFilter.IgnoreKeyRepeats = false;
        config.PlaybackFilter.GlobalCooldownMs = 30;

        var firstDown = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.True(firstDown);

        // Immediate repeat within cooldown
        _currentTime = _currentTime.AddMilliseconds(10);
        var immediateRepeat = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.False(immediateRepeat);

        // Repeat after cooldown passes
        _currentTime = _currentTime.AddMilliseconds(25);
        var afterCooldown = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.True(afterCooldown);
    }

    [Fact]
    public void ShouldProcess_GlobalCooldown_BlocksRapidKeystrokesAcrossDifferentKeys()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        config.PlaybackFilter.GlobalCooldownMs = 30;

        var first = filter.ShouldProcess(new KeyEvent(0x41, true), config); // 'A'
        Assert.True(first);

        _currentTime = _currentTime.AddMilliseconds(10);
        var second = filter.ShouldProcess(new KeyEvent(0x42, true), config); // 'B'
        Assert.False(second);

        _currentTime = _currentTime.AddMilliseconds(25);
        var third = filter.ShouldProcess(new KeyEvent(0x42, true), config); // 'B' after cooldown
        Assert.True(third);
    }

    [Fact]
    public void ShouldProcess_KeyCooldown_OverridesGlobalAndGroupCooldown()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        config.PlaybackFilter.GlobalCooldownMs = 50;
        config.PlaybackFilter.KeyCooldownMs[0x41] = 10; // 'A' has fast 10ms cooldown

        var first = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.True(first);

        // Clear pressed state via key up
        filter.ShouldProcess(new KeyEvent(0x41, false), config);

        _currentTime = _currentTime.AddMilliseconds(15); // > 10ms but < 50ms
        var second = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.True(second);
    }

    [Fact]
    public void ShouldProcess_GroupCooldown_OverridesGlobalCooldown()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        config.PlaybackFilter.GlobalCooldownMs = 100;
        config.PlaybackFilter.GroupCooldownMs[KeyGroups.Space] = 20; // Space (0x20) has 20ms cooldown

        var first = filter.ShouldProcess(new KeyEvent(0x20, true), config);
        Assert.True(first);

        // Key up
        filter.ShouldProcess(new KeyEvent(0x20, false), config);

        _currentTime = _currentTime.AddMilliseconds(25); // > 20ms but < 100ms
        var second = filter.ShouldProcess(new KeyEvent(0x20, true), config);
        Assert.True(second);
    }

    [Fact]
    public void Reset_ClearsPressedKeysAndTimestamps()
    {
        var filter = CreateFilter();
        var config = new AppConfig();
        config.PlaybackFilter.IgnoreKeyRepeats = true;

        filter.ShouldProcess(new KeyEvent(0x41, true), config);
        filter.Reset();

        // After reset, pressing 'A' again should succeed even without key-up
        var result = filter.ShouldProcess(new KeyEvent(0x41, true), config);
        Assert.True(result);
    }
}
