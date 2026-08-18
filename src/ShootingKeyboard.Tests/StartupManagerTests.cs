using System;
using Microsoft.Win32;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class StartupManagerTests
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ShootingKeyboard";

    [Fact]
    public void IsStartupEnabled_DoesNotThrow()
    {
        var manager = new StartupManager();
        var ex = Record.Exception(() => manager.IsStartupEnabled());
        Assert.Null(ex);
    }

    [Fact]
    public void SetStartupEnabled_ToggleTrueAndFalse_ModifiesRegistrySafely()
    {
        var manager = new StartupManager();

        // Remember initial state
        var initialEnabled = manager.IsStartupEnabled();

        try
        {
            // Test enabling
            manager.SetStartupEnabled(true);
            var isEnabledAfterTrue = manager.IsStartupEnabled();
            Assert.True(isEnabledAfterTrue);

            // Test disabling
            manager.SetStartupEnabled(false);
            var isEnabledAfterFalse = manager.IsStartupEnabled();
            Assert.False(isEnabledAfterFalse);
        }
        finally
        {
            // Restore initial state
            manager.SetStartupEnabled(initialEnabled);
        }
    }
}
