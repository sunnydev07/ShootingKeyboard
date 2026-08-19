using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface ISoundPackImportExportService
{
    string InstallFromZip(string zipFilePath, string userPacksRoot);
    void ExportToZip(SoundPack pack, string zipFilePath);
}
