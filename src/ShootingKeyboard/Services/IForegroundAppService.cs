using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface IForegroundAppService
{
    ForegroundAppInfo? GetForegroundApp();
}
