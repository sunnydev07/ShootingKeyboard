using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShootingKeyboard.Models;

public sealed class PlaybackFilterConfig
{
    [JsonPropertyName("ignoreKeyRepeats")]
    public bool IgnoreKeyRepeats { get; set; } = true;

    [JsonPropertyName("globalCooldownMs")]
    public int GlobalCooldownMs { get; set; } = 20;

    [JsonPropertyName("groupCooldownMs")]
    public Dictionary<string, int> GroupCooldownMs { get; set; } = new();

    [JsonPropertyName("keyCooldownMs")]
    public Dictionary<int, int> KeyCooldownMs { get; set; } = new();
}
