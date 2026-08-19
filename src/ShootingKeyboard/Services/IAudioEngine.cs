namespace ShootingKeyboard.Services;

/// <summary>
/// Interface for low-latency audio playback engine
/// </summary>
public interface IAudioEngine : IDisposable
{
    /// <summary>
    /// Preloads a sound file into memory for instant playback
    /// </summary>
    /// <param name="soundId">Unique identifier for the sound</param>
    /// <param name="filePath">Path to the audio file</param>
    /// <returns>True if loaded successfully</returns>
    bool LoadSound(string soundId, string filePath);

    /// <summary>
    /// Preloads multiple sounds from a directory
    /// </summary>
    /// <param name="basePath">Base directory containing sound files</param>
    /// <param name="soundEntries">List of sound entries with ID and filename</param>
    void LoadSoundPack(string basePath, IEnumerable<(string SoundId, string FileName, float Volume)> soundEntries);

    /// <summary>
    /// Plays a sound by ID
    /// </summary>
    /// <param name="soundId">Sound identifier</param>
    /// <param name="volume">Volume multiplier (0.0 - 1.0)</param>
    void Play(string soundId, float volume = 1.0f);

    /// <summary>
    /// Plays a sound with pitch modification (for combo variants)
    /// </summary>
    /// <param name="soundId">Sound identifier</param>
    /// <param name="volume">Volume multiplier</param>
    /// <param name="pitch">Pitch multiplier (1.0 = normal, >1.0 = higher, <1.0 = lower)</param>
    void PlayWithPitch(string soundId, float volume, float pitch);

    /// <summary>
    /// Sets the master volume
    /// </summary>
    void SetMasterVolume(float volume);

    /// <summary>
    /// Gets the current master volume
    /// </summary>
    float GetMasterVolume();

    /// <summary>
    /// Mutes or unmutes all audio
    /// </summary>
    void SetMuted(bool muted);

    /// <summary>
    /// Checks if a sound is loaded
    /// </summary>
    bool IsSoundLoaded(string soundId);

    /// <summary>
    /// Gets all loaded sound IDs
    /// </summary>
    IReadOnlyCollection<string> GetLoadedSoundIds();

    /// <summary>
    /// Unloads a specific sound
    /// </summary>
    void UnloadSound(string soundId);

    /// <summary>
    /// Unloads all sounds
    /// </summary>
    void UnloadAllSounds();

    /// <summary>
    /// Gets available audio output devices
    /// </summary>
    IReadOnlyList<Models.AudioDeviceInfo> GetOutputDevices();

    /// <summary>
    /// ID of the current output device, or null for default
    /// </summary>
    string? CurrentOutputDeviceId { get; }

    /// <summary>
    /// Sets the output device. Pass null or empty for default device. Returns true if device was applied.
    /// </summary>
    bool SetOutputDevice(string? deviceId);
}