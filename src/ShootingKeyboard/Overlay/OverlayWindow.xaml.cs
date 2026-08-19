using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ShootingKeyboard.Overlay;

/// <summary>
/// Transparent, click-through overlay window for visual feedback
/// </summary>
public partial class OverlayWindow : Window
{
    private bool _isComboDisplayActive = false;
    private Models.OverlayConfig _config = new();

    public bool IsComboDisplayActive => _isComboDisplayActive;

    public OverlayWindow()
    {
        InitializeComponent();
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        // Ensure window is click-through
        Loaded += (s, e) => MakeClickThrough();
    }

    private void MakeClickThrough()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
    }

    public void ApplyConfig(Models.OverlayConfig config)
    {
        _config = config ?? new Models.OverlayConfig();

        try
        {
            if (ColorConverter.ConvertFromString(_config.RippleColor) is Color col)
            {
                RippleEllipse.Stroke = new SolidColorBrush(col);
            }
        }
        catch
        {
            RippleEllipse.Stroke = Brushes.Orange;
        }

        RootGrid.LayoutTransform = new ScaleTransform(_config.Scale, _config.Scale);

        switch (_config.ComboPosition)
        {
            case "TopLeft":
                ComboBorder.HorizontalAlignment = HorizontalAlignment.Left;
                ComboBorder.VerticalAlignment = VerticalAlignment.Top;
                ComboBorder.Margin = new Thickness(50, 50, 0, 0);
                break;
            case "TopRight":
                ComboBorder.HorizontalAlignment = HorizontalAlignment.Right;
                ComboBorder.VerticalAlignment = VerticalAlignment.Top;
                ComboBorder.Margin = new Thickness(0, 50, 50, 0);
                break;
            case "BottomCenter":
                ComboBorder.HorizontalAlignment = HorizontalAlignment.Center;
                ComboBorder.VerticalAlignment = VerticalAlignment.Bottom;
                ComboBorder.Margin = new Thickness(0, 0, 0, 50);
                break;
            case "TopCenter":
            default:
                ComboBorder.HorizontalAlignment = HorizontalAlignment.Center;
                ComboBorder.VerticalAlignment = VerticalAlignment.Top;
                ComboBorder.Margin = new Thickness(0, 50, 0, 0);
                break;
        }
    }

    public void ShowEffect(Point screenPosition)
    {
        if (!_config.ShowRipple) return;

        // Position the ripple at the screen coordinates
        RippleTranslate.X = screenPosition.X - 30; // Center on cursor (half width)
        RippleTranslate.Y = screenPosition.Y - 30; // Center on cursor (half height)

        RippleEllipse.Opacity = 1;
        RippleScale.ScaleX = 0;
        RippleScale.ScaleY = 0;

        var scaleAnim = new DoubleAnimation(0, 1.5, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        RippleScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
        RippleScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
        RippleEllipse.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

        if (!IsVisible)
        {
            Show();
        }
    }

    public void HideEffect()
    {
        if (!_isComboDisplayActive)
        {
            Hide();
        }
    }

    public void UpdateCombo(int comboCount, int tier)
    {
        if (!_config.ShowCombo || comboCount <= 0)
        {
            ComboBorder.Visibility = Visibility.Collapsed;
            _isComboDisplayActive = false;
            HideEffect();
            return;
        }

        _isComboDisplayActive = true;
        ComboBorder.Visibility = Visibility.Visible;
        ComboCountText.Text = comboCount.ToString();

        // Update tier text
        TierText.Text = tier switch
        {
            1 => "ENHANCED",
            2 => "EXTREME",
            3 => "OVERLOAD",
            4 => "MAXIMUM",
            _ => ""
        };

        // Color code by tier
        ComboCountText.Foreground = tier switch
        {
            1 => Brushes.Orange,
            2 => Brushes.Red,
            3 => Brushes.Magenta,
            4 => Brushes.Gold,
            _ => Brushes.White
        };

        // Pulse animation on new combo
        var pulseAnim = new DoubleAnimation(1, 1.2, TimeSpan.FromMilliseconds(100))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        ComboCountText.BeginAnimation(TextBlock.FontSizeProperty, pulseAnim);

        if (!IsVisible)
        {
            Show();
        }
    }

    private static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}