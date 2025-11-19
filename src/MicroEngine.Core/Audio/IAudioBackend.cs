using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Audio;

/// <summary>
/// Backend-agnostic audio playback interface.
/// </summary>
public interface IAudioBackend
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
    /// Plays a sound effect (non-streaming audio).
    /// </summary>
    void PlaySound(IAudioClip sound);

    /// <summary>
    /// Stops all instances of a sound effect.
    /// </summary>
    void StopSound(IAudioClip sound);

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
    /// Sets the volume for a specific sound effect (0.0 to 1.0).
    /// </summary>
    void SetSoundVolume(IAudioClip sound, float volume);

    /// <summary>
    /// Updates music streaming (must be called every frame for streaming audio).
    /// </summary>
    void UpdateMusic(IAudioClip music);

    /// <summary>
    /// Checks if music is currently playing.
    /// </summary>
    bool IsMusicPlaying(IAudioClip music);

    /// <summary>
    /// Sets the master volume (0.0 to 1.0).
    /// </summary>
    void SetMasterVolume(float volume);

    /// <summary>
    /// Gets the master volume (0.0 to 1.0).
    /// </summary>
    float GetMasterVolume();

    /// <summary>
    /// Sets the volume for a specific music track (0.0 to 1.0).
    /// </summary>
    void SetMusicVolume(IAudioClip music, float volume);
}
