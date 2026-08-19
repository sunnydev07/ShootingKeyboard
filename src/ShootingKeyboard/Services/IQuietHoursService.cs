using System;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface IQuietHoursService
{
    bool IsQuietNow(QuietHoursConfig config, DateTimeOffset now);
}
