using Zaya.Screenshot.Models;

namespace Zaya.Screenshot.Services;

/// <summary>
/// Service for creating screen capture sessions targeting a specific window or monitor region.
/// </summary>
public interface ICaptureService : IDisposable
{
    /// <summary>
    /// Gets a unique identifier for this capture engine (e.g., "graphics-capture").
    /// Used for profile serialization and engine lookup.
    /// </summary>
    string EngineId { get; }

    /// <summary>
    /// Gets whether this capture engine is available on the current system.
    /// A lightweight check that does not initialize any resources.
    /// Returns <c>false</c> when the required platform APIs are not present.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Creates a new capture session for the specified region.
    /// </summary>
    /// <param name="region">The region to capture (window or monitor, full-screen or rect).</param>
    /// <param name="cancellationToken">Token to cancel the session creation.</param>
    /// <returns>An active capture session ready to produce frames.</returns>
    Task<ICaptureSession> CreateSessionAsync(ICaptureRegion region, CancellationToken cancellationToken = default);
}