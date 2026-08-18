using System;
using System.Threading;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class ComboTrackerTests
{
    [Fact]
    public void ComboTracker_InitialState_IsZero()
    {
        using var tracker = new ComboTracker();
        Assert.Equal(0, tracker.ComboCount);
        Assert.Equal(0, tracker.CurrentTier);
    }

    [Fact]
    public void ComboTracker_RegisterKeyPress_IncrementsComboAndFiresEvent()
    {
        using var tracker = new ComboTracker();
        var reportedCount = 0;
        tracker.ComboChanged += (s, count) => reportedCount = count;

        tracker.RegisterKeyPress();
        Assert.Equal(1, tracker.ComboCount);
        Assert.Equal(1, reportedCount);

        tracker.RegisterKeyPress();
        Assert.Equal(2, tracker.ComboCount);
        Assert.Equal(2, reportedCount);
    }

    [Fact]
    public void ComboTracker_ReachesHigherTiers()
    {
        using var tracker = new ComboTracker();
        var highestTier = 0;
        tracker.TierChanged += (s, tier) => highestTier = Math.Max(highestTier, tier);

        // Register 45 presses to advance through tiers (5, 10, 20, 40)
        for (int i = 0; i < 45; i++)
        {
            tracker.RegisterKeyPress();
        }

        Assert.Equal(45, tracker.ComboCount);
        Assert.Equal(4, tracker.CurrentTier);
        Assert.Equal(4, highestTier);
    }

    [Fact]
    public void ComboTracker_Reset_ResetsComboAndTier()
    {
        using var tracker = new ComboTracker();
        tracker.RegisterKeyPress();
        tracker.RegisterKeyPress();
        Assert.Equal(2, tracker.ComboCount);

        tracker.Reset();
        Assert.Equal(0, tracker.ComboCount);
        Assert.Equal(0, tracker.CurrentTier);
    }

    [Fact]
    public void ComboTracker_Timeout_ResetsCombo()
    {
        using var tracker = new ComboTracker();
        tracker.ComboWindowMs = 60; // 60ms window

        tracker.RegisterKeyPress();
        Assert.Equal(1, tracker.ComboCount);

        // Wait for timeout
        Thread.Sleep(120);

        Assert.Equal(0, tracker.ComboCount);
        Assert.Equal(0, tracker.CurrentTier);
    }
}
