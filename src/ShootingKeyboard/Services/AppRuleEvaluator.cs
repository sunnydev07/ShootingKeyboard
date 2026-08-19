using System;
using System.Linq;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class AppRuleEvaluator : IAppRuleEvaluator
{
    public AppRuleDecision Evaluate(ForegroundAppInfo? appInfo, AppConfig config)
    {
        if (appInfo == null || string.IsNullOrWhiteSpace(appInfo.ProcessName) || config?.AppRules == null || config.AppRules.Count == 0)
        {
            return new AppRuleDecision { ShouldPlay = true, Reason = "no-rule" };
        }

        var processName = NormalizeProcessName(appInfo.ProcessName);

        var matchingRule = config.AppRules.FirstOrDefault(r =>
            !string.IsNullOrWhiteSpace(r.ProcessName) &&
            NormalizeProcessName(r.ProcessName).Equals(processName, StringComparison.OrdinalIgnoreCase));

        if (matchingRule == null)
        {
            return new AppRuleDecision { ShouldPlay = true, Reason = "no-rule" };
        }

        if (matchingRule.DisableSounds)
        {
            return new AppRuleDecision
            {
                ShouldPlay = false,
                Reason = "disabled-by-app-rule"
            };
        }

        if (matchingRule.MuteOnly)
        {
            return new AppRuleDecision
            {
                ShouldPlay = false,
                Reason = "muted-by-app-rule"
            };
        }

        return new AppRuleDecision
        {
            ShouldPlay = true,
            ProfileIdOverride = matchingRule.ProfileIdOverride,
            SoundPackIdOverride = matchingRule.SoundPackIdOverride,
            Reason = "matched-rule"
        };
    }

    private static string NormalizeProcessName(string processName)
    {
        var trimmed = processName.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^4];
        }
        return trimmed;
    }
}
