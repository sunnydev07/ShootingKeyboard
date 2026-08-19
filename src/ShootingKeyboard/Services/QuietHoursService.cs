using System;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class QuietHoursService : IQuietHoursService
{
    public bool IsQuietNow(QuietHoursConfig config, DateTimeOffset now)
    {
        if (config == null || !config.Enabled)
        {
            return false;
        }

        var time = now.TimeOfDay;

        if (config.Start < config.End)
        {
            return time >= config.Start && time < config.End;
        }
        else if (config.Start > config.End)
        {
            return time >= config.Start || time < config.End;
        }
        else
        {
            return true;
        }
    }
}
