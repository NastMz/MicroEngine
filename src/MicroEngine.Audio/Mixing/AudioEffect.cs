namespace MicroEngine.Audio;

/// <summary>
/// A processor in a bus's effect chain (<see cref="AudioBus.Add"/>): it changes the bus's mix in place, one block at a
/// time, on the thread that renders audio. The built-in effects (<c>MicroEngine.Audio.Effects</c>) derive from it, and so
/// does an effect a game or a package writes.
/// </summary>
/// <remarks>
/// <para>
/// Two threads touch an effect. The game's thread creates it, sets its properties and adds it to a bus. The rendering
/// thread calls <see cref="Process(Span{float})"/>. They never share a field: a property setter calls
/// <see cref="SetParameter"/>, which keeps the value for the getter and sends it to the rendering thread through the
/// mixer's command queue; the rendering thread receives it in <see cref="OnParameter"/> at the exact output frame of
/// the game step that set it, as a voice's volume does. No lock is involved, and the same calls give the same samples
/// whatever block sizes the sink renders.
/// </para>
/// <para>
/// What keeps the two apart, as rules of the contract: <see cref="Prepare"/> is called once in an effect's life, on the
/// game's thread, before the rendering thread has ever seen it; from then on only the rendering thread calls
/// <see cref="OnParameter"/>, <see cref="Reset"/> and <c>Process</c>, also when the effect is taken off its bus and
/// added again, or moved to another bus (it is cleared with <see cref="Reset"/> where it enters a chain). An effect
/// belongs to the first mixer that plays it: it cannot be added to a bus of another one, whose rendering thread would
/// process it while the first may still do so. And <c>Process</c> never sets the effect's own properties: they are
/// the game's thread's.
/// </para>
/// <para>
/// <see cref="Process(Span{float})"/> must not allocate, lock, block or throw. An effect that throws is taken out of the
/// mix for good (<see cref="Faulted"/>) and the bus goes on without it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Tremolo : AudioEffect
/// {
///     private float _rate = 5, _liveRate = 5, _phase, _step;
///     private int _channels;
///     public Tremolo() : base(parameters: 1) { }
///     public float Rate { get =&gt; _rate; set { _rate = value; SetParameter(0, value); } }
///     protected override void Prepare(int sampleRate, int channels) =&gt; (_channels, _step) = (channels, MathF.Tau / sampleRate);
///     protected override void OnParameter(int index, float value) =&gt; _liveRate = value;
///     protected override void Process(Span&lt;float&gt; buffer)
///     {
///         for (int i = 0; i &lt; buffer.Length; i += _channels)
///         {
///             float gain = 0.5f + 0.5f * MathF.Cos(_phase);
///             _phase = (_phase + _step * _liveRate) % MathF.Tau;
///             for (int c = 0; c &lt; _channels; c++) buffer[i + c] *= gain;
///         }
///     }
/// }
/// </code>
/// </example>
public abstract class AudioEffect
{
    private readonly float[] _parameters;
    private bool _bypass;
    private string? _sidechain;

    /// <summary>Creates an effect with <paramref name="parameters"/> numbered parameters (0 for an effect without any).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameters"/> is negative.</exception>
    protected AudioEffect(int parameters = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parameters);
        _parameters = new float[parameters];
    }

    // Main thread: the bus the effect is on, whether a parameter could not be queued (the sink stopped reading), and
    // the mixer that prepared it, which is the only one whose render function ever touches it.
    internal AudioBus? Bus;
    internal bool Unsent;
    internal AudioEngine? Owner;
    // Rendering thread.
    internal bool Bypassed, Failed;
    private volatile Exception? _error;

    /// <summary>
    /// Whether the bus's mix passes the effect unchanged, from the current step's frame; its state is cleared
    /// (<see cref="Reset"/>) when it is switched back in.
    /// </summary>
    public bool Bypass
    {
        get => _bypass;
        set
        {
            if (_bypass == value) return;
            _bypass = value;
            Bus?.Engine?.PostBypass(this, value);
        }
    }

    /// <summary>Whether <see cref="Process(Span{float})"/> threw: the effect no longer takes part in the mix.</summary>
    public bool Faulted => _error is not null;
    /// <summary>What <see cref="Process(Span{float})"/> (or another call on the rendering thread) threw, or null.</summary>
    public Exception? Error => _error;

    internal int ParameterCount => _parameters.Length;
    internal float ParameterValue(int index) => _parameters[index];
    internal void Fault(Exception error) => _error = error;

    /// <summary>The value a property last gave parameter <paramref name="index"/> (the game's thread).</summary>
    protected float GetParameter(int index) => _parameters[index];

    /// <summary>
    /// Sets parameter <paramref name="index"/> from a property setter (the game's thread): the rendering thread
    /// receives it in <see cref="OnParameter"/> at the current step's frame. A value that is not a number is ignored.
    /// </summary>
    protected void SetParameter(int index, float value)
    {
        if (float.IsNaN(value)) return;
        _parameters[index] = value;
        Bus?.Engine?.PostParameter(this, index, value);
    }

    /// <summary>
    /// The bus whose mix the effect listens to besides its own (a compressor that turns the music down while a voice
    /// line plays); null for none. With a bus named here, the mixer calls
    /// <see cref="Process(Span{float}, ReadOnlySpan{float})"/> with that bus's mix, after its effects and volume.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Set while the effect is on a bus: the named bus does not exist, or it is the effect's own bus or one it plays
    /// into (its mix is not known before this one's).
    /// </exception>
    protected string? SidechainBus
    {
        get => _sidechain;
        set
        {
            if (_sidechain == value) return;
            string? old = _sidechain;
            _sidechain = value;
            try { Bus?.GraphChanged(); }
            catch { _sidechain = old; throw; }
        }
    }

    internal string? Key => _sidechain;

    /// <summary>
    /// Called once in the effect's life, on the game's thread, the first time it is added to a bus of a device that
    /// plays, before the rendering thread processes anything: the place to allocate delay lines and tables for the
    /// mixer's format. It is not called again when the effect is removed and added again, so it never runs while the
    /// rendering thread uses the effect. On a device without sound it is not called at all.
    /// </summary>
    /// <param name="sampleRate">Frames per second.</param>
    /// <param name="channels">Samples per frame, 1 or 2.</param>
    protected virtual void Prepare(int sampleRate, int channels) { }

    /// <summary>
    /// Rendering thread: parameter <paramref name="index"/> takes <paramref name="value"/> from the next frame
    /// processed. Every parameter is delivered once when the effect enters a chain, then at each change.
    /// </summary>
    protected virtual void OnParameter(int index, float value) { }

    /// <summary>Rendering thread: forgets what the effect has heard (delay lines, filter memory), when it enters a chain or leaves bypass.</summary>
    protected virtual void Reset() { }

    /// <summary>
    /// Rendering thread: processes <paramref name="buffer"/> in place. The samples are interleaved by channel
    /// (<c>buffer.Length / channels</c> frames) and blocks follow one another without gaps, of any length from one frame.
    /// </summary>
    protected abstract void Process(Span<float> buffer);

    /// <summary>
    /// Rendering thread, for an effect with a <see cref="SidechainBus"/>: processes <paramref name="buffer"/> in place,
    /// with the same frames of the named bus in <paramref name="sidechain"/>. The default ignores them.
    /// </summary>
    protected virtual void Process(Span<float> buffer, ReadOnlySpan<float> sidechain) => Process(buffer);

    // What the mixer calls: the overridable members stay protected, so an effect outside the engine overrides them as
    // an effect inside it does.
    internal void PrepareFor(int sampleRate, int channels) => Prepare(sampleRate, channels);
    internal void Deliver(int index, float value) => OnParameter(index, value);
    internal void Clear() => Reset();
    internal void Run(Span<float> buffer) => Process(buffer);
    internal void Run(Span<float> buffer, ReadOnlySpan<float> sidechain) => Process(buffer, sidechain);
}
