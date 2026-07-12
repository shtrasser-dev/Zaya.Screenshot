using System.Drawing;
using Zaya.Screenshot.Impl.Windows.Services.Impl;
using Zaya.Screenshot.Models;

namespace Zaya.Screenshot.Tests.Integration;

public class CaptureServiceIntegrationTests : IAsyncDisposable
{
    private readonly CaptureService _captureService = new();

    [Fact]
    public async Task CaptureFullScreen_ReturnsNonBlackFrame()
    {
        var region = new FullScreenDesktopRegion();

        using var session = await _captureService.CreateSessionAsync(region);
        using var frame = await session.CaptureAsync();

        Assert.NotNull(frame);
        Assert.True(frame!.Width > 0);
        Assert.True(frame!.Height > 0);
        AssertNonBlack(frame);
    }

    [Fact]
    public async Task CaptureRectRegion_ReturnsCorrectSizeAndNonBlack()
    {
        var rect = new Rectangle(100, 100, 200, 150);
        var region = new RectDesktopRegion
        {
            DisplayIndex = 0,
            Rectangle = rect
        };

        using var session = await _captureService.CreateSessionAsync(region);
        using var frame = await session.CaptureAsync();

        Assert.NotNull(frame);
        Assert.Equal(rect.Width, frame!.Width);
        Assert.Equal(rect.Height, frame!.Height);
        AssertNonBlack(frame);
    }

    private static void AssertNonBlack(ICapturedFrame frame)
    {
        var pixelData = frame.GetPixelData();

        for (int i = 0; i < pixelData.Length; i++)
        {
            if (pixelData[i] != 0)
                return;
        }

        Assert.Fail("Captured frame is completely black.");
    }

    public async ValueTask DisposeAsync()
    {
        _captureService.Dispose();
        await ValueTask.CompletedTask;
    }
}
