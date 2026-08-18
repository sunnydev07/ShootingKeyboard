namespace ShootingKeyboard.Services;

/// <summary>
/// Tracks typing combos for escalating sound effects
/// </summary>
public interface IComboTracker : IDisposable
{
    /// <summary>
    /// Fired when combo count changes
    /// </summary>
    event EventHandler<int>? ComboChanged;

    /// <summary>
    /// Fired when combo tier changes (for sound variant selection)
    /// </summary>
    event EventHandler<int>? TierChanged;

    /// <summary>
    /// Current combo count
    /// </summary>
    int ComboCount { get; }

    /// <summary>
    /// Current combo tier (0 = base, 1 = enhanced, 2 = extreme, etc.)
    /// </summary>
    int CurrentTier { get; }

    /// <summary>
    /// Window in milliseconds to maintain combo
    /// </summary>
    int ComboWindowMs { get; set; }

    /// <summary>
    /// Registers a key press and updates combo state
    /// </summary>
    void RegisterKeyPress();

    /// <summary>
    /// Resets combo to zero
    /// </summary>
    void Reset();
}