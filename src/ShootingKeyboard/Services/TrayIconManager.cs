using System;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace ShootingKeyboard.Services;

/// <summary>
/// System tray icon manager using H.NotifyIcon
/// </summary>
public sealed class TrayIconManager : ITrayIconManager
{
    private readonly TaskbarIcon _trayIcon;
    private bool _disposed = false;

    public event EventHandler? ShowSettingsRequested;
    public event EventHandler? DiagnosticsRequested;
    public event EventHandler? ToggleMuteRequested;
    public event EventHandler? ToggleEnabledRequested;
    public event EventHandler? ExitRequested;

    public TrayIconManager()
    {
        _trayIcon = new TaskbarIcon();

        try
        {
            var streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Icons/app.ico", UriKind.Absolute));
            if (streamInfo?.Stream != null)
            {
                using var icon = new System.Drawing.Icon(streamInfo.Stream);
                _trayIcon.Icon = (System.Drawing.Icon)icon.Clone();
            }
            else
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        catch
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        _trayIcon.ToolTipText = "Shooting Keyboard — Running";
        _trayIcon.ForceCreate(true);

        // Create context menu
        var contextMenu = new ContextMenu();

        var showSettingsItem = new MenuItem { Header = "Settings & Dashboard" };
        showSettingsItem.Click += (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(showSettingsItem);

        var showDiagnosticsItem = new MenuItem { Header = "Diagnostics" };
        showDiagnosticsItem.Click += (s, e) => DiagnosticsRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(showDiagnosticsItem);

        var toggleMuteItem = new MenuItem { Header = "Mute/Unmute Sounds" };
        toggleMuteItem.Click += (s, e) => ToggleMuteRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(toggleMuteItem);

        var toggleEnabledItem = new MenuItem { Header = "Pause/Resume Interception" };
        toggleEnabledItem.Click += (s, e) => ToggleEnabledRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(toggleEnabledItem);

        contextMenu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit Application" };
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;

        // Left click shows settings
        _trayIcon.TrayLeftMouseDown += (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateTooltip(string tooltip)
    {
        if (!_disposed)
        {
            _trayIcon.ToolTipText = tooltip;
        }
    }

    public void ShowNotification(string title, string message, BalloonIcon icon = BalloonIcon.Info)
    {
        if (!_disposed)
        {
            try
            {
                var notifyIcon = icon switch
                {
                    BalloonIcon.Info => NotificationIcon.Info,
                    BalloonIcon.Warning => NotificationIcon.Warning,
                    BalloonIcon.Error => NotificationIcon.Error,
                    _ => NotificationIcon.None
                };

                _trayIcon.ShowNotification(title, message, notifyIcon);
            }
            catch
            {
                // Fallback if balloon notification is not supported
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _trayIcon?.Dispose();
    }
}