using System;
using System.Text.Json.Serialization;

namespace ShootingKeyboard.Models;

public sealed class QuietHoursConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("start")]
    public TimeSpan Start { get; set; } = new(22, 0, 0);

    [JsonPropertyName("end")]
    public TimeSpan End { get; set; } = new(8, 0, 0);
}
