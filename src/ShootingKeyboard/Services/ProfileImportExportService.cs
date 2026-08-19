using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ShootingKeyboard.Models;

namespace ShootingKeyboard.Services;

public sealed class ProfileImportExportService : IProfileImportExportService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void ExportProfile(AppProfile profile, string filePath)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    public AppProfile ImportProfile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException($"Profile file not found: {filePath}");
        }

        string json;
        try
        {
            json = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to read profile file: {ex.Message}", ex);
        }

        AppProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<AppProfile>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Invalid profile JSON structure: {ex.Message}", ex);
        }

        if (profile == null || string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new InvalidDataException("Profile must contain a non-empty Id and Name.");
        }

        // Validate and clamp values
        profile.MasterVolume = Math.Clamp(profile.MasterVolume, 0f, 1f);
        profile.ComboWindowMs = Math.Clamp(profile.ComboWindowMs, 50, 2000);
        profile.KeyBindings ??= new();
        profile.GroupBindings ??= new();
        profile.GroupVolumeOverrides ??= new();
        profile.KeyVolumeOverrides ??= new();

        var invalidGroups = profile.GroupVolumeOverrides.Keys
            .Where(k => !KeyGroups.All.Contains(k))
            .ToList();
        foreach (var g in invalidGroups)
        {
            profile.GroupVolumeOverrides.Remove(g);
        }
        foreach (var (k, v) in profile.GroupVolumeOverrides.ToList())
        {
            profile.GroupVolumeOverrides[k] = Math.Clamp(v, 0f, 1f);
        }

        foreach (var (k, v) in profile.KeyVolumeOverrides.ToList())
        {
            profile.KeyVolumeOverrides[k] = Math.Clamp(v, 0f, 1f);
        }

        if (profile.PlaybackFilter != null)
        {
            profile.PlaybackFilter.GlobalCooldownMs = Math.Clamp(profile.PlaybackFilter.GlobalCooldownMs, 0, 1000);
            profile.PlaybackFilter.GroupCooldownMs ??= new();
            profile.PlaybackFilter.KeyCooldownMs ??= new();

            var invalidFilterGroups = profile.PlaybackFilter.GroupCooldownMs.Keys
                .Where(k => !KeyGroups.All.Contains(k))
                .ToList();
            foreach (var k in invalidFilterGroups)
            {
                profile.PlaybackFilter.GroupCooldownMs.Remove(k);
            }

            foreach (var (k, v) in profile.PlaybackFilter.GroupCooldownMs.ToList())
            {
                profile.PlaybackFilter.GroupCooldownMs[k] = Math.Clamp(v, 0, 5000);
            }

            foreach (var (k, v) in profile.PlaybackFilter.KeyCooldownMs.ToList())
            {
                profile.PlaybackFilter.KeyCooldownMs[k] = Math.Clamp(v, 0, 5000);
            }
        }
        else
        {
            profile.PlaybackFilter = new PlaybackFilterConfig();
        }

        return profile;
    }
}
