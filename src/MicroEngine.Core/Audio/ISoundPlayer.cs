using MicroEngine.Core.Resources;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.Audio;

/// <summary>
/// Interface for playing sound effects (short, non-streaming audio).
/// </summary>
public interface ISoundPlayer
{
    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    void PlaySound(IAudioClip sound);

    /// <summary>
    /// Stops all instances of a sound effect.
    /// </summary>
    void StopSound(IAudioClip sound);

    /// <summary>
    /// Sets the volume for a specific sound effect (0.0 to 1.0).
    /// </summary>
    void SetSoundVolume(IAudioClip sound, float volume);

    /// <summary>
    /// Plays a sound effect at a specific position with distance attenuation.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="position">The position of the sound source.</param>
    /// <param name="maxDistance">Maximum distance for attenuation (sound is silent beyond this).</param>
    void PlaySoundAtPosition(IAudioClip sound, Vector2 position, float maxDistance);
}
