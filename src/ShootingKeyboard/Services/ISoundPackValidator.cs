using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public interface ISoundPackValidator
{
    SoundPackValidationResult Validate(SoundPack pack);
    SoundPackValidationResult ValidatePackFolder(string packDirectory);
}
