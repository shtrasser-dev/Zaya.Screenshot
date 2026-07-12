using Zaya.Primitives;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Represents a region that captures the entire desktop of a specific display.
/// </summary>
public sealed record FullScreenDesktopRegion : ICaptureRegion
{
    /// <summary>
    /// Gets the desired pixel format for the captured frame.
    /// Default is <see cref="PixelFormat.Bgra32"/>.
    /// </summary>
    public PixelFormat PixelFormat { get; init; } = PixelFormat.Bgra32;

    /// <summary>
    /// Gets the index of the target display. Default is 0 (primary display).
    /// </summary>
    public int DisplayIndex { get; init; } = 0;
}