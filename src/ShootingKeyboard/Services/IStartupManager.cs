namespace ShootingKeyboard.Services;

/// <summary>
/// Manages Windows startup registration via registry (HKCU\Software\Microsoft\Windows\CurrentVersion\Run).
/// </summary>
public interface IStartupManager
{
    bool IsStartupEnabled();
    void SetStartupEnabled(bool enable);
}
