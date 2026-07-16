using System.Buffers;
using Zaya.Primitives;

namespace Zaya.Screenshot.Impl.Windows.Models;

/// <summary>
/// Implementation of <see cref="IRawImage"/> with direct pixel data access.
/// Owns a byte array rented from <see cref="ArrayPool{Byte}.Shared"/>.
/// </summary>
public sealed class CapturedFrame : IRawImage
{
    private readonly byte[] _pixelData;
    private readonly bool _returnToPool;
    private bool _disposed;

    /// <summary>
    /// Gets the width of the frame in pixels.
    /// </summary>
    public int Width { get; }
    /// <summary>
    /// Gets the height of the frame in pixels.
    /// </summary>
    public int Height { get; }
    /// <summary>
    /// Gets the number of bytes per row (stride).
    /// May be larger than Width * BytesPerPixel due to memory alignment.
    /// </summary>
    public int Stride { get; }
    /// <summary>
    /// Gets the pixel format of the frame.
    /// </summary>
    public PixelFormat Format { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapturedFrame"/> class.
    /// </summary>
    /// <param name="pixelData">The raw pixel data buffer. Ownership transfers to this frame;
    /// the buffer is returned to <see cref="ArrayPool{Byte}.Shared"/> on dispose when <paramref name="returnToPool"/> is true.</param>
    /// <param name="width">The width of the frame in pixels.</param>
    /// <param name="height">The height of the frame in pixels.</param>
    /// <param name="stride">The number of bytes per row.</param>
    /// <param name="format">The pixel format of the data.</param>
    /// <param name="returnToPool">Whether to return the pixel data to the array pool on dispose.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pixelData"/> is null.</exception>
    public CapturedFrame(
        byte[] pixelData,
        int width,
        int height,
        int stride,
        PixelFormat format,
        bool returnToPool = true)
    {
        _pixelData = pixelData ?? throw new ArgumentNullException(nameof(pixelData));
        _returnToPool = returnToPool;
        Width = width;
        Height = height;
        Stride = stride;
        Format = format;
    }

    private int DataSize => Stride * Height;

    /// <summary>
    /// Gets the raw pixel data as a read-only span.
    /// Provides direct access to the underlying buffer without additional allocations.
    /// </summary>
    /// <returns>A read-only span over the valid pixel data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the frame has been disposed.</exception>
    public ReadOnlySpan<byte> GetPixelData()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _pixelData.AsSpan(0, DataSize);
    }

    /// <summary>
    /// Creates a copy of the pixel data as a byte array.
    /// Useful when the data needs to outlive the frame instance.
    /// </summary>
    /// <returns>A new byte array containing a copy of the pixel data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the frame has been disposed.</exception>
    public byte[] ToByteArray()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _pixelData.AsSpan(0, DataSize).ToArray();
    }

    /// <summary>
    /// Releases the pixel data buffer. If <c>returnToPool</c> was true,
    /// the buffer is returned to <see cref="ArrayPool{Byte}.Shared"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        if (_returnToPool)
            ArrayPool<byte>.Shared.Return(_pixelData);
        _disposed = true;
    }
}
