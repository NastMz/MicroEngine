using MicroEngine.Audio;
using System.Runtime.ExceptionServices;
namespace MicroEngine.Desktop;

public sealed class AudioDeviceUnavailableException : IOException
{
    public AudioDeviceUnavailableException() : base("Audio device is unavailable.") { }
}

/// <summary>Standalone owner-thread audio provider. No window or game-specific cue names.</summary>
public sealed class DesktopOneShotAudio : IOneShotAudio
{
    private static readonly AudioReservation ProcessReservation=new();
    private readonly IDesktopAudioApi _api;
    private readonly AudioReservation _reservation;
    private readonly Dictionary<string,object> _sounds=new(StringComparer.Ordinal);
    private readonly int _owner=Environment.CurrentManagedThreadId;
    private bool _disposed;
    public DesktopOneShotAudio(IReadOnlyDictionary<string,byte[]> waves) : this(waves,new RaylibAudioApi(),ProcessReservation) { }
    internal DesktopOneShotAudio(IReadOnlyDictionary<string,byte[]> waves,IDesktopAudioApi api,AudioReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(waves);ArgumentNullException.ThrowIfNull(api);ArgumentNullException.ThrowIfNull(reservation);
        if(waves.Count is <1 or >16)throw new ArgumentException("Configure one through sixteen cues.",nameof(waves));
        var copies=new Dictionary<string,byte[]>(StringComparer.Ordinal);
        foreach(var pair in waves){ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);copies.Add(pair.Key,WaveFile.CopyAndValidate(pair.Value));}
        (_api,_reservation)=(api,reservation);_reservation.Take();
        try
        {
            _api.Init();if(!_api.Ready)throw new AudioDeviceUnavailableException();
            foreach(var pair in copies){var sound=_api.Load(pair.Value);_sounds.Add(pair.Key,sound);_api.Volume(sound,.5f);}
        }
        catch(Exception initialization)
        {
            var errors=new List<Exception>{initialization};Cleanup(errors,false);Throw(errors);throw;
        }
    }
    public void Play(string cue)
    {
        CheckAccess();ArgumentException.ThrowIfNullOrWhiteSpace(cue);
        if(!_sounds.TryGetValue(cue,out var sound))throw new KeyNotFoundException("Audio cue is not configured.");
        _api.Play(sound);
    }
    public void StopAll()
    {
        CheckAccess();var errors=new List<Exception>();
        foreach(var sound in _sounds.Values)try{_api.Stop(sound);}catch(Exception error){errors.Add(error);}
        Throw(errors);
    }
    public void Dispose()
    {
        CheckThread();if(_disposed)return;_disposed=true;
        var errors=new List<Exception>();Cleanup(errors,true);Throw(errors);
    }
    private void Cleanup(List<Exception> errors,bool stop)
    {
        if(stop)foreach(var sound in _sounds.Values)try{_api.Stop(sound);}catch(Exception error){errors.Add(error);}
        foreach(var sound in _sounds.Values.Reverse())try{_api.Unload(sound);}catch(Exception error){errors.Add(error);}
        _sounds.Clear();
        try{_api.Close();_reservation.Release();}catch(Exception error){errors.Add(error);}
    }
    private static void Throw(List<Exception> errors)
    {
        if(errors.Count==1)ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if(errors.Count>1)throw new AggregateException(errors);
    }
    private void CheckThread(){if(Environment.CurrentManagedThreadId!=_owner)throw new InvalidOperationException("Audio belongs to its creating thread.");}
    private void CheckAccess(){CheckThread();ObjectDisposedException.ThrowIf(_disposed,this);}
}
