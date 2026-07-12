using Zaya.Primitives;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Represents a region that captures the entire client area of a window.
/// </summary>
public sealed record FullScreenWindowRegion : ICaptureRegion
{
    /// <summary>
    /// Gets the desired pixel format for the captured frame.
    /// Default is <see cref="PixelFormat.Bgra32"/>.
    /// </summary>
    public PixelFormat PixelFormat { get; init; } = PixelFormat.Bgra32;

    /// <summary>
    /// Handle to the target window.
    /// </summary>
    public required nint WindowHandle { get; init; }
}