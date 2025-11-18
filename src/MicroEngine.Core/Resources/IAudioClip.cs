namespace MicroEngine.Core.Resources;

/// <summary>
/// Audio clip resource (sound effect or music).
/// </summary>
public interface IAudioClip : IResource
{
    /// <summary>
    /// Gets the duration in seconds.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Gets the number of audio channels (1=mono, 2=stereo).
    /// </summary>
    int Channels { get; }

    /// <summary>
    /// Gets whether this is a streaming audio (for music).
    /// </summary>
    bool IsStreaming { get; }
}
