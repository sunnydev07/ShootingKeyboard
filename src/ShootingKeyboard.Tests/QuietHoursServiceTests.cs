using System;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class QuietHoursServiceTests
{
    private readonly QuietHoursService _service = new();

    [Fact]
    public void IsQuietNow_WhenDisabled_ReturnsFalse()
    {
        var config = new QuietHoursConfig
        {
            Enabled = false,
            Start = new TimeSpan(22, 0, 0),
            End = new TimeSpan(8, 0, 0)
        };

        var now = new DateTimeOffset(2026, 8, 19, 23, 0, 0, TimeSpan.Zero);
        Assert.False(_service.IsQuietNow(config, now));
    }

    [Fact]
    public void IsQuietNow_SameDayRange_EvaluatesCorrectly()
    {
        var config = new QuietHoursConfig
        {
            Enabled = true,
            Start = new TimeSpan(9, 0, 0),
            End = new TimeSpan(17, 0, 0)
        };

        var insideTime = new DateTimeOffset(2026, 8, 19, 14, 30, 0, TimeSpan.Zero);
        var beforeTime = new DateTimeOffset(2026, 8, 19, 8, 59, 0, TimeSpan.Zero);
        var afterTime = new DateTimeOffset(2026, 8, 19, 17, 0, 0, TimeSpan.Zero);

        Assert.True(_service.IsQuietNow(config, insideTime));
        Assert.False(_service.IsQuietNow(config, beforeTime));
        Assert.False(_service.IsQuietNow(config, afterTime));
    }

    [Fact]
    public void IsQuietNow_AcrossMidnightRange_EvaluatesCorrectly()
    {
        var config = new QuietHoursConfig
        {
            Enabled = true,
            Start = new TimeSpan(22, 0, 0),
            End = new TimeSpan(8, 0, 0)
        };

        var lateNight = new DateTimeOffset(2026, 8, 19, 23, 15, 0, TimeSpan.Zero);
        var earlyMorning = new DateTimeOffset(2026, 8, 20, 6, 45, 0, TimeSpan.Zero);
        var daytime = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

        Assert.True(_service.IsQuietNow(config, lateNight));
        Assert.True(_service.IsQuietNow(config, earlyMorning));
        Assert.False(_service.IsQuietNow(config, daytime));
    }

    [Fact]
    public void IsQuietNow_AllDayRange_ReturnsTrue()
    {
        var config = new QuietHoursConfig
        {
            Enabled = true,
            Start = new TimeSpan(8, 0, 0),
            End = new TimeSpan(8, 0, 0)
        };

        var now = new DateTimeOffset(2026, 8, 19, 15, 0, 0, TimeSpan.Zero);
        Assert.True(_service.IsQuietNow(config, now));
    }
}
