using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface IAppRuleEvaluator
{
    AppRuleDecision Evaluate(ForegroundAppInfo? appInfo, AppConfig config);
}
