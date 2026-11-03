namespace MicroEngine.Kernel;

/// <summary>
/// A <see cref="Random"/> that draws exactly what <c>new Random(seed)</c> draws, and whose state can take part in the
/// per-step state hash (<see cref="IStateHashed"/>): its seed, how many values it drew and a running hash of them. Two
/// runs that drew the same values from the same seed hash the same; one extra or different draw changes the hash.
/// </summary>
/// <remarks>
/// The game's own generator (<c>Game.Random</c>) is one. Use one wherever simulation draws from a generator of its own
/// (<c>new SeededRandom(seed)</c> instead of <c>new Random(seed)</c>) and add it from the owner's <c>HashState</c>.
/// </remarks>
public sealed class SeededRandom : Random, IStateHashed
{
    private const ulong Prime = 1099511628211UL;
    private readonly Random _inner;
    private ulong _digest = 14695981039346656037UL;

    /// <summary>Creates a generator that draws the sequence of <c>new Random(<paramref name="seed"/>)</c>.</summary>
    public SeededRandom(int seed)
    {
        Seed = seed;
        _inner = new Random(seed);
    }

    /// <summary>The seed the generator started from.</summary>
    public int Seed { get; }

    /// <summary>How many values (or byte buffers) were drawn so far.</summary>
    public long Draws { get; private set; }

    private int Drew(int value) { Draws++; _digest = (_digest ^ unchecked((uint)value)) * Prime; return value; }
    private long Drew(long value) { Draws++; _digest = (_digest ^ unchecked((ulong)value)) * Prime; return value; }
    private double Drew(double value) { Draws++; _digest = (_digest ^ unchecked((ulong)BitConverter.DoubleToInt64Bits(value))) * Prime; return value; }
    private float Drew(float value) { Draws++; _digest = (_digest ^ unchecked((uint)BitConverter.SingleToInt32Bits(value))) * Prime; return value; }

    private void Drew(ReadOnlySpan<byte> bytes)
    {
        Draws++;
        foreach (byte b in bytes) _digest = (_digest ^ b) * Prime;
    }

    /// <inheritdoc/>
    public override int Next() => Drew(_inner.Next());
    /// <inheritdoc/>
    public override int Next(int maxValue) => Drew(_inner.Next(maxValue));
    /// <inheritdoc/>
    public override int Next(int minValue, int maxValue) => Drew(_inner.Next(minValue, maxValue));
    /// <inheritdoc/>
    public override long NextInt64() => Drew(_inner.NextInt64());
    /// <inheritdoc/>
    public override long NextInt64(long maxValue) => Drew(_inner.NextInt64(maxValue));
    /// <inheritdoc/>
    public override long NextInt64(long minValue, long maxValue) => Drew(_inner.NextInt64(minValue, maxValue));
    /// <inheritdoc/>
    public override double NextDouble() => Drew(_inner.NextDouble());
    /// <inheritdoc/>
    public override float NextSingle() => Drew(_inner.NextSingle());
    /// <inheritdoc/>
    protected override double Sample() => Drew(_inner.NextDouble());

    /// <inheritdoc/>
    public override void NextBytes(byte[] buffer)
    {
        _inner.NextBytes(buffer);
        Drew(buffer);
    }

    /// <inheritdoc/>
    public override void NextBytes(Span<byte> buffer)
    {
        _inner.NextBytes(buffer);
        Drew(buffer);
    }

    /// <summary>Adds the seed, the number of draws and the running hash of the values drawn.</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Seed);
        hash.Add(Draws);
        hash.Add(_digest);
    }
}
