using System;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Event arguments for keyboard press events raised by IKeyboardHook.
/// </summary>
public sealed class KeyPressedEventArgs : EventArgs
{
    public KeyEvent KeyEvent { get; }

    public int KeyCode => KeyEvent.KeyCode;
    public bool IsPressed => KeyEvent.IsPressed;
    public bool IsExtended => KeyEvent.IsExtended;
    public int ScanCode => KeyEvent.ScanCode;
    public uint Timestamp => KeyEvent.Timestamp;
    public bool IsInjected => KeyEvent.IsInjected;

    public KeyPressedEventArgs(KeyEvent keyEvent)
    {
        KeyEvent = keyEvent ?? throw new ArgumentNullException(nameof(keyEvent));
    }

    public KeyPressedEventArgs(
        int keyCode,
        bool isPressed,
        bool isExtended = false,
        int scanCode = 0,
        uint timestamp = 0,
        bool isInjected = false)
        : this(new KeyEvent(keyCode, isPressed, isExtended, scanCode, timestamp, isInjected))
    {
    }
}

/// <summary>
/// Contract for low-level keyboard hook services.
/// </summary>
public interface IKeyboardHook : IDisposable
{
    /// <summary>
    /// Raised when a key press or release is intercepted.
    /// </summary>
    event EventHandler<KeyPressedEventArgs>? KeyPressed;

    /// <summary>
    /// Indicates whether the keyboard hook is actively running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the system-wide keyboard hook on a dedicated background thread with message loop.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the keyboard hook and unhooks from Windows.
    /// </summary>
    void Stop();
}