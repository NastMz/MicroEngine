namespace MicroEngine.Audio.Effects;

/// <summary>
/// A second-order filter (12 dB per octave): the base of <see cref="LowPass"/> and <see cref="HighPass"/>. A change of
/// <see cref="Cutoff"/> or <see cref="Resonance"/> glides over a fiftieth of a second instead of clicking.
/// </summary>
public abstract class BiquadFilter : AudioEffect
{
    private float _cutoff, _resonance = 0.7071f;
    // Rendering thread: the gliding parameters, the coefficients for them, and each channel's memory.
    private Smoothed _liveCutoff, _liveResonance;
    private double _b0, _b1, _b2, _a1, _a2, _z1Left, _z2Left, _z1Right, _z2Right;
    private int _rate = AudioClip.DefaultSampleRate, _channels = 2, _untilUpdate;
    private bool _fresh = true;

    private protected BiquadFilter(float cutoff) : base(parameters: 2)
    {
        SetParameter(0, _cutoff = cutoff);
        SetParameter(1, _resonance);
    }

    /// <summary>The frequency, in hertz, where the filter starts to cut (clamped to 10..20000, and below half the sample rate).</summary>
    public float Cutoff { get => _cutoff; set { if (!float.IsNaN(value)) SetParameter(0, _cutoff = Math.Clamp(value, 10, 20000)); } }
    /// <summary>
    /// How much the filter emphasizes the cutoff frequency (its Q), 0.1..20: 0.7071 by default, which is flat; higher
    /// values ring at the cutoff.
    /// </summary>
    public float Resonance { get => _resonance; set { if (!float.IsNaN(value)) SetParameter(1, _resonance = Math.Clamp(value, 0.1f, 20)); } }

    /// <summary>The numerator of the filter for <paramref name="cosine"/> of the cutoff's angle, before normalizing.</summary>
    private protected abstract (double B0, double B1, double B2) Numerator(double cosine);

    /// <inheritdoc/>
    protected override void Prepare(int sampleRate, int channels)
    {
        (_rate, _channels) = (sampleRate, channels);
        // Before the first chain delivers them: an effect prepared but never given values still has its own.
        _liveCutoff.To(_cutoff, 0);
        _liveResonance.To(_resonance, 0);
        Update();
    }

    /// <inheritdoc/>
    protected override void OnParameter(int index, float value)
    {
        int glide = _fresh ? 0 : _rate / 50;
        if (index == 0) _liveCutoff.To(value, glide);
        else _liveResonance.To(value, glide);
        _untilUpdate = 0;
    }

    /// <inheritdoc/>
    protected override void Reset()
    {
        _z1Left = _z2Left = _z1Right = _z2Right = 0;
        _fresh = true;
        _untilUpdate = 0;
    }

    private void Update()
    {
        double cutoff = Math.Min(_liveCutoff.Value, 0.45 * _rate);
        double angle = 2 * Math.PI * cutoff / _rate, cosine = Math.Cos(angle), alpha = Math.Sin(angle) / (2 * _liveResonance.Value);
        var (b0, b1, b2) = Numerator(cosine);
        double a0 = 1 + alpha;
        (_b0, _b1, _b2, _a1, _a2) = (b0 / a0, b1 / a0, b2 / a0, -2 * cosine / a0, (1 - alpha) / a0);
    }

    /// <inheritdoc/>
    protected override void Process(Span<float> buffer)
    {
        _fresh = false;
        int channels = _channels;
        for (int i = 0; i < buffer.Length; i += channels)
        {
            // While a parameter glides the coefficients follow it every 16 frames, counted by the filter itself, so the
            // result does not depend on how the mixer cuts its blocks.
            if (_liveCutoff.Left > 0 || _liveResonance.Left > 0 || _untilUpdate == 0)
            {
                _liveCutoff.Next();
                _liveResonance.Next();
                bool gliding = _liveCutoff.Left > 0 || _liveResonance.Left > 0;
                if (_untilUpdate <= 0 || !gliding)
                {
                    Update();
                    _untilUpdate = gliding ? 16 : -1;
                }
                else _untilUpdate--;
            }
            double x = buffer[i], y = _b0 * x + _z1Left;
            _z1Left = _b1 * x - _a1 * y + _z2Left;
            _z2Left = _b2 * x - _a2 * y;
            buffer[i] = (float)y;
            if (channels == 2)
            {
                x = buffer[i + 1];
                y = _b0 * x + _z1Right;
                _z1Right = _b1 * x - _a1 * y + _z2Right;
                _z2Right = _b2 * x - _a2 * y;
                buffer[i + 1] = (float)y;
            }
        }
        // Memory that has decayed to nothing is cleared, so silence stays cheap to process.
        if (Math.Abs(_z1Left) < 1e-30) _z1Left = 0;
        if (Math.Abs(_z2Left) < 1e-30) _z2Left = 0;
        if (Math.Abs(_z1Right) < 1e-30) _z1Right = 0;
        if (Math.Abs(_z2Right) < 1e-30) _z2Right = 0;
    }
}

/// <summary>
/// Lets through what is below <see cref="BiquadFilter.Cutoff"/> and cuts what is above: a muffled sound (music behind
/// a pause menu, under water, behind a wall).
/// </summary>
/// <example>
/// <code>
/// var muffle = new LowPass { Cutoff = 800, Bypass = true };
/// Audio.Bus("music").Add(muffle);
/// // When the game pauses and resumes:
/// muffle.Bypass = !paused;
/// </code>
/// </example>
public sealed class LowPass : BiquadFilter
{
    /// <summary>Creates a low-pass filter at 1000 Hz.</summary>
    public LowPass() : base(1000) { }
    private protected override (double B0, double B1, double B2) Numerator(double cosine) => ((1 - cosine) / 2, 1 - cosine, (1 - cosine) / 2);
}

/// <summary>
/// Lets through what is above <see cref="BiquadFilter.Cutoff"/> and cuts what is below: a thin sound (a radio, a
/// telephone), or a rumble removed.
/// </summary>
public sealed class HighPass : BiquadFilter
{
    /// <summary>Creates a high-pass filter at 200 Hz.</summary>
    public HighPass() : base(200) { }
    private protected override (double B0, double B1, double B2) Numerator(double cosine) => ((1 + cosine) / 2, -(1 + cosine), (1 + cosine) / 2);
}
