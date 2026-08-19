using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface IKeyPressFilter
{
    bool ShouldProcess(KeyEvent keyEvent, AppConfig config);
    void Reset();
}
