using MicroEngine.Core.Math;

namespace MicroEngine.Core.Audio;

/// <summary>
/// Interface for the audio device lifecycle and global settings.
/// </summary>
public interface IAudioDevice
{
    /// <summary>
    /// Initializes the audio device.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Shuts down the audio device.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Sets the master volume (0.0 to 1.0).
    /// </summary>
    void SetMasterVolume(float volume);

    /// <summary>
    /// Gets the master volume (0.0 to 1.0).
    /// </summary>
    float GetMasterVolume();

    /// <summary>
    /// Sets the listener position for spatial audio calculations.
    /// </summary>
    /// <param name="position">The listener's position.</param>
    void SetListenerPosition(Vector2 position);
}
