using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class ForegroundAppService : IForegroundAppService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public ForegroundAppInfo? GetForegroundApp()
    {
        try
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0) return null;

            using var process = Process.GetProcessById((int)processId);
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);

            return new ForegroundAppInfo
            {
                ProcessName = process.ProcessName,
                MainWindowTitle = sb.ToString()
            };
        }
        catch
        {
            return null;
        }
    }
}
