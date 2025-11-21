using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Audio;

/// <summary>
/// Interface for playing music (streaming audio).
/// </summary>
public interface IMusicPlayer
{
    /// <summary>
    /// Starts playing streaming music.
    /// </summary>
    void PlayMusic(IAudioClip music);

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    void StopMusic(IAudioClip music);

    /// <summary>
    /// Pauses the currently playing music.
    /// </summary>
    void PauseMusic(IAudioClip music);

    /// <summary>
    /// Resumes the paused music.
    /// </summary>
    void ResumeMusic(IAudioClip music);

    /// <summary>
    /// Updates music streaming (must be called every frame for streaming audio).
    /// </summary>
    void UpdateMusic(IAudioClip music);

    /// <summary>
    /// Checks if music is currently playing.
    /// </summary>
    bool IsMusicPlaying(IAudioClip music);

    /// <summary>
    /// Sets the volume for a specific music track (0.0 to 1.0).
    /// </summary>
    void SetMusicVolume(IAudioClip music, float volume);
}
