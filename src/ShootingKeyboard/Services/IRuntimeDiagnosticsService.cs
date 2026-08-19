using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface IRuntimeDiagnosticsService
{
    void RecordKeyEvent(KeyEvent keyEvent);
    void RecordResolvedSound(int keyCode, string? soundId);
    void RecordPlayback(string soundId, bool played, string reason);
    RuntimeDiagnosticsSnapshot CreateSnapshot(
        AppConfig config,
        IKeyboardHook keyboardHook,
        IAudioEngine audioEngine,
        ISoundPackManager soundPackManager,
        string configPath);
}
