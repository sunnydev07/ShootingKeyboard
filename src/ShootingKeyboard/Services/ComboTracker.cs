using System;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ShootingKeyboard.Services;

/// <summary>
/// Tracks typing combo with configurable time window and tier thresholds
/// </summary>
public sealed class ComboTracker : IComboTracker
{
    private readonly Timer _comboTimer;
    private int _comboCount = 0;
    private int _currentTier = 0;
    private int _comboWindowMs = 400;
    private readonly object _lock = new();

    // Tier thresholds (combo count needed to reach each tier)
    private readonly int[] _tierThresholds = { 5, 10, 20, 40 };

    public event EventHandler<int>? ComboChanged;
    public event EventHandler<int>? TierChanged;

    public int ComboCount => _comboCount;

    public int CurrentTier => _currentTier;

    public int ComboWindowMs
    {
        get => _comboWindowMs;
        set
        {
            _comboWindowMs = Math.Clamp(value, 50, 2000);
            _comboTimer.Interval = _comboWindowMs;
        }
    }

    public ComboTracker()
    {
        _comboTimer = new Timer(_comboWindowMs)
        {
            AutoReset = false
        };
        _comboTimer.Elapsed += OnComboTimeout;
    }

    public void RegisterKeyPress()
    {
        lock (_lock)
        {
            _comboCount++;
            _comboTimer.Stop();
            _comboTimer.Start();

            // Check tier changes
            var newTier = CalculateTier(_comboCount);
            if (newTier != _currentTier)
            {
                _currentTier = newTier;
                TierChanged?.Invoke(this, _currentTier);
            }

            ComboChanged?.Invoke(this, _comboCount);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _comboTimer.Stop();
            _comboCount = 0;
            _currentTier = 0;
            ComboChanged?.Invoke(this, 0);
            TierChanged?.Invoke(this, 0);
        }
    }

    private void OnComboTimeout(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_comboCount > 0)
            {
                _comboCount = 0;
                _currentTier = 0;
                ComboChanged?.Invoke(this, 0);
                TierChanged?.Invoke(this, 0);
            }
        }
    }

    private int CalculateTier(int count)
    {
        for (int i = 0; i < _tierThresholds.Length; i++)
        {
            if (count < _tierThresholds[i])
                return i;
        }
        return _tierThresholds.Length;
    }

    public void Dispose()
    {
        _comboTimer?.Dispose();
    }
}