using Windows.Graphics.Capture;
using Zaya.Primitives;
using Zaya.Screenshot.Impl.Windows.Models;
using Zaya.Screenshot.Models;
using Zaya.Screenshot.Services;

namespace Zaya.Screenshot.Impl.Windows.Services.Impl;

/// <summary>
/// Implementation of <see cref="ICaptureSession"/>.
/// </summary>
internal sealed class CaptureSession : ICaptureSession
{
    private readonly Direct3DConverterService _converter;
    private readonly ICaptureRegion _region;
    private readonly Direct3D11CaptureFramePool _framePool;
    private readonly GraphicsCaptureSession _session;
    private static readonly TimeSpan DefaultFrameTimeout = TimeSpan.FromSeconds(5);

    private bool _disposed;
    private bool _paused;

    public ICaptureRegion Region => _region;

    public CaptureSession(
        Direct3DConverterService converter,
        ICaptureRegion region,
        Direct3D11CaptureFramePool framePool,
        GraphicsCaptureSession session)
    {
        _converter = converter;
        _region = region;
        _framePool = framePool;
        _session = session;
    }

    public async Task<ICapturedFrame?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CaptureSession));

        if (_paused)
            return null;

        var frame = await WaitForFrameAsync(cancellationToken);
        if (frame == null)
            return null;

        try
        {
            return await ConvertFrameAsync(frame);
        }
        finally
        {
            frame.Dispose();
        }
    }

    private async Task<ICapturedFrame> ConvertFrameAsync(Direct3D11CaptureFrame frame)
    {
        PixelFormat outputFormat = _region.PixelFormat;

        var (pixelData, width, height) = await _converter.ConvertSurfaceToByteArrayAsync(
            frame.Surface,
            _region);

        int bytesPerPixel = outputFormat.BytesPerPixel;
        int stride = width * bytesPerPixel;

        return new CapturedFrame(
            pixelData,
            width,
            height,
            stride,
            outputFormat);
    }

    private async Task<Direct3D11CaptureFrame?> WaitForFrameAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<Direct3D11CaptureFrame?>();

        void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            try
            {
                var frame = sender.TryGetNextFrame();
                if (frame != null)
                    tcs.TrySetResult(frame);
            }
            catch (Exception ex) { tcs.TrySetException(ex); }
        }

        _framePool.FrameArrived += OnFrameArrived;

        try
        {
            var existingFrame = _framePool.TryGetNextFrame();
            if (existingFrame != null)
                return existingFrame;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(DefaultFrameTimeout);
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (completedTask != tcs.Task)
                throw new TimeoutException("No frame received within timeout.");
            return await tcs.Task;
        }
        finally
        {
            _framePool.FrameArrived -= OnFrameArrived;
        }
    }

    public void Pause()
    {
        if (!_disposed)
            _paused = true;
    }

    public void Resume()
    {
        if (!_disposed)
            _paused = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _session?.Dispose(); } catch { }
        try { _framePool?.Dispose(); } catch { }
    }
}