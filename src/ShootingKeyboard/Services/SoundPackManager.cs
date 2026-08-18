using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

/// <summary>
/// Manages discovery and loading of sound packs from multiple locations
/// </summary>
public sealed class SoundPackManager : ISoundPackManager
{
    private readonly List<SoundPack> _packs = new();
    private string? _activePackId;

    public event EventHandler? PacksChanged;

    public IReadOnlyList<SoundPack> GetPacks() => _packs.AsReadOnly();

    public SoundPack? ActivePack => _activePackId != null ? GetPack(_activePackId) : null;

    public SoundPack? GetPack(string packId) => _packs.FirstOrDefault(p => p.Id.Equals(packId, StringComparison.OrdinalIgnoreCase));

    public bool SetActivePack(string packId)
    {
        var pack = GetPack(packId);
        if (pack == null)
            return false;

        _activePackId = packId;
        PacksChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Refresh()
    {
        _packs.Clear();

        // Ensure default sound packs exist in user AppData directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var userPacksPath = Path.Combine(appData, "ShootingKeyboard", "packs");
        BuiltInSoundPackFactory.EnsureDefaultPacks(userPacksPath);

        DiscoverPacks();

        if (string.IsNullOrEmpty(_activePackId) && _packs.Count > 0)
        {
            _activePackId = _packs[0].Id;
        }

        PacksChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DiscoverPacks()
    {
        var searchPaths = GetSearchPaths();

        foreach (var path in searchPaths)
        {
            if (!Directory.Exists(path))
                continue;

            try
            {
                var packDirs = Directory.GetDirectories(path);
                foreach (var packDir in packDirs)
                {
                    var packJson = Path.Combine(packDir, "pack.json");
                    if (File.Exists(packJson))
                    {
                        try
                        {
                            var json = File.ReadAllText(packJson);
                            var pack = JsonSerializer.Deserialize<SoundPack>(json, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            if (pack != null && !string.IsNullOrEmpty(pack.Id))
                            {
                                // Resolve relative file paths
                                foreach (var sound in pack.Sounds)
                                {
                                    if (!Path.IsPathRooted(sound.File))
                                    {
                                        sound.File = Path.Combine(packDir, sound.File);
                                    }
                                }

                                if (!_packs.Any(p => p.Id.Equals(pack.Id, StringComparison.OrdinalIgnoreCase)))
                                {
                                    _packs.Add(pack);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load pack from {packJson}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning pack directory {path}: {ex.Message}");
            }
        }
    }

    private IEnumerable<string> GetSearchPaths()
    {
        var appBase = AppDomain.CurrentDomain.BaseDirectory;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // 1. User packs in AppData
        yield return Path.Combine(appData, "ShootingKeyboard", "packs");

        // 2. Beside executable: sound-packs
        yield return Path.Combine(appBase, "sound-packs");

        // 3. Beside executable: Resources/DefaultSounds
        yield return Path.Combine(appBase, "Resources", "DefaultSounds");

        // 4. Relative parent levels for development / testing
        var current = appBase;
        for (int i = 0; i < 4; i++)
        {
            var parent = Directory.GetParent(current)?.FullName;
            if (string.IsNullOrEmpty(parent)) break;

            yield return Path.Combine(parent, "sound-packs");
            yield return Path.Combine(parent, "src", "ShootingKeyboard", "Resources", "DefaultSounds");
            current = parent;
        }
    }
}