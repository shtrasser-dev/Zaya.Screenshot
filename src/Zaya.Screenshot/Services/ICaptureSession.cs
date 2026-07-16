using Zaya.Primitives;
using Zaya.Screenshot.Models;

namespace Zaya.Screenshot.Services;

/// <summary>
/// Represents an active capture session.
/// </summary>
public interface ICaptureSession : IDisposable
{
    /// <summary>
    /// Gets the region being captured.
    /// </summary>
    ICaptureRegion Region { get; }

    /// <summary>
    /// Captures the next available frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captured raw image, or null if no frame is available.</returns>
    Task<IRawImage?> CaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the capture session if paused.
    /// </summary>
    void Resume();

    /// <summary>
    /// Pauses the capture session.
    /// </summary>
    void Pause();
}
