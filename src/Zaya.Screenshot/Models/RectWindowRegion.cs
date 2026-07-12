using System.Drawing;

using Zaya.Primitives;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Represents a rectangular region within a specific window.
/// </summary>
public sealed record RectWindowRegion : ICaptureRegion
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

    /// <summary>
    /// The rectangular area within the window to capture, in client coordinates.
    /// </summary>
    public required Rectangle Rectangle { get; init; }
}