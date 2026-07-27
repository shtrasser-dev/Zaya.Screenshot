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
    private readonly Action? _onDisposed;
    private static readonly TimeSpan DefaultFrameTimeout = TimeSpan.FromSeconds(5);

    private bool _disposed;

    public ICaptureRegion Region => _region;

    public CaptureSession(
        Direct3DConverterService converter,
        ICaptureRegion region,
        Direct3D11CaptureFramePool framePool,
        GraphicsCaptureSession session,
        Action? onDisposed = null)
    {
        _converter = converter;
        _region = region;
        _framePool = framePool;
        _session = session;
        _onDisposed = onDisposed;
    }

    public async Task<IRawImage?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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

    private async Task<IRawImage> ConvertFrameAsync(Direct3D11CaptureFrame frame)
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
        var tcs = new TaskCompletionSource<Direct3D11CaptureFrame?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Direct3D11CaptureFrame? result = null;

        void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            try
            {
                var frame = sender.TryGetNextFrame();
                if (frame is null)
                    return;

                if (!tcs.TrySetResult(frame))
                    frame.Dispose();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        _framePool.FrameArrived += OnFrameArrived;
        try
        {
            var existingFrame = _framePool.TryGetNextFrame();
            if (existingFrame != null)
            {
                result = existingFrame;
                return result;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(DefaultFrameTimeout);
            try
            {
                result = await tcs.Task.WaitAsync(cts.Token);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw new CaptureFrameTimeoutException();
            }
        }
        finally
        {
            _framePool.FrameArrived -= OnFrameArrived;

            // Dispose a frame that completed on the TCS but was not returned to the caller.
            if (tcs.Task.IsCompletedSuccessfully)
            {
                var leftover = tcs.Task.Result;
                if (leftover is not null && !ReferenceEquals(leftover, result))
                    leftover.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _session.Dispose(); } catch { }
        try { _framePool.Dispose(); } catch { }
        _onDisposed?.Invoke();
    }
}
