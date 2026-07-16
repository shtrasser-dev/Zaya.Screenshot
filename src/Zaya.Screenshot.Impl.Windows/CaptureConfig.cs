namespace Zaya.Screenshot.Impl.Windows;

/// <summary>
/// Typed configuration for <see cref="Services.Impl.CaptureService"/>.
/// Converts to the dictionary format expected by <c>InitializeAsync</c>.
/// Empty for now — no engine-specific settings yet.
/// </summary>
public class CaptureConfig
{
    /// <summary>
    /// Converts the typed configuration to the dictionary format accepted by <c>InitializeAsync</c>.
    /// </summary>
    public Dictionary<string, object?> ToDictionary() => [];
}
