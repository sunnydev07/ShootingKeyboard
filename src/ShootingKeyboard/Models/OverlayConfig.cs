using System.Text.Json.Serialization;

namespace ShootingKeyboard.Models;

public sealed class OverlayConfig
{
    [JsonPropertyName("showRipple")]
    public bool ShowRipple { get; set; } = true;

    [JsonPropertyName("showCombo")]
    public bool ShowCombo { get; set; } = true;

    [JsonPropertyName("rippleColor")]
    public string RippleColor { get; set; } = "#FFA500";

    [JsonPropertyName("comboPosition")]
    public string ComboPosition { get; set; } = "TopCenter";

    [JsonPropertyName("scale")]
    public double Scale { get; set; } = 1.0;
}
