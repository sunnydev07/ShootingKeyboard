using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class SelectedSoundClip
{
    public string AudioId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public float Volume { get; set; } = 1.0f;
}

public interface ISoundVariantSelector
{
    string GetVariantAudioId(string soundId, int variantIndex);
    SelectedSoundClip SelectClip(SoundEntry soundEntry);
}
