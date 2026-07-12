using Zaya.Primitives;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Represents a captured frame with direct pixel data access.
/// The frame owns the underlying memory and releases it when disposed.
/// </summary>
public interface ICapturedFrame : IDisposable
{
    /// <summary>
    /// Gets the width of the frame in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the frame in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the number of bytes per row (stride).
    /// May be larger than Width * BytesPerPixel due to memory alignment.
    /// </summary>
    int Stride { get; }

    /// <summary>
    /// Gets the pixel format of the frame.
    /// Provides information about pixel layout and bytes per pixel.
    /// </summary>
    PixelFormat Format { get; }

    /// <summary>
    /// Gets the raw pixel data as a read-only span.
    /// Direct access to the underlying buffer without additional allocations.
    /// </summary>
    ReadOnlySpan<byte> GetPixelData();

    /// <summary>
    /// Creates a copy of the pixel data as a byte array.
    /// Useful when the data needs to outlive the frame instance.
    /// </summary>
    /// <returns>A new byte array containing a copy of the pixel data.</returns>
    byte[] ToByteArray();
}