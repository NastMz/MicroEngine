using MicroEngine.Graphics;

namespace TrailDuel.Visuals;

/// <summary>
/// The game's art, built once at load by <see cref="AtlasFactory"/>: the sprite atlas and what was measured while
/// building it. The atlas is generated in code, not read from a content file, so the game disposes it in its
/// <c>Unload</c> instead of leaving it to <c>Game.Assets</c>.
/// </summary>
internal sealed class GameArt(SpriteAtlas atlas, IReadOnlyDictionary<string, int> creatureHeights) : IDisposable
{
    /// <summary>Height used for a species the atlas does not know.</summary>
    public const int DefaultCreatureHeight = 44;

    public SpriteAtlas Atlas { get; } = atlas;

    /// <summary>Drawn height of a species' battle sprite in pixels (effects aim at its middle).</summary>
    public int CreatureHeight(string speciesId) => creatureHeights.GetValueOrDefault(speciesId, DefaultCreatureHeight);

    public void Dispose() => Atlas.Dispose();
}
