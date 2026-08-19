using System.Collections.Generic;
using System.Linq;

namespace ShootingKeyboard.Models;

public enum SoundPackValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed class SoundPackValidationIssue
{
    public SoundPackValidationSeverity Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SoundId { get; set; }
    public string? FilePath { get; set; }
}

public sealed class SoundPackValidationResult
{
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public List<SoundPackValidationIssue> Issues { get; set; } = new();
    public bool IsValid => Issues.All(i => i.Severity != SoundPackValidationSeverity.Error);
}
