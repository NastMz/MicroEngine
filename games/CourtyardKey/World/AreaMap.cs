using CourtyardKey.Content;

namespace CourtyardKey.World;

/// <summary>
/// One area's rooms. Pristine rooms (never modified) answer neighbour collision queries; the hero
/// always plays in a fresh copy so each visit starts from the authored state.
/// </summary>
public sealed class AreaMap
{
    /// <summary>The vale, parsed once: its pristine rooms never change, so every play scene shares them.</summary>
    public static readonly AreaMap Overworld = new(Content.Overworld.Create());
    /// <summary>The Sunken Courtyard, parsed once and shared like <see cref="Overworld"/>.</summary>
    public static readonly AreaMap Dungeon = new(Content.Dungeon.Create());

    private readonly Room?[,] _pristine;

    public AreaMap(AreaDef def)
    {
        Def = def;
        _pristine = new Room?[def.Width, def.Height];
        for (int x = 0; x < def.Width; x++)
            for (int y = 0; y < def.Height; y++)
                if (def.Room(x, y) is not null) _pristine[x, y] = new Room(def, x, y);
    }

    public AreaDef Def { get; }
    public string Id => Def.Id;

    public Room? Pristine(int x, int y) => x >= 0 && y >= 0 && x < Def.Width && y < Def.Height ? _pristine[x, y] : null;
    public Room Fresh(int x, int y) => new(Def, x, y);
    public bool Exists(int x, int y) => Pristine(x, y) is not null;
}
