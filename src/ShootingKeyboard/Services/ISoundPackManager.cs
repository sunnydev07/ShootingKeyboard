using System.Collections.Generic;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Interface for managing sound packs
/// </summary>
public interface ISoundPackManager
{
    /// <summary>
    /// Fired when available packs change
    /// </summary>
    event EventHandler? PacksChanged;

    /// <summary>
    /// Gets all available sound packs
    /// </summary>
    IReadOnlyList<SoundPack> GetPacks();

    /// <summary>
    /// Gets a pack by ID
    /// </summary>
    SoundPack? GetPack(string packId);

    /// <summary>
    /// Gets the currently active pack
    /// </summary>
    SoundPack? ActivePack { get; }

    /// <summary>
    /// Sets the active pack by ID
    /// </summary>
    bool SetActivePack(string packId);

    /// <summary>
    /// Refreshes pack discovery
    /// </summary>
    void Refresh();
}