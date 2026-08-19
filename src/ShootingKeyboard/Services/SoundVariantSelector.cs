using System;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class SoundVariantSelector : ISoundVariantSelector
{
    private readonly Func<int, int> _randomPicker;

    public SoundVariantSelector(Func<int, int>? randomPicker = null)
    {
        _randomPicker = randomPicker ?? (max => Random.Shared.Next(max));
    }

    public string GetVariantAudioId(string soundId, int variantIndex)
    {
        return $"{soundId}::variant::{variantIndex}";
    }

    public SelectedSoundClip SelectClip(SoundEntry soundEntry)
    {
        if (soundEntry == null)
        {
            return new SelectedSoundClip();
        }

        var variants = soundEntry.Variants;
        if (variants == null || variants.Count == 0)
        {
            return new SelectedSoundClip
            {
                AudioId = soundEntry.Id,
                FilePath = soundEntry.File,
                Volume = soundEntry.Volume
            };
        }

        // Total choices: 1 base + N variants
        int totalChoices = 1 + variants.Count;
        int choice = _randomPicker(totalChoices);

        if (choice <= 0 || choice > variants.Count)
        {
            return new SelectedSoundClip
            {
                AudioId = soundEntry.Id,
                FilePath = soundEntry.File,
                Volume = soundEntry.Volume
            };
        }

        int variantIndex = choice - 1;
        return new SelectedSoundClip
        {
            AudioId = GetVariantAudioId(soundEntry.Id, variantIndex),
            FilePath = variants[variantIndex],
            Volume = soundEntry.Volume
        };
    }
}
