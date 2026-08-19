using System.IO;
using System.Text.Json;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Handles loading and saving application configuration with atomic writes
/// </summary>
public sealed class ConfigService : IConfigService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppConfig? _cachedConfig;

    public string ConfigPath => _configPath;

    public ConfigService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _configPath = customPath;
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appData, "ShootingKeyboard");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public AppConfig Load()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? AppConfig.CreateDefault();
            }
            else
            {
                _cachedConfig = AppConfig.CreateDefault();
            }
        }
        catch (Exception)
        {
            _cachedConfig = AppConfig.CreateDefault();
        }

        _cachedConfig.Validate();
        return _cachedConfig;
    }

    /// <inheritdoc />
    public void Save(AppConfig config)
    {
        var activeProfile = config.Profiles?.FirstOrDefault(p => p.Id.Equals(config.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
                            ?? config.Profiles?.FirstOrDefault();
        if (activeProfile != null)
        {
            activeProfile.MasterVolume = config.MasterVolume;
            activeProfile.IsMuted = config.IsMuted;
            activeProfile.IsEnabled = config.IsEnabled;
            activeProfile.ActivePackId = config.ActivePackId;
            activeProfile.OverlayEnabled = config.OverlayEnabled;
            activeProfile.PerformanceMode = config.PerformanceMode;
            activeProfile.ComboWindowMs = config.ComboWindowMs;
            activeProfile.KeyBindings = new Dictionary<int, string>(config.KeyBindings ?? new());
            activeProfile.GroupBindings = new Dictionary<string, string>(config.GroupBindings ?? new());
            activeProfile.GroupVolumeOverrides = new Dictionary<string, float>(config.GroupVolumeOverrides ?? new());
            activeProfile.KeyVolumeOverrides = new Dictionary<int, float>(config.KeyVolumeOverrides ?? new());
            activeProfile.PlaybackFilter = new PlaybackFilterConfig
            {
                IgnoreKeyRepeats = config.PlaybackFilter?.IgnoreKeyRepeats ?? true,
                GlobalCooldownMs = config.PlaybackFilter?.GlobalCooldownMs ?? 20,
                GroupCooldownMs = new Dictionary<string, int>(config.PlaybackFilter?.GroupCooldownMs ?? new()),
                KeyCooldownMs = new Dictionary<int, int>(config.PlaybackFilter?.KeyCooldownMs ?? new())
            };
        }

        config.Validate();
        _cachedConfig = config;

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        var tempPath = _configPath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _configPath, true);
        }
        catch (Exception)
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    /// <inheritdoc />
    public void ResetToDefaults()
    {
        var defaults = AppConfig.CreateDefault();
        Save(defaults);
    }
}

/// <summary>
/// Interface for configuration service to enable testing
/// </summary>
public interface IConfigService
{
    string ConfigPath { get; }
    AppConfig Load();
    void Save(AppConfig config);
    void ResetToDefaults();
}