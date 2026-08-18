using System.Windows;

namespace ShootingKeyboard.Services;

/// <summary>
/// Manages on-screen overlay for visual feedback
/// </summary>
public interface IOverlayManager
{
    /// <summary>
    /// Shows a ripple/flash effect at the specified screen coordinates
    /// </summary>
    void ShowKeyPressEffect(Point screenPosition);

    /// <summary>
    /// Updates the combo display
    /// </summary>
    void UpdateComboDisplay(int comboCount, int tier);

    /// <summary>
    /// Shows the overlay
    /// </summary>
    void Show();

    /// <summary>
    /// Hides the overlay
    /// </summary>
    void Hide();

    /// <summary>
    /// Whether the overlay is currently visible
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Whether the overlay is enabled
    /// </summary>
    bool IsEnabled { get; set; }
}