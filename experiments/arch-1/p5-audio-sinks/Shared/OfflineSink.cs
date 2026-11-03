namespace P5.Audio;

/// <summary>
/// Renders into memory with no device and no clock: the test sink. Each <see cref="Pump"/> renders exactly
/// <c>FramesPerPump</c> frames in blocks of <c>BlockFrames</c>, so the output depends only on the commands, never on time.
/// </summary>
public sealed class OfflineSink(AudioFormat format, int blockFrames, int framesPerPump) : IAudioSink
{
    private AudioRender? _render;
    private readonly List<float> _output = [];
    private float[] _block = new float[blockFrames * format.Channels];

    public AudioFormat Format => format;
    public AudioSinkDriver Driver => AudioSinkDriver.HostPump;
    public int DevicePeriodFrames => blockFrames;
    public int TargetBufferedFrames => 0;
    public long StarvedFrames => 0;
    public long PlayedFrames => _output.Count / format.Channels;
    public IReadOnlyList<float> Output => _output;

    public void Start(AudioRender render) => _render = render;

    public void Pump()
    {
        if (_render is null) return;
        int left = framesPerPump;
        while (left > 0)
        {
            int n = Math.Min(left, blockFrames);
            var span = _block.AsSpan(0, n * format.Channels);
            _render(span, 0);
            for (int i = 0; i < span.Length; i++) _output.Add(span[i]);
            left -= n;
        }
    }

    public string Describe() => $"offline block={blockFrames} pump={framesPerPump}";
    public void Dispose() { }
}
