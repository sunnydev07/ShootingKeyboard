namespace ShootingKeyboard.Models;

/// <summary>
/// Represents a keyboard event translated from Windows low-level keyboard hook.
/// </summary>
public sealed class KeyEvent
{
    /// <summary>
    /// Virtual key code (e.g., VK_A = 0x41, VK_SPACE = 0x20)
    /// </summary>
    public int KeyCode { get; }

    /// <summary>
    /// True if the key was pressed (down), false if released (up)
    /// </summary>
    public bool IsPressed { get; }

    /// <summary>
    /// Indicates whether the key is an extended key (e.g. right Alt/Ctrl)
    /// </summary>
    public bool IsExtended { get; }

    /// <summary>
    /// Hardware scan code for the key
    /// </summary>
    public int ScanCode { get; }

    /// <summary>
    /// Timestamp of the message from Windows hook
    /// </summary>
    public uint Timestamp { get; }

    /// <summary>
    /// Indicates whether the event was injected (simulated)
    /// </summary>
    public bool IsInjected { get; }

    public KeyEvent(
        int keyCode,
        bool isPressed,
        bool isExtended = false,
        int scanCode = 0,
        uint timestamp = 0,
        bool isInjected = false)
    {
        KeyCode = keyCode;
        IsPressed = isPressed;
        IsExtended = isExtended;
        ScanCode = scanCode;
        Timestamp = timestamp;
        IsInjected = isInjected;
    }

    public override string ToString() =>
        $"KeyEvent(KeyCode=0x{KeyCode:X2} ({KeyCode}), IsPressed={IsPressed}, IsInjected={IsInjected})";
}
