using Zaya.Primitives;
using Zaya.Screenshot.Models;

namespace Zaya.Screenshot.Services;

/// <summary>
/// Service for creating screen capture sessions targeting a specific window or monitor region.
/// Pass engine settings directly to <see cref="CreateSessionAsync(ICaptureRegion, IReadOnlyDictionary{string, object}, CancellationToken)"/>.
/// </summary>
public interface ICaptureService : IDisposable
{
    /// <summary>
    /// Gets a unique identifier for this capture engine (e.g., "graphics-capture").
    /// Used for profile serialization and engine lookup.
    /// </summary>
    string EngineId { get; }

    /// <summary>
    /// Gets the UI display name for this engine (localized).
    /// </summary>
    LocalizedString DisplayName { get; }

    /// <summary>
    /// Gets the UI description for this engine (localized).
    /// </summary>
    LocalizedString Description { get; }

    /// <summary>
    /// Gets whether this capture service is available on the current system.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the list of engine-specific settings that can be configured via UI.
    /// </summary>
    IReadOnlyList<SettingDescriptor> Settings { get; }

    /// <summary>
    /// Creates a new capture session for the specified region with default engine settings.
    /// </summary>
    /// <param name="region">The region to capture (window or monitor, full-screen or rect).</param>
    /// <param name="cancellationToken">Token to cancel the session creation.</param>
    /// <returns>An active capture session ready to produce frames.</returns>
    Task<ICaptureSession> CreateSessionAsync(ICaptureRegion region, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new capture session for the specified region with the given engine settings.
    /// </summary>
    /// <param name="region">The region to capture (window or monitor, full-screen or rect).</param>
    /// <param name="engineSettings">Engine-specific settings dictionary, or <c>null</c> for defaults.</param>
    /// <param name="cancellationToken">Token to cancel the session creation.</param>
    /// <returns>An active capture session ready to produce frames.</returns>
    Task<ICaptureSession> CreateSessionAsync(ICaptureRegion region, IReadOnlyDictionary<string, object> engineSettings, CancellationToken cancellationToken = default);
}
