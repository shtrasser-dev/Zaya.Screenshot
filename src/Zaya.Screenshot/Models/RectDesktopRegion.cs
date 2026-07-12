using System.Drawing;

using Zaya.Primitives;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Represents a rectangular region on a specific display.
/// </summary>
public sealed record RectDesktopRegion : ICaptureRegion
{
    /// <summary>
    /// Gets the desired pixel format for the captured frame.
    /// Default is <see cref="PixelFormat.Bgra32"/>.
    /// </summary>
    public PixelFormat PixelFormat { get; init; } = PixelFormat.Bgra32;

    /// <summary>
    /// Index of the target display (0 = primary display).
    /// </summary>
    public required int DisplayIndex { get; init; }

    /// <summary>
    /// The rectangular area on the display to capture, in screen coordinates.
    /// </summary>
    public required Rectangle Rectangle { get; init; }
}