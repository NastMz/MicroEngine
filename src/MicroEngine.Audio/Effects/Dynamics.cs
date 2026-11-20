namespace MicroEngine.Audio.Effects;

/// <summary>A value the rendering thread moves towards a target in equal steps, one per frame, so a change is not heard as a click.</summary>
internal struct Smoothed
{
    public float Value, Target, Step;
    public int Left;

    /// <summary>Takes <paramref name="target"/> at once (<paramref name="frames"/> 0) or over that many frames.</summary>
    public void To(float target, int frames)
    {
        Target = target;
        if (frames <= 0) { Value = target; Left = 0; return; }
        Left = frames;
        Step = (target - Value) / frames;
    }

    /// <summary>The value for the next frame.</summary>
    public float Next()
    {
        if (Left > 0)
        {
            Value += Step;
            if (--Left == 0) Value = Target;
        }
        return Value;
    }
}

/// <summary>
/// Keeps a mix within a ceiling: when a frame would go over <see cref="Threshold"/>, the gain drops at once to what
/// keeps it there, then recovers over <see cref="Release"/>. It looks at nothing ahead, so it adds no delay; and while
/// the mix stays within the threshold (and the gain has recovered) every sample passes exactly as it is.
/// </summary>
/// <remarks>
/// The mixer has one after "master" (<see cref="AudioEngine.Limiter"/>), so a loud moment is turned down instead of
/// clipped by the device. On a bus of its own it tames one group of sounds.
/// </remarks>
public sealed class Limiter : AudioEffect
{
    private float _threshold = 1, _release = 0.1f;
    // Rendering thread.
    private float _ceiling = 1, _recover, _gain = 1;
    private int _rate = AudioClip.DefaultSampleRate, _channels = 2;

    /// <summary>Creates a limiter at full scale that recovers in a tenth of a second.</summary>
    public Limiter() : base(parameters: 2)
    {
        SetParameter(0, _threshold);
        SetParameter(1, _release);
    }

    /// <summary>The largest sample value let through, 0.05..4 (1, full scale, by default).</summary>
    public float Threshold { get => _threshold; set { if (!float.IsNaN(value)) SetParameter(0, _threshold = Math.Clamp(value, 0.05f, 4)); } }
    /// <summary>Seconds the gain takes to come back after a peak, 0.001..5 (0.1 by default).</summary>
    public float Release { get => _release; set { if (!float.IsNaN(value)) SetParameter(1, _release = Math.Clamp(value, 0.001f, 5)); } }

    /// <inheritdoc/>
    protected override void Prepare(int sampleRate, int channels) => (_rate, _channels) = (sampleRate, channels);

    /// <inheritdoc/>
    protected override void OnParameter(int index, float value)
    {
        if (index == 0) _ceiling = value;
        else _recover = 1f / (value * _rate);
    }

    /// <inheritdoc/>
    protected override void Reset() => _gain = 1;

    /// <inheritdoc/>
    protected override void Process(Span<float> buffer)
    {
        float gain = _gain, ceiling = _ceiling, recover = _recover;
        int channels = _channels;
        for (int i = 0; i < buffer.Length; i += channels)
        {
            float peak = MathF.Abs(buffer[i]);
            if (channels == 2) peak = MathF.Max(peak, MathF.Abs(buffer[i + 1]));
            float needed = peak > ceiling ? ceiling / peak : 1;
            gain = MathF.Min(needed, gain + recover);
            // At a gain of exactly 1 nothing is written: the samples stay bit for bit what the mix made them.
            if (gain >= 1) { gain = 1; continue; }
            buffer[i] *= gain;
            if (channels == 2) buffer[i + 1] *= gain;
        }
        _gain = gain;
    }
}

/// <summary>
/// Turns a bus down while it, or another bus, is loud. Over <see cref="Threshold"/>, each decibel of level gives only
/// 1/<see cref="Ratio"/> of a decibel out. With <see cref="Sidechain"/> naming another bus, it is that bus's level that
/// counts: music turned down while a voice line plays (ducking).
/// </summary>
/// <example>
/// <code>
/// Audio.AddBus("voice");
/// Audio.Bus("music").Add(Compressor.Duck("voice"));   // the music drops 12 dB under every voice line
/// </code>
/// </example>
public sealed class Compressor : AudioEffect
{
    private float _threshold = -18, _ratio = 4, _attack = 0.01f, _release = 0.2f, _makeup, _range = 60;
    // Rendering thread.
    private float _liveThreshold = -18, _slope = 0.75f, _liveRange = 60, _attackCoefficient, _releaseCoefficient, _reduction;
    private Smoothed _gain;
    private bool _fresh = true;
    private int _rate = AudioClip.DefaultSampleRate, _channels = 2;

    /// <summary>Creates a compressor: threshold -18 dB, ratio 4, attack 10 ms, release 200 ms.</summary>
    public Compressor() : base(parameters: 6)
    {
        SetParameter(0, _threshold);
        SetParameter(1, _ratio);
        SetParameter(2, _attack);
        SetParameter(3, _release);
        SetParameter(4, _makeup);
        SetParameter(5, _range);
    }

    /// <summary>
    /// A compressor that ducks its bus under bus <paramref name="under"/>: whenever that bus sounds, this one drops by
    /// <paramref name="decibels"/> within <paramref name="attack"/> seconds, and comes back over
    /// <paramref name="release"/> seconds when it falls silent.
    /// </summary>
    public static Compressor Duck(string under, float decibels = 12, float attack = 0.05f, float release = 0.4f) =>
        new() { Sidechain = under, Threshold = -45, Ratio = 100, Range = decibels, Attack = attack, Release = release };

    /// <summary>The level, in decibels below full scale, over which the bus is turned down (-80..0; -18 by default).</summary>
    public float Threshold { get => _threshold; set { if (!float.IsNaN(value)) SetParameter(0, _threshold = Math.Clamp(value, -80, 0)); } }
    /// <summary>Decibels of level over the threshold for one decibel out, 1..100 (4 by default; 1 changes nothing).</summary>
    public float Ratio { get => _ratio; set { if (!float.IsNaN(value)) SetParameter(1, _ratio = Math.Clamp(value, 1, 100)); } }
    /// <summary>Seconds to turn down when the level rises, 0.0001..2 (0.01 by default).</summary>
    public float Attack { get => _attack; set { if (!float.IsNaN(value)) SetParameter(2, _attack = Math.Clamp(value, 0.0001f, 2)); } }
    /// <summary>Seconds to come back when the level falls, 0.001..5 (0.2 by default).</summary>
    public float Release { get => _release; set { if (!float.IsNaN(value)) SetParameter(3, _release = Math.Clamp(value, 0.001f, 5)); } }
    /// <summary>Decibels added after compressing, -24..24 (0 by default).</summary>
    public float Makeup { get => _makeup; set { if (!float.IsNaN(value)) SetParameter(4, _makeup = Math.Clamp(value, -24, 24)); } }
    /// <summary>The most the bus is ever turned down, in decibels, 0..60 (60 by default: no limit that matters).</summary>
    public float Range { get => _range; set { if (!float.IsNaN(value)) SetParameter(5, _range = Math.Clamp(value, 0, 60)); } }
    /// <summary>
    /// The bus whose level decides (the bus's own when null): name the bus that should be heard over this one.
    /// </summary>
    /// <exception cref="ArgumentException">Set while on a bus: no such bus, or this bus plays into it.</exception>
    public string? Sidechain { get => SidechainBus; set => SidechainBus = value; }

    /// <inheritdoc/>
    protected override void Prepare(int sampleRate, int channels) => (_rate, _channels) = (sampleRate, channels);

    /// <inheritdoc/>
    protected override void OnParameter(int index, float value)
    {
        switch (index)
        {
            case 0: _liveThreshold = value; break;
            case 1: _slope = 1 - 1 / value; break;
            case 2: _attackCoefficient = MathF.Exp(-1f / (value * _rate)); break;
            case 3: _releaseCoefficient = MathF.Exp(-1f / (value * _rate)); break;
            case 4: _gain.To(MathF.Pow(10, value / 20), _fresh ? 0 : _rate / 50); break;
            case 5: _liveRange = value; break;
        }
    }

    /// <inheritdoc/>
    protected override void Reset()
    {
        _reduction = 0;
        _fresh = true;
    }

    /// <inheritdoc/>
    protected override void Process(Span<float> buffer) => Process(buffer, buffer);

    /// <inheritdoc/>
    protected override void Process(Span<float> buffer, ReadOnlySpan<float> sidechain)
    {
        _fresh = false;
        int channels = _channels;
        float reduction = _reduction;
        for (int i = 0; i < buffer.Length; i += channels)
        {
            float level = MathF.Abs(sidechain[i]);
            if (channels == 2) level = MathF.Max(level, MathF.Abs(sidechain[i + 1]));
            // Decibels over the threshold, and how far to turn down for them.
            float over = level > 1e-6f ? 20 * MathF.Log10(level) - _liveThreshold : -200;
            float wanted = over > 0 ? MathF.Min(over * _slope, _liveRange) : 0;
            float coefficient = wanted > reduction ? _attackCoefficient : _releaseCoefficient;
            reduction = wanted + (reduction - wanted) * coefficient;
            float makeup = _gain.Next();
            // Below a thousandth of a decibel there is nothing to do: the sample is left as it is.
            if (reduction < 0.001f && makeup == 1) { if (wanted == 0) reduction = 0; continue; }
            float gain = MathF.Pow(10, -reduction / 20) * makeup;
            buffer[i] *= gain;
            if (channels == 2) buffer[i + 1] *= gain;
        }
        _reduction = reduction;
    }
}
