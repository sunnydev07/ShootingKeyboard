using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface IProfileImportExportService
{
    void ExportProfile(AppProfile profile, string filePath);
    AppProfile ImportProfile(string filePath);
}
