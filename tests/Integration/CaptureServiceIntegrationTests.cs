using System.Drawing;
using Zaya.Primitives;
using Zaya.Screenshot.Impl.Windows.Services.Impl;
using Zaya.Screenshot.Models;

namespace Zaya.Screenshot.Tests.Integration;

public class CaptureServiceIntegrationTests : IAsyncDisposable
{
    private readonly CaptureService _captureService = new();

    [Fact]
    public void DisplayName_ReturnsNonEmpty()
    {
        var name = _captureService.DisplayName.GetValue(System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(name));
    }

    [Fact]
    public void Settings_ReturnsEmptyList()
    {
        var settings = _captureService.Settings;
        Assert.NotNull(settings);
        Assert.Empty(settings);
    }

    [Fact]
    public async Task CreateSession_WithoutInitialize_Throws()
    {
        var region = new FullScreenDesktopRegion();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _captureService.CreateSessionAsync(region, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CaptureFullScreen_ReturnsNonBlackFrame()
    {
        await _captureService.InitializeAsync(null, TestContext.Current.CancellationToken);

        var region = new FullScreenDesktopRegion();

        using var session = await _captureService.CreateSessionAsync(region, TestContext.Current.CancellationToken);
        using var frame = await session.CaptureAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(frame);
        Assert.True(frame!.Width > 0);
        Assert.True(frame!.Height > 0);
        AssertNonBlack(frame);
    }

    [Fact]
    public async Task CaptureRectRegion_ReturnsCorrectSizeAndNonBlack()
    {
        await _captureService.InitializeAsync(null, TestContext.Current.CancellationToken);

        var rect = new Rectangle(100, 100, 200, 150);
        var region = new RectDesktopRegion
        {
            DisplayIndex = 0,
            Rectangle = rect
        };

        using var session = await _captureService.CreateSessionAsync(region, TestContext.Current.CancellationToken);
        using var frame = await session.CaptureAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(frame);
        Assert.Equal(rect.Width, frame!.Width);
        Assert.Equal(rect.Height, frame!.Height);
        AssertNonBlack(frame);
    }

    private static void AssertNonBlack(IRawImage frame)
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
