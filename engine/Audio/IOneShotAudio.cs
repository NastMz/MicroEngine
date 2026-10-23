namespace MicroEngine.Audio;

/// <summary>Optional configured one-shot requests; no rendering, codec or genre dependency.</summary>
public interface IOneShotAudio : IDisposable
{
    void Play(string cue);
    void StopAll();
}
