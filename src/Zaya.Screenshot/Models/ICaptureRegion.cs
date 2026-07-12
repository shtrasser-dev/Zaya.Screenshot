using Zaya.Primitives;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Represents a capture region with a specified output pixel format.
/// </summary>
public interface ICaptureRegion
{
    /// <summary>
    /// Gets the desired pixel format for the captured frame.
    /// </summary>
    PixelFormat PixelFormat { get; }
}