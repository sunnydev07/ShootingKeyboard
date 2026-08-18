namespace ShootingKeyboard.Models;

/// <summary>
/// Model representing an explicit key-to-sound binding mapping.
/// </summary>
public sealed class KeyBindingEntry
{
    public int KeyCode { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string SoundId { get; set; } = string.Empty;

    public KeyBindingEntry() { }

    public KeyBindingEntry(int keyCode, string keyName, string soundId)
    {
        KeyCode = keyCode;
        KeyName = keyName;
        SoundId = soundId;
    }
}
