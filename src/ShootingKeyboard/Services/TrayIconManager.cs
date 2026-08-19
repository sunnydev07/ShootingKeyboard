using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using ShootingKeyboard.Models;

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

    public event EventHandler<string>? ProfileSelected;
    public event EventHandler<string>? SoundPackSelected;
    public event EventHandler<float>? VolumeSelected;
    public event EventHandler? ToggleOverlayRequested;

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

        RebuildQuickMenus(Array.Empty<AppProfile>(), string.Empty, Array.Empty<SoundPack>(), string.Empty);

        // Left click shows settings
        _trayIcon.TrayLeftMouseDown += (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RebuildQuickMenus(IReadOnlyList<AppProfile> profiles, string activeProfileId, IReadOnlyList<SoundPack> packs, string activePackId)
    {
        if (_disposed) return;

        var contextMenu = new ContextMenu();

        var showSettingsItem = new MenuItem { Header = "Settings & Dashboard" };
        showSettingsItem.Click += (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(showSettingsItem);

        var showDiagnosticsItem = new MenuItem { Header = "Diagnostics" };
        showDiagnosticsItem.Click += (s, e) => DiagnosticsRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(showDiagnosticsItem);

        contextMenu.Items.Add(new Separator());

        // Profiles Submenu
        if (profiles != null && profiles.Count > 0)
        {
            var profilesMenu = new MenuItem { Header = "Profiles" };
            foreach (var profile in profiles)
            {
                var item = new MenuItem
                {
                    Header = profile.Name,
                    IsChecked = profile.Id.Equals(activeProfileId, StringComparison.OrdinalIgnoreCase)
                };
                var id = profile.Id;
                item.Click += (s, e) => ProfileSelected?.Invoke(this, id);
                profilesMenu.Items.Add(item);
            }
            contextMenu.Items.Add(profilesMenu);
        }

        // Sound Packs Submenu
        if (packs != null && packs.Count > 0)
        {
            var packsMenu = new MenuItem { Header = "Sound Packs" };
            foreach (var pack in packs)
            {
                var item = new MenuItem
                {
                    Header = pack.Name,
                    IsChecked = pack.Id.Equals(activePackId, StringComparison.OrdinalIgnoreCase)
                };
                var id = pack.Id;
                item.Click += (s, e) => SoundPackSelected?.Invoke(this, id);
                packsMenu.Items.Add(item);
            }
            contextMenu.Items.Add(packsMenu);
        }

        // Volume Submenu
        var volumeMenu = new MenuItem { Header = "Volume" };
        var volumeLevels = new[] { ("25%", 0.25f), ("50%", 0.50f), ("75%", 0.75f), ("100%", 1.0f) };
        foreach (var (label, vol) in volumeLevels)
        {
            var item = new MenuItem { Header = label };
            item.Click += (s, e) => VolumeSelected?.Invoke(this, vol);
            volumeMenu.Items.Add(item);
        }
        contextMenu.Items.Add(volumeMenu);

        // Overlay Toggle
        var overlayItem = new MenuItem { Header = "Toggle Overlay" };
        overlayItem.Click += (s, e) => ToggleOverlayRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(overlayItem);

        contextMenu.Items.Add(new Separator());

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