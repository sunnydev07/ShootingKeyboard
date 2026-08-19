using System;
using System.Collections.Generic;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Interface for system tray icon management
/// </summary>
public interface ITrayIconManager : IDisposable
{
    /// <summary>
    /// Shows the settings window
    /// </summary>
    event EventHandler? ShowSettingsRequested;

    /// <summary>
    /// Shows the diagnostics window
    /// </summary>
    event EventHandler? DiagnosticsRequested;

    /// <summary>
    /// Toggles mute state
    /// </summary>
    event EventHandler? ToggleMuteRequested;

    /// <summary>
    /// Toggles enabled state (pause/resume)
    /// </summary>
    event EventHandler? ToggleEnabledRequested;

    /// <summary>
    /// Requests application exit
    /// </summary>
    event EventHandler? ExitRequested;

    /// <summary>
    /// Event raised when a profile is selected from the quick menu
    /// </summary>
    event EventHandler<string>? ProfileSelected;

    /// <summary>
    /// Event raised when a sound pack is selected from the quick menu
    /// </summary>
    event EventHandler<string>? SoundPackSelected;

    /// <summary>
    /// Event raised when a volume level is selected from the quick menu
    /// </summary>
    event EventHandler<float>? VolumeSelected;

    /// <summary>
    /// Event raised when overlay toggle is requested
    /// </summary>
    event EventHandler? ToggleOverlayRequested;

    /// <summary>
    /// Updates the tray icon tooltip
    /// </summary>
    void UpdateTooltip(string tooltip);

    /// <summary>
    /// Shows a balloon notification
    /// </summary>
    void ShowNotification(string title, string message, BalloonIcon icon = BalloonIcon.Info);

    /// <summary>
    /// Rebuilds quick menus (profiles, sound packs, volume) in tray context menu
    /// </summary>
    void RebuildQuickMenus(IReadOnlyList<AppProfile> profiles, string activeProfileId, IReadOnlyList<SoundPack> packs, string activePackId);
}

/// <summary>
/// Balloon notification icons
/// </summary>
public enum BalloonIcon
{
    None,
    Info,
    Warning,
    Error
}