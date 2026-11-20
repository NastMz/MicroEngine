namespace MicroEngine.Audio.Effects;

/// <summary>
/// An echo: the bus is heard again <see cref="Time"/> seconds later, and each repeat again, <see cref="Feedback"/>
/// times as loud.
/// </summary>
public sealed class Delay : AudioEffect
{
    /// <summary>The longest <see cref="Time"/>, in seconds.</summary>
    public const float MaxTime = 2;

    private float _time = 0.3f, _feedback = 0.35f, _wet = 0.5f, _dry = 1;
    // Rendering thread: a ring of stereo frames, and the gliding parameters.
    private float[] _line = [];
    private int _frames, _write, _rate = AudioClip.DefaultSampleRate, _channels = 2;
    private Smoothed _liveTime, _liveWet, _liveDry;
    private float _liveFeedback;
    private bool _fresh = true;

    /// <summary>Creates an echo of 0.3 seconds.</summary>
    public Delay() : base(parameters: 4)
    {
        SetParameter(0, _time);
        SetParameter(1, _feedback);
        SetParameter(2, _wet);
        SetParameter(3, _dry);
    }

    /// <summary>Seconds between repeats, 0.001..<see cref="MaxTime"/> (0.3 by default). A change glides, bending the pitch of what is in the echo for a moment, as a tape delay does.</summary>
    public float Time { get => _time; set { if (!float.IsNaN(value)) SetParameter(0, _time = Math.Clamp(value, 0.001f, MaxTime)); } }
    /// <summary>How loud each repeat is against the one before, 0..0.95 (0.35 by default; 0 for a single repeat).</summary>
    public float Feedback { get => _feedback; set { if (!float.IsNaN(value)) SetParameter(1, _feedback = Math.Clamp(value, 0, 0.95f)); } }
    /// <summary>Gain of the echoes, 0..1 (0.5 by default).</summary>
    public float Wet { get => _wet; set { if (!float.IsNaN(value)) SetParameter(2, _wet = Math.Clamp(value, 0, 1)); } }
    /// <summary>Gain of the sound itself, 0..1 (1 by default).</summary>
    public float Dry { get => _dry; set { if (!float.IsNaN(value)) SetParameter(3, _dry = Math.Clamp(value, 0, 1)); } }

    /// <inheritdoc/>
    protected override void Prepare(int sampleRate, int channels)
    {
        int frames = (int)(MaxTime * sampleRate) + 2;
        if (_line.Length != frames * channels) _line = new float[frames * channels];
        (_frames, _rate, _channels) = (frames, sampleRate, channels);
    }

    /// <inheritdoc/>
    protected override void OnParameter(int index, float value)
    {
        int glide = _fresh ? 0 : _rate / 50;
        switch (index)
        {
            case 0: _liveTime.To(value * _rate, _fresh ? 0 : _rate / 5); break;
            case 1: _liveFeedback = value; break;
            case 2: _liveWet.To(value, glide); break;
            case 3: _liveDry.To(value, glide); break;
        }
    }

    /// <inheritdoc/>
    protected override void Reset()
    {
        _line.AsSpan().Clear();
        _write = 0;
        _fresh = true;
    }

    /// <inheritdoc/>
    protected override void Process(Span<float> buffer)
    {
        _fresh = false;
        int channels = _channels, frames = _frames;
        var line = _line;
        for (int i = 0; i < buffer.Length; i += channels)
        {
            float delay = _liveTime.Next(), wet = _liveWet.Next(), dry = _liveDry.Next();
            // The frame that far back, between the two stored frames around it.
            float at = _write - delay;
            if (at < 0) at += frames;
            int first = (int)at;
            if (first >= frames) first -= frames;
            int second = first + 1 == frames ? 0 : first + 1;
            float t = at - first;
            for (int c = 0; c < channels; c++)
            {
                float echo = line[first * channels + c] + (line[second * channels + c] - line[first * channels + c]) * t;
                float input = buffer[i + c];
                line[_write * channels + c] = input + echo * _liveFeedback;
                buffer[i + c] = input * dry + echo * wet;
            }
            if (++_write == frames) _write = 0;
        }
    }
}

/// <summary>
/// A room: the bus is heard with the reflections of a space around it, from a small room to a hall
/// (<see cref="RoomSize"/>). The design is Freeverb's (Jezar at Dreampoint, public domain): eight comb filters and four
/// all-pass filters for each side.
/// </summary>
public sealed class Reverb : AudioEffect
{
    private static readonly int[] CombFrames = [1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617], AllPassFrames = [556, 441, 341, 225];
    private const int Spread = 23;
    private const float InputGain = 0.015f, WetScale = 3;

    private float _roomSize = 0.5f, _damping = 0.5f, _wet = 0.3f, _dry = 1, _width = 1;
    // Rendering thread: the delay lines of each side (combs then all-passes), their positions and the combs' memory.
    private float[][] _lines = [];
    private readonly int[] _positions = new int[24];
    private readonly float[] _filters = new float[16];
    private float _feedback, _damp;
    private Smoothed _liveWet, _liveDry, _liveWidth;
    private int _rate = AudioClip.DefaultSampleRate, _channels = 2;
    private bool _fresh = true;

    /// <summary>Creates a medium room, mixed in at 0.3.</summary>
    public Reverb() : base(parameters: 5)
    {
        SetParameter(0, _roomSize);
        SetParameter(1, _damping);
        SetParameter(2, _wet);
        SetParameter(3, _dry);
        SetParameter(4, _width);
    }

    /// <summary>How large the space sounds, 0 (a small room) to 1 (a hall that rings for many seconds); 0.5 by default.</summary>
    public float RoomSize { get => _roomSize; set { if (!float.IsNaN(value)) SetParameter(0, _roomSize = Math.Clamp(value, 0, 1)); } }
    /// <summary>How fast the high frequencies die in the space, 0 (bright, hard walls) to 1 (dull, soft walls); 0.5 by default.</summary>
    public float Damping { get => _damping; set { if (!float.IsNaN(value)) SetParameter(1, _damping = Math.Clamp(value, 0, 1)); } }
    /// <summary>Gain of the reflections, 0..1 (0.3 by default).</summary>
    public float Wet { get => _wet; set { if (!float.IsNaN(value)) SetParameter(2, _wet = Math.Clamp(value, 0, 1)); } }
    /// <summary>Gain of the sound itself, 0..1 (1 by default).</summary>
    public float Dry { get => _dry; set { if (!float.IsNaN(value)) SetParameter(3, _dry = Math.Clamp(value, 0, 1)); } }
    /// <summary>How wide the reflections are in stereo, 0 (the same on both sides) to 1 (default).</summary>
    public float Width { get => _width; set { if (!float.IsNaN(value)) SetParameter(4, _width = Math.Clamp(value, 0, 1)); } }

    /// <summary>The length in frames of comb <paramref name="index"/> (0..7) of the left side at <paramref name="sampleRate"/>: what a test needs to know how long the room rings.</summary>
    internal static int CombLength(int index, int sampleRate) => Math.Max(1, (int)((long)CombFrames[index] * sampleRate / 44100));
    /// <summary>The gain a comb feeds back for a room size.</summary>
    internal static float FeedbackFor(float roomSize) => roomSize * 0.28f + 0.7f;

    /// <inheritdoc/>
    protected override void Prepare(int sampleRate, int channels)
    {
        if (_lines.Length != 0 && _rate == sampleRate && _channels == channels) return;
        (_rate, _channels) = (sampleRate, channels);
        // 12 lines for each side: the right side's are a little longer, which is what makes the reflections wide.
        _lines = new float[24][];
        for (int side = 0; side < 2; side++)
            for (int i = 0; i < 12; i++)
            {
                int frames = (i < 8 ? CombFrames[i] : AllPassFrames[i - 8]) + side * Spread;
                _lines[side * 12 + i] = new float[Math.Max(1, (int)((long)frames * sampleRate / 44100))];
            }
    }

    /// <inheritdoc/>
    protected override void OnParameter(int index, float value)
    {
        int glide = _fresh ? 0 : _rate / 50;
        switch (index)
        {
            case 0: _feedback = FeedbackFor(value); break;
            case 1: _damp = value * 0.4f; break;
            case 2: _liveWet.To(value * WetScale, glide); break;
            case 3: _liveDry.To(value, glide); break;
            case 4: _liveWidth.To(value, glide); break;
        }
    }

    /// <inheritdoc/>
    protected override void Reset()
    {
        foreach (var line in _lines) line.AsSpan().Clear();
        _positions.AsSpan().Clear();
        _filters.AsSpan().Clear();
        _fresh = true;
    }

    /// <inheritdoc/>
    protected override void Process(Span<float> buffer)
    {
        _fresh = false;
        int channels = _channels;
        for (int i = 0; i < buffer.Length; i += channels)
        {
            float inLeft = buffer[i], inRight = channels == 2 ? buffer[i + 1] : inLeft;
            float input = (inLeft + inRight) * InputGain;
            float left = Side(0, input), right = channels == 2 ? Side(1, input) : left;
            float wet = _liveWet.Next(), dry = _liveDry.Next(), width = _liveWidth.Next();
            float wetSame = wet * (width / 2 + 0.5f), wetOther = wet * ((1 - width) / 2);
            buffer[i] = left * wetSame + right * wetOther + inLeft * dry;
            if (channels == 2) buffer[i + 1] = right * wetSame + left * wetOther + inRight * dry;
        }
    }

    /// <summary>One frame through the combs (in parallel) and the all-passes (in series) of a side.</summary>
    private float Side(int side, float input)
    {
        float sum = 0;
        int first = side * 12;
        for (int c = 0; c < 8; c++)
        {
            var line = _lines[first + c];
            ref int position = ref _positions[first + c];
            ref float filter = ref _filters[side * 8 + c];
            float output = line[position];
            filter = output * (1 - _damp) + filter * _damp;
            // Values too small to hear are dropped, so a tail that has died costs no more than silence.
            if (MathF.Abs(filter) < 1e-20f) filter = 0;
            line[position] = input + filter * _feedback;
            if (++position == line.Length) position = 0;
            sum += output;
        }
        for (int a = 8; a < 12; a++)
        {
            var line = _lines[first + a];
            ref int position = ref _positions[first + a];
            float held = line[position], output = held - sum;
            line[position] = sum + held * 0.5f;
            if (++position == line.Length) position = 0;
            sum = output;
        }
        return sum;
    }
}
