using MicroEngine.Audio;
using MicroEngine.Loop;
using System.Runtime.ExceptionServices;
namespace Platformer;

/// <summary>Only this game composes audio/window ownership; the engine loop remains unchanged.</summary>
public sealed class SoundPlatform : IFramePlatform
{
    private readonly IFramePlatform _window;
    private readonly IOneShotAudio? _audio;
    private bool _disposed;
    private readonly int _owner=Environment.CurrentManagedThreadId;
    private SoundPlatform(IFramePlatform window,IOneShotAudio? audio)=>(_window,_audio)=(window,audio);
    public static SoundPlatform Create(Func<IFramePlatform> createWindow,Func<IOneShotAudio?> createAudio)
    {
        ArgumentNullException.ThrowIfNull(createWindow);ArgumentNullException.ThrowIfNull(createAudio);
        var audio=createAudio();
        try{return new(createWindow()??throw new InvalidOperationException("Window factory returned null."),audio);}
        catch(Exception initialization)
        {
            try{audio?.Dispose();}catch(Exception cleanup){throw new AggregateException(initialization,cleanup);}
            throw;
        }
    }
    public PlatformFrame Poll(){CheckAccess();return _window.Poll();}
    public ICanvas Canvas {get{CheckAccess();return _window.Canvas;}}
    public void Begin(){CheckAccess();_window.Begin();}
    public void End(){CheckAccess();_window.End();}
    public void Dispose()
    {
        CheckThread();if(_disposed)return;_disposed=true;List<Exception> errors=[];
        try{_window.Dispose();}catch(Exception error){errors.Add(error);}
        try{_audio?.Dispose();}catch(Exception error){errors.Add(error);}
        if(errors.Count==1)ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if(errors.Count>1)throw new AggregateException(errors);
    }
    private void CheckThread(){if(Environment.CurrentManagedThreadId!=_owner)throw new InvalidOperationException("Sound platform belongs to its creating thread.");}
    private void CheckAccess(){CheckThread();ObjectDisposedException.ThrowIf(_disposed,this);}
}