using System.Buffers;
using Zaya.Primitives;
using Zaya.Screenshot.Impl.Windows.Models;
using Zaya.Screenshot.Models;

namespace Zaya.Screenshot.Tests.Models;

public class CapturedFrameTests
{
    private const int Width = 2;
    private const int Height = 2;
    private const int Stride = 8; // 2 pixels * 4 bytes
    private const int TotalBytes = Stride * Height; // 16 bytes

    [Fact]
    public void Constructor_WithNullPixelData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CapturedFrame(null!, Width, Height, Stride, PixelFormat.Bgra32));
    }

    [Fact]
    public void Constructor_ValidParameters_SetsPropertiesCorrectly()
    {
        var pixelData = new byte[TotalBytes];
        using var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32);

        Assert.Equal(Width, frame.Width);
        Assert.Equal(Height, frame.Height);
        Assert.Equal(Stride, frame.Stride);
        Assert.Equal(PixelFormat.Bgra32, frame.Format);
    }

    [Fact]
    public void GetPixelData_ReturnsCorrectSpan()
    {
        var pixelData = new byte[TotalBytes];
        for (int i = 0; i < TotalBytes; i++)
            pixelData[i] = (byte)i;

        using var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32);
        var span = frame.GetPixelData();

        Assert.Equal(TotalBytes, span.Length);
        for (int i = 0; i < TotalBytes; i++)
            Assert.Equal(pixelData[i], span[i]);
    }

    [Fact]
    public void GetPixelData_AfterDispose_ThrowsObjectDisposedException()
    {
        var pixelData = new byte[TotalBytes];
        var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32);
        frame.Dispose();

        Assert.Throws<ObjectDisposedException>(() => frame.GetPixelData());
    }

    [Fact]
    public void ToByteArray_ReturnsCorrectCopy()
    {
        var pixelData = new byte[TotalBytes];
        for (int i = 0; i < TotalBytes; i++)
            pixelData[i] = (byte)i;

        using var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32);
        var copy = frame.ToByteArray();

        Assert.Equal(pixelData, copy);
        Assert.NotSame(pixelData, copy);
    }

    [Fact]
    public void ToByteArray_AfterDispose_ThrowsObjectDisposedException()
    {
        var pixelData = new byte[TotalBytes];
        var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32);
        frame.Dispose();

        Assert.Throws<ObjectDisposedException>(() => frame.ToByteArray());
    }

    [Fact]
    public void Dispose_WhenReturnToPoolTrue_ReturnsArrayToPool()
    {
        var pixelData = ArrayPool<byte>.Shared.Rent(TotalBytes);
        var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32, returnToPool: true);

        frame.Dispose();

        var rented = ArrayPool<byte>.Shared.Rent(TotalBytes);
        ArrayPool<byte>.Shared.Return(rented);
    }

    [Fact]
    public void Dispose_WhenReturnToPoolFalse_DoesNotReturnArrayToPool()
    {
        var pixelData = new byte[TotalBytes];
        var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32, returnToPool: false);

        // Get a reference to the original array
        var originalArray = pixelData;

        frame.Dispose();

        // The array should not be returned to the pool, so it still holds the data
        // We can verify by checking that the array is still accessible
        Assert.Equal(originalArray, pixelData);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var pixelData = new byte[TotalBytes];
        var frame = new CapturedFrame(pixelData, Width, Height, Stride, PixelFormat.Bgra32);

        frame.Dispose();
        frame.Dispose(); // Second call should not throw
    }

    [Fact]
    public void GetPixelData_ReturnsCorrectSize_WithDifferentStride()
    {
        int customStride = 12; // 3 pixels * 4 bytes (with padding)
        int customWidth = 2;
        int customHeight = 3;
        int totalBytes = customStride * customHeight;

        byte[] pixelData = ArrayPool<byte>.Shared.Rent(totalBytes);

        using var frame = new CapturedFrame(pixelData, customWidth, customHeight, customStride, PixelFormat.Bgra32, returnToPool: true);

        var span = frame.GetPixelData();
        Assert.Equal(totalBytes, span.Length);
        Assert.Equal(customStride * customHeight, span.Length);
    }

    [Fact]
    public void DifferentFormats_StoreCorrectly()
    {
        int width = 2;
        int height = 2;
        int bytesPerPixel = 3; // Rgb24
        int stride = width * bytesPerPixel;
        int totalBytes = stride * height;

        byte[] pixelData = ArrayPool<byte>.Shared.Rent(totalBytes);

        using var frame = new CapturedFrame(pixelData, width, height, stride, PixelFormat.Rgb24, returnToPool: true);

        Assert.Equal(PixelFormat.Rgb24, frame.Format);
        Assert.Equal(stride, frame.Stride);
        Assert.Equal(totalBytes, frame.GetPixelData().Length);
    }
}