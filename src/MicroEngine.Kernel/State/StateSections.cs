namespace MicroEngine.Kernel;

/// <summary>
/// The parts of the per-step state hash that modules own: each is a named section hashed after the engine's own
/// sections and before the scenes, in the order they were added. A module that holds simulation state outside scenes
/// (progress, a shared world) adds one from its <c>Configure</c>, so a deterministic run covers it without the game
/// hashing it by hand.
/// </summary>
/// <example>
/// <code>
/// public void Configure(EngineBuilder builder) =>
///     builder.StateSections.Add("mygame.economy", hash => hash.Add(_gold));
/// </code>
/// </example>
public sealed class StateSections
{
    private readonly List<(string Name, Action<StateHasher> Hash)> _sections = [];

    /// <summary>How many sections were added.</summary>
    public int Count => _sections.Count;

    /// <summary>Adds a section named <paramref name="name"/> whose state <paramref name="hash"/> writes.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">A section of that name was already added.</exception>
    public void Add(string name, Action<StateHasher> hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(hash);
        foreach (var section in _sections)
            if (section.Name == name) throw new InvalidOperationException($"The state section '{name}' was already added.");
        _sections.Add((name, hash));
    }

    /// <summary>Adds a section named <paramref name="name"/> holding the state of <paramref name="state"/>.</summary>
    public void Add(string name, IStateHashed state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Add(name, state.HashState);
    }

    /// <summary>Writes every section to <paramref name="hash"/>, each under its name, in the order they were added.</summary>
    public void HashAll(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        foreach (var (name, write) in _sections)
        {
            hash.Section(name);
            write(hash);
        }
    }
}
