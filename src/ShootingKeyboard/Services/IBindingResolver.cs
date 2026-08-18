using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Service responsible for resolving a virtual key code to a sound ID
/// using explicit key maps, group bindings, pack defaults, and fallback chains.
/// </summary>
public interface IBindingResolver
{
    /// <summary>
    /// Resolves the sound ID to play for a given virtual key code.
    /// </summary>
    string? ResolveSound(int virtualKeyCode, SoundPack? activePack, AppConfig config);
}
