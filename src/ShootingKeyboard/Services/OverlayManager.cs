using System;
using System.Windows;
using ShootingKeyboard.Overlay;

namespace ShootingKeyboard.Services;

/// <summary>
/// Manages on-screen overlay effects and combo rendering
/// </summary>
public sealed class OverlayManager : IOverlayManager
{
    private OverlayWindow? _overlayWindow;

    public bool IsVisible => _overlayWindow?.IsVisible ?? false;
    public bool IsEnabled { get; set; } = true;

    private void EnsureWindow()
    {
        if (_overlayWindow == null && Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlayWindow = new OverlayWindow();
            });
        }
    }

    public void ShowKeyPressEffect(Point screenPosition)
    {
        if (!IsEnabled) return;

        EnsureWindow();
        if (_overlayWindow != null)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                _overlayWindow.ShowEffect(screenPosition);
            });
        }
    }

    public void UpdateComboDisplay(int comboCount, int tier)
    {
        if (!IsEnabled) return;

        EnsureWindow();
        if (_overlayWindow != null)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                _overlayWindow.UpdateCombo(comboCount, tier);
            });
        }
    }

    public void Show()
    {
        EnsureWindow();
        if (_overlayWindow != null)
        {
            Application.Current?.Dispatcher.Invoke(() => _overlayWindow.Show());
        }
    }

    public void Hide()
    {
        if (_overlayWindow != null)
        {
            Application.Current?.Dispatcher.Invoke(() => _overlayWindow.Hide());
        }
    }
}
