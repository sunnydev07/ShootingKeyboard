using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Implements system-wide low-level keyboard hook using SetWindowsHookEx (WH_KEYBOARD_LL)
/// on a dedicated message-pump thread to achieve minimal latency without blocking the OS callback.
/// </summary>
public sealed class KeyboardHookService : IKeyboardHook
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_QUIT = 0x0012;

    private const uint LLKHF_EXTENDED = 0x01;
    private const uint LLKHF_INJECTED = 0x10;
    private const uint LLKHF_ALTDOWN = 0x20;
    private const uint LLKHF_UP = 0x80;

    private readonly object _syncLock = new();
    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc;
    private Thread? _hookThread;
    private uint _hookThreadId;
    private ManualResetEventSlim? _readyEvent;
    private volatile bool _isRunning;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<KeyPressedEventArgs>? KeyPressed;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public void Start()
    {
        lock (_syncLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isRunning)
                return;

            _readyEvent = new ManualResetEventSlim(false);

            _hookThread = new Thread(HookThreadMain)
            {
                IsBackground = true,
                Name = "ShootingKeyboard-KeyboardHookThread",
                Priority = ThreadPriority.Highest
            };

            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();

            // Wait up to 3 seconds for hook thread to install the hook
            if (!_readyEvent.Wait(TimeSpan.FromSeconds(3)))
            {
                Stop();
                throw new TimeoutException("Timed out while initializing the Windows keyboard hook.");
            }

            if (_hookHandle == IntPtr.Zero)
            {
                Stop();
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to install low-level keyboard hook. Error code: {errorCode}");
            }

            _isRunning = true;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_syncLock)
        {
            if (!_isRunning && _hookThread == null)
                return;

            _isRunning = false;

            if (_hookThreadId != 0)
            {
                PostThreadMessage(_hookThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }

            if (_hookThread != null)
            {
                if (!_hookThread.Join(TimeSpan.FromSeconds(2)))
                {
                    // Thread didn't terminate cleanly
                    try
                    {
                        _hookThread.Interrupt();
                    }
                    catch
                    {
                        // Ignore interruption errors
                    }
                }
                _hookThread = null;
            }

            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            _hookThreadId = 0;
            _readyEvent?.Dispose();
            _readyEvent = null;
        }
    }

    private void HookThreadMain()
    {
        try
        {
            _hookThreadId = GetCurrentThreadId();
            _hookProc = HookCallback;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            var hMod = GetModuleHandle(curModule?.ModuleName);

            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, hMod, 0);

            _readyEvent?.Set();

            if (_hookHandle == IntPtr.Zero)
            {
                return;
            }

            // Message pump required for WH_KEYBOARD_LL
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        catch
        {
            _readyEvent?.Set();
        }
        finally
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msgType = (int)wParam;
            var isKeyDown = msgType is WM_KEYDOWN or WM_SYSKEYDOWN;
            var isKeyUp = msgType is WM_KEYUP or WM_SYSKEYUP;

            if (isKeyDown || isKeyUp)
            {
                var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var isExtended = (hookStruct.flags & LLKHF_EXTENDED) != 0;
                var isInjected = (hookStruct.flags & LLKHF_INJECTED) != 0;

                var keyEvent = new KeyEvent(
                    (int)hookStruct.vkCode,
                    isKeyDown,
                    isExtended,
                    (int)hookStruct.scanCode,
                    hookStruct.time,
                    isInjected);

                // Dispatch asynchronously so the native hook callback returns instantly
                ThreadPool.UnsafeQueueUserWorkItem(state =>
                {
                    if (state is KeyEvent ev)
                    {
                        RaiseKeyPressed(ev);
                    }
                }, keyEvent);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void RaiseKeyPressed(KeyEvent keyEvent)
    {
        try
        {
            KeyPressed?.Invoke(this, new KeyPressedEventArgs(keyEvent));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[KeyboardHookService] Error in KeyPressed handler: {ex.Message}");
        }
    }

    /// <summary>
    /// For testing purposes: allows directly simulating an intercepted key event.
    /// </summary>
    public void SimulateKeyEvent(KeyEvent keyEvent)
    {
        RaiseKeyPressed(keyEvent);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
    }

    #region Win32 P/Invoke

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    #endregion
}