using System.Collections.Generic;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class AppRuleEvaluatorTests
{
    private readonly AppRuleEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_NoRules_ReturnsShouldPlayTrueWithNoRuleReason()
    {
        var config = new AppConfig();
        var app = new ForegroundAppInfo { ProcessName = "notepad", MainWindowTitle = "Untitled - Notepad" };

        var decision = _evaluator.Evaluate(app, config);

        Assert.True(decision.ShouldPlay);
        Assert.Equal("no-rule", decision.Reason);
    }

    [Fact]
    public void Evaluate_NullAppInfo_ReturnsShouldPlayTrueWithNoRuleReason()
    {
        var config = new AppConfig();

        var decision = _evaluator.Evaluate(null, config);

        Assert.True(decision.ShouldPlay);
        Assert.Equal("no-rule", decision.Reason);
    }

    [Fact]
    public void Evaluate_DisableSoundsRule_ReturnsShouldPlayFalse()
    {
        var config = new AppConfig
        {
            AppRules = new List<AppRule>
            {
                new AppRule { ProcessName = "notepad.exe", DisableSounds = true }
            }
        };
        var app = new ForegroundAppInfo { ProcessName = "Notepad", MainWindowTitle = "Notes.txt" };

        var decision = _evaluator.Evaluate(app, config);

        Assert.False(decision.ShouldPlay);
        Assert.Equal("disabled-by-app-rule", decision.Reason);
    }

    [Fact]
    public void Evaluate_MuteOnlyRule_ReturnsShouldPlayFalse()
    {
        var config = new AppConfig
        {
            AppRules = new List<AppRule>
            {
                new AppRule { ProcessName = "teams", MuteOnly = true }
            }
        };
        var app = new ForegroundAppInfo { ProcessName = "teams.exe", MainWindowTitle = "Microsoft Teams" };

        var decision = _evaluator.Evaluate(app, config);

        Assert.False(decision.ShouldPlay);
        Assert.Equal("muted-by-app-rule", decision.Reason);
    }

    [Fact]
    public void Evaluate_SoundPackOverrideRule_ReturnsShouldPlayTrueWithOverride()
    {
        var config = new AppConfig
        {
            AppRules = new List<AppRule>
            {
                new AppRule { ProcessName = "Code", SoundPackIdOverride = "scifi" }
            }
        };
        var app = new ForegroundAppInfo { ProcessName = "code.exe", MainWindowTitle = "Visual Studio Code" };

        var decision = _evaluator.Evaluate(app, config);

        Assert.True(decision.ShouldPlay);
        Assert.Equal("scifi", decision.SoundPackIdOverride);
        Assert.Equal("matched-rule", decision.Reason);
    }

    [Fact]
    public void Evaluate_ProfileOverrideRule_ReturnsShouldPlayTrueWithOverride()
    {
        var config = new AppConfig
        {
            AppRules = new List<AppRule>
            {
                new AppRule { ProcessName = "game.exe", ProfileIdOverride = "gaming_profile" }
            }
        };
        var app = new ForegroundAppInfo { ProcessName = "game", MainWindowTitle = "Awesome Game" };

        var decision = _evaluator.Evaluate(app, config);

        Assert.True(decision.ShouldPlay);
        Assert.Equal("gaming_profile", decision.ProfileIdOverride);
        Assert.Equal("matched-rule", decision.Reason);
    }
}
