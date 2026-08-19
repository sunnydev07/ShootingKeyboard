using System.Collections.Generic;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface IProfileManager
{
    IReadOnlyList<AppProfile> GetProfiles(AppConfig config);
    AppProfile GetActiveProfile(AppConfig config);
    AppProfile CreateProfile(AppConfig config, string name);
    bool DeleteProfile(AppConfig config, string profileId);
    bool SetActiveProfile(AppConfig config, string profileId);
    void CopyRootSettingsToActiveProfile(AppConfig config);
    void ApplyActiveProfileToRootSettings(AppConfig config);
}
