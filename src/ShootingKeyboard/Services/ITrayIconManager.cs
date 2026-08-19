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
    /// Updates the tray icon tooltip
    /// </summary>
    void UpdateTooltip(string tooltip);

    /// <summary>
    /// Shows a balloon notification
    /// </summary>
    void ShowNotification(string title, string message, BalloonIcon icon = BalloonIcon.Info);
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