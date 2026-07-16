using Zaya.Screenshot.Models;
using Zaya.Primitives;

namespace Zaya.Screenshot.Services;

/// <summary>
/// Service for creating screen capture sessions targeting a specific window or monitor region.
/// Call <see cref="InitializeAsync"/> before creating sessions.
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
    /// Gets the list of engine-specific settings that can be configured via UI.
    /// </summary>
    IReadOnlyList<SettingDescriptor> Settings { get; }

    /// <summary>
    /// Initializes the engine with the specified settings.
    /// Must be called before <see cref="CreateSessionAsync"/>.
    /// </summary>
    Task InitializeAsync(IReadOnlyDictionary<string, object?>? engineSettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether this capture engine is initialized and ready.
    /// Returns <c>true</c> only after successful <see cref="InitializeAsync"/>.
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
