using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroEngine.Desktop;
namespace Desktop.Contracts;

[TestClass]
public sealed class AudioTests
{
    internal static byte[] Wave()
    {
        var b=new byte[132];"RIFF"u8.CopyTo(b);BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4),124);"WAVEfmt "u8.CopyTo(b.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(16),16);BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(20),1);BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(22),1);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(24),44100);BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(28),88200);BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(32),2);BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(34),16);
        "data"u8.CopyTo(b.AsSpan(36));BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(40),88);return b;
    }
    private static Dictionary<string,byte[]> Waves()=>new(){{"alpha",Wave()},{"beta",Wave()}};
    private sealed class Api : IDesktopAudioApi
    {
        public List<string> Trace { get; }=[];public HashSet<string> Fail {get;}=[];public bool Ready {get;set;}=true;private int _id;
        private void Call(string value){Trace.Add(value);if(Fail.Contains(value))throw new InvalidOperationException(value);}
        public void Init()=>Call("init");public object Load(byte[] encoded){int id=++_id;Call($"load:{id}");return id;}
        public void Volume(object sound,float volume){Assert.AreEqual(.5f,volume);Call($"volume:{sound}");}
        public void Play(object sound)=>Call($"play:{sound}");public void Stop(object sound)=>Call($"stop:{sound}");
        public void Unload(object sound)=>Call($"unload:{sound}");public void Close()=>Call("close");
    }
    [TestMethod] public void ValidationOccursBeforeNativeEffects()
    {
        var api=new Api();var reservation=new AudioReservation();
        Assert.ThrowsExactly<ArgumentNullException>(()=>new DesktopOneShotAudio(null!,api,reservation));
        Assert.ThrowsExactly<ArgumentException>(()=>new DesktopOneShotAudio(new Dictionary<string,byte[]>(),api,reservation));
        Assert.ThrowsExactly<ArgumentException>(()=>new DesktopOneShotAudio(new Dictionary<string,byte[]>{{" ",Wave()}},api,reservation));
        Assert.ThrowsExactly<InvalidDataException>(()=>new DesktopOneShotAudio(new Dictionary<string,byte[]>{{"a",new byte[44]}},api,reservation));
        Assert.AreEqual(0,api.Trace.Count);
    }
    [TestMethod] public void ProfileRejectsMalformedLengthsAndCopiesAcceptedBytes()
    {
        var valid=Wave();var copy=WaveFile.CopyAndValidate(valid);Assert.AreNotSame(valid,copy);CollectionAssert.AreEqual(valid,copy);
        foreach(int offset in new[]{4,16,20,22,24,28,32,34,40})
        {
            var bad=Wave();bad[offset]=255;Assert.ThrowsExactly<InvalidDataException>(()=>WaveFile.CopyAndValidate(bad),$"offset {offset}");
        }
        Assert.ThrowsExactly<InvalidDataException>(()=>WaveFile.CopyAndValidate(new byte[1048577]));
    }
    [TestMethod] public void PlaybackAndDisposalHaveDefinedOrder()
    {
        var api=new Api();var audio=new DesktopOneShotAudio(Waves(),api,new());
        audio.Play("alpha");Assert.AreEqual("play:1",api.Trace[^1]);var count=api.Trace.Count;
        Assert.ThrowsExactly<KeyNotFoundException>(()=>audio.Play("unknown"));Assert.ThrowsExactly<ArgumentException>(()=>audio.Play(" "));Assert.AreEqual(count,api.Trace.Count);
        audio.Dispose();audio.Dispose();CollectionAssert.AreEqual(new[]{"stop:1","stop:2","unload:2","unload:1","close"},api.Trace.TakeLast(5).ToArray());
        Assert.ThrowsExactly<ObjectDisposedException>(()=>audio.Play("alpha"));
    }
    [TestMethod] public void PartialFailureRetainsPrimaryAndAllCleanupErrorsAndPoisonsFailedClose()
    {
        var api=new Api();api.Fail.UnionWith(["load:2","unload:1","close"]);var reservation=new AudioReservation();
        var error=Assert.ThrowsExactly<AggregateException>(()=>new DesktopOneShotAudio(Waves(),api,reservation));Assert.AreEqual(3,error.InnerExceptions.Count);
        CollectionAssert.AreEqual(new[]{"unload:1","close"},api.Trace.TakeLast(2).ToArray());
        Assert.ThrowsExactly<InvalidOperationException>(()=>new DesktopOneShotAudio(Waves(),new Api(),reservation));
    }
    [TestMethod] public void UnavailabilityRequiresCleanInitializationAndCleanup()
    {
        var api=new Api{Ready=false};var reservation=new AudioReservation();
        Assert.ThrowsExactly<AudioDeviceUnavailableException>(()=>new DesktopOneShotAudio(Waves(),api,reservation));
        using(var recovered=new DesktopOneShotAudio(Waves(),new Api(),reservation)){}
        var failed=new Api{Ready=false};failed.Fail.Add("close");
        Assert.ThrowsExactly<AggregateException>(()=>new DesktopOneShotAudio(Waves(),failed,new()));
        var init=new Api();init.Fail.Add("init");Assert.ThrowsExactly<InvalidOperationException>(()=>new DesktopOneShotAudio(Waves(),init,new()));
    }
    [TestMethod] public void WrongThreadCallsNeverTouchBackendAndDisposeAttemptsEverything()
    {
        var api=new Api();var audio=new DesktopOneShotAudio(Waves(),api,new());var count=api.Trace.Count;
        Task.Run(()=>{Assert.ThrowsExactly<InvalidOperationException>(()=>audio.Play("alpha"));Assert.ThrowsExactly<InvalidOperationException>(()=>audio.StopAll());Assert.ThrowsExactly<InvalidOperationException>(()=>audio.Dispose());}).GetAwaiter().GetResult();
        Assert.AreEqual(count,api.Trace.Count);api.Fail.UnionWith(["stop:1","unload:2","close"]);
        var error=Assert.ThrowsExactly<AggregateException>(()=>audio.Dispose());Assert.AreEqual(3,error.InnerExceptions.Count);Assert.AreEqual("close",api.Trace[^1]);
    }
    [TestMethod] public void UnknownPaddedChunksAndDataBeforeFormatAreAccepted()
    {
        var wave=Wave();var b=new byte[wave.Length+10];wave.AsSpan(0,12).CopyTo(b);
        "JUNK"u8.CopyTo(b.AsSpan(12));BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(16),1);b[20]=42;
        wave.AsSpan(12).CopyTo(b.AsSpan(22));BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4),b.Length-8);
        CollectionAssert.AreEqual(b,WaveFile.CopyAndValidate(b));
        var reordered=new byte[wave.Length];wave.AsSpan(0,12).CopyTo(reordered);
        wave.AsSpan(36).CopyTo(reordered.AsSpan(12));wave.AsSpan(12,24).CopyTo(reordered.AsSpan(108));
        CollectionAssert.AreEqual(reordered,WaveFile.CopyAndValidate(reordered));
    }
    [TestMethod] public void DuplicateChunksAndDurationOverflowAreRejected()
    {
        var wave=Wave();foreach(var chunk in new[]{wave.AsSpan(12,24).ToArray(),wave.AsSpan(36).ToArray()})
        {
            var bad=wave.Concat(chunk).ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(4),bad.Length-8);
            Assert.ThrowsExactly<InvalidDataException>(()=>WaveFile.CopyAndValidate(bad));
        }
        var maximum=new byte[44+48000*2*2*3];wave.AsSpan(0,44).CopyTo(maximum);
        BinaryPrimitives.WriteInt32LittleEndian(maximum.AsSpan(4),maximum.Length-8);
        BinaryPrimitives.WriteInt16LittleEndian(maximum.AsSpan(22),2);BinaryPrimitives.WriteInt32LittleEndian(maximum.AsSpan(24),48000);
        BinaryPrimitives.WriteInt32LittleEndian(maximum.AsSpan(28),192000);BinaryPrimitives.WriteInt16LittleEndian(maximum.AsSpan(32),4);
        BinaryPrimitives.WriteInt32LittleEndian(maximum.AsSpan(40),maximum.Length-44);
        Assert.AreEqual(maximum.Length,WaveFile.CopyAndValidate(maximum).Length);
        var tooLong=maximum.Concat(new byte[4]).ToArray();BinaryPrimitives.WriteInt32LittleEndian(tooLong.AsSpan(4),tooLong.Length-8);BinaryPrimitives.WriteInt32LittleEndian(tooLong.AsSpan(40),tooLong.Length-44);
        Assert.ThrowsExactly<InvalidDataException>(()=>WaveFile.CopyAndValidate(tooLong));
    }
    [TestMethod] public void VolumeFailureUnloadsNewSoundAndSuccessfulCloseReleasesReservation()
    {
        var api=new Api();api.Fail.Add("volume:1");var reservation=new AudioReservation();
        Assert.ThrowsExactly<InvalidOperationException>(()=>new DesktopOneShotAudio(Waves(),api,reservation));
        CollectionAssert.AreEqual(new[]{"unload:1","close"},api.Trace.TakeLast(2).ToArray());
        using var recovered=new DesktopOneShotAudio(Waves(),new Api(),reservation);
    }
}