using System.Buffers;
using System.Runtime.InteropServices.WindowsRuntime;
using Zaya.Primitives;
using Zaya.Screenshot.Models;
using IBuffer = Windows.Storage.Streams.IBuffer;

namespace Zaya.Screenshot.Impl.Windows.Services.Impl;

internal sealed class FrameCropperService
{
    private static void CopyGray8(
        IBuffer source,
        byte[] dst,
        int srcWidth,
        int cropX,
        int cropY,
        int dstWidth,
        int dstHeight)
    {
        int srcStride = srcWidth * 4;
        byte[] srcRow = ArrayPool<byte>.Shared.Rent(srcStride);

        try
        {
            for (int row = 0; row < dstHeight; row++)
            {
                int srcOffset = (cropY + row) * srcStride + cropX * 4;
                source.CopyTo((uint)srcOffset, srcRow, 0, srcStride);

                int dstRowBase = row * dstWidth;

                int col = 0;
                for (; col + 3 < dstWidth; col += 4)
                {
                    int s = col * 4;
                    int d = dstRowBase + col;

                    dst[d] = GrayLum(srcRow[s + 2], srcRow[s + 1], srcRow[s]);
                    dst[d + 1] = GrayLum(srcRow[s + 6], srcRow[s + 5], srcRow[s + 4]);
                    dst[d + 2] = GrayLum(srcRow[s + 10], srcRow[s + 9], srcRow[s + 8]);
                    dst[d + 3] = GrayLum(srcRow[s + 14], srcRow[s + 13], srcRow[s + 12]);
                }

                for (; col < dstWidth; col++)
                {
                    int s = col * 4;
                    dst[dstRowBase + col] = GrayLum(srcRow[s + 2], srcRow[s + 1], srcRow[s]);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(srcRow);
        }
    }

    private static byte GrayLum(byte r, byte g, byte b)
    {
        return (byte)((r * 76 + g * 150 + b * 29) >> 8);
    }

    public byte[] Crop(
        IBuffer sourceBuffer,
        int srcWidth,
        int srcHeight,
        ICaptureRegion region,
        out int dstWidth,
        out int dstHeight)
    {
        int cropX = 0, cropY = 0, cropW = srcWidth, cropH = srcHeight;

        if (region is RectWindowRegion rw)
        {
            cropX = rw.Rectangle.X;
            cropY = rw.Rectangle.Y;
            cropW = rw.Rectangle.Width;
            cropH = rw.Rectangle.Height;
        }
        else if (region is RectDesktopRegion rd)
        {
            cropX = rd.Rectangle.X;
            cropY = rd.Rectangle.Y;
            cropW = rd.Rectangle.Width;
            cropH = rd.Rectangle.Height;
        }

        dstWidth = cropW;
        dstHeight = cropH;

        if (cropX < 0 || cropY < 0 || cropW <= 0 || cropH <= 0)
            throw new ArgumentException($"Crop rectangle ({cropX},{cropY},{cropW},{cropH}) is invalid.");

        if (cropX + cropW > srcWidth || cropY + cropH > srcHeight)
            throw new ArgumentException(
                $"Crop rectangle ({cropX},{cropY},{cropW},{cropH}) exceeds source bounds ({srcWidth}x{srcHeight}).");

        int outputBpp = region.PixelFormat.BytesPerPixel;
        int dstTotalBytes = dstWidth * dstHeight * outputBpp;

        byte[] result = ArrayPool<byte>.Shared.Rent(dstTotalBytes);

        bool fullScreen = cropX == 0 && cropY == 0 && cropW == srcWidth && cropH == srcHeight;

        if (outputBpp == 4 && fullScreen)
        {
            sourceBuffer.CopyTo(0, result, 0, dstTotalBytes);
        }
        else if (outputBpp == 4)
        {
            CopyBgra(sourceBuffer, result, srcWidth, cropX, cropY, dstWidth, dstHeight);
        }
        else if (outputBpp == 3)
        {
            CopyConvert(sourceBuffer, result, region.PixelFormat, srcWidth, cropX, cropY, dstWidth, dstHeight);
        }
        else if (outputBpp == 1)
        {
            CopyGray8(sourceBuffer, result, srcWidth, cropX, cropY, dstWidth, dstHeight);
        }
        else
        {
            ArrayPool<byte>.Shared.Return(result);
            throw new NotSupportedException(
                $"Pixel format '{region.PixelFormat.Name}' (Bpp={outputBpp}) is not supported.");
        }

        return result;
    }

    private static void CopyBgra(
        IBuffer source,
        byte[] dst,
        int srcWidth,
        int cropX,
        int cropY,
        int dstWidth,
        int dstHeight)
    {
        int srcStride = srcWidth * 4;
        int dstStride = dstWidth * 4;

        for (int row = 0; row < dstHeight; row++)
        {
            int srcOffset = (cropY + row) * srcStride + cropX * 4;
            int dstOffset = row * dstStride;
            source.CopyTo((uint)srcOffset, dst, dstOffset, dstStride);
        }
    }

    private static void CopyConvert(
        IBuffer source,
        byte[] dst,
        PixelFormat format,
        int srcWidth,
        int cropX,
        int cropY,
        int dstWidth,
        int dstHeight)
    {
        int srcStride = srcWidth * 4;
        int dstStride = dstWidth * 3;
        byte[] srcRow = ArrayPool<byte>.Shared.Rent(srcStride);

        bool swapRgb = format.Name == "Rgb24";

        try
        {
            for (int row = 0; row < dstHeight; row++)
            {
                int srcOffset = (cropY + row) * srcStride + cropX * 4;
                source.CopyTo((uint)srcOffset, srcRow, 0, srcStride);

                int dstRowBase = row * dstStride;

                int col = 0;
                for (; col + 3 < dstWidth; col += 4)
                {
                    int d = dstRowBase + col * 3;
                    int s0 = col * 4;
                    int s1 = s0 + 4;
                    int s2 = s0 + 8;
                    int s3 = s0 + 12;

                    if (swapRgb)
                    {
                        dst[d] = srcRow[s0 + 2]; dst[d + 1] = srcRow[s0 + 1]; dst[d + 2] = srcRow[s0];
                        dst[d + 3] = srcRow[s1 + 2]; dst[d + 4] = srcRow[s1 + 1]; dst[d + 5] = srcRow[s1];
                        dst[d + 6] = srcRow[s2 + 2]; dst[d + 7] = srcRow[s2 + 1]; dst[d + 8] = srcRow[s2];
                        dst[d + 9] = srcRow[s3 + 2]; dst[d + 10] = srcRow[s3 + 1]; dst[d + 11] = srcRow[s3];
                    }
                    else
                    {
                        dst[d] = srcRow[s0]; dst[d + 1] = srcRow[s0 + 1]; dst[d + 2] = srcRow[s0 + 2];
                        dst[d + 3] = srcRow[s1]; dst[d + 4] = srcRow[s1 + 1]; dst[d + 5] = srcRow[s1 + 2];
                        dst[d + 6] = srcRow[s2]; dst[d + 7] = srcRow[s2 + 1]; dst[d + 8] = srcRow[s2 + 2];
                        dst[d + 9] = srcRow[s3]; dst[d + 10] = srcRow[s3 + 1]; dst[d + 11] = srcRow[s3 + 2];
                    }
                }

                for (; col < dstWidth; col++)
                {
                    int d = dstRowBase + col * 3;
                    int s = col * 4;

                    if (swapRgb)
                    {
                        dst[d] = srcRow[s + 2];
                        dst[d + 1] = srcRow[s + 1];
                        dst[d + 2] = srcRow[s];
                    }
                    else
                    {
                        dst[d] = srcRow[s];
                        dst[d + 1] = srcRow[s + 1];
                        dst[d + 2] = srcRow[s + 2];
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(srcRow);
        }
    }
}
