using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace MicroEngine.Tiled;

/// <summary>
/// A map made in the Tiled editor, read as data: its size, layers, tilesets and objects. Load it with
/// <see cref="TiledAssets.TiledMap"/>; give its tiles textures with <see cref="TiledSprites"/>.
/// </summary>
/// <remarks>
/// Positions are in map pixels: tile (0, 0) has its top-left at (0, 0). An infinite map is cropped to the cells its
/// layers use, and <see cref="Origin"/> says where that first cell was in Tiled. Only orthogonal maps load.
/// </remarks>
public sealed class TiledMap
{
    private readonly Dictionary<int, TiledTile> _tiles;
    private readonly Dictionary<int, TiledObject> _objectsById;

    internal TiledMap(string path, int width, int height, int tileWidth, int tileHeight, (int X, int Y) origin, Color? background,
        string className, TiledProperties properties, IReadOnlyList<TiledLayer> layers, IReadOnlyList<TiledTileset> tilesets)
    {
        (Path, Width, Height, TileWidth, TileHeight, Origin, Background, Class, Properties, Layers, Tilesets) =
            (path, width, height, tileWidth, tileHeight, origin, background, className, properties, layers, tilesets);
        _tiles = [];
        foreach (var tileset in tilesets)
            foreach (var tile in tileset.Tiles) _tiles[tile.Gid] = tile;
        _objectsById = [];
        foreach (var o in Objects) _objectsById[o.Id] = o;
    }

    /// <summary>The map's content path.</summary>
    public string Path { get; }
    /// <summary>Width in tiles.</summary>
    public int Width { get; }
    /// <summary>Height in tiles.</summary>
    public int Height { get; }
    /// <summary>Width of a grid cell in pixels.</summary>
    public int TileWidth { get; }
    /// <summary>Height of a grid cell in pixels.</summary>
    public int TileHeight { get; }
    /// <summary>Width of the map in pixels.</summary>
    public int PixelWidth => Width * TileWidth;
    /// <summary>Height of the map in pixels.</summary>
    public int PixelHeight => Height * TileHeight;
    /// <summary>Where tile (0, 0) was in Tiled, in tiles: (0, 0) except for an infinite map, which is cropped to the cells it uses.</summary>
    public (int X, int Y) Origin { get; }
    /// <summary>The point where parallax shifts nothing, in map pixels (Tiled's parallax origin).</summary>
    public Vector2 ParallaxOrigin { get; init; }

    /// <summary>
    /// How far <paramref name="layer"/> moves from its place for a view centred on <paramref name="viewCenter"/>, as
    /// Tiled shows it: (view centre − <see cref="ParallaxOrigin"/>) × (1 − the layer's parallax factor). Add it to
    /// where the layer is drawn; it is zero for layers that move with the map.
    /// </summary>
    public Vector2 ParallaxShift(TiledLayer layer, Vector2 viewCenter)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return (viewCenter - ParallaxOrigin) * (Vector2.One - layer.Parallax);
    }

    /// <summary>The background colour set in Tiled, if any.</summary>
    public Color? Background { get; }
    /// <summary>The map's class (Tiled's "Class" field); empty when unset.</summary>
    public string Class { get; }
    /// <summary>The map's custom properties.</summary>
    public TiledProperties Properties { get; }
    /// <summary>The top-level layers, bottom to top; groups hold their own (<see cref="TiledGroupLayer"/>).</summary>
    public IReadOnlyList<TiledLayer> Layers { get; }
    /// <summary>The tilesets, by increasing <see cref="TiledTileset.FirstGid"/>.</summary>
    public IReadOnlyList<TiledTileset> Tilesets { get; }

    /// <summary>Every layer, groups included, in drawing order (bottom to top, a group before its layers).</summary>
    public IEnumerable<TiledLayer> AllLayers => Flatten(Layers);

    /// <summary>Every tile layer in drawing order.</summary>
    public IEnumerable<TiledTileLayer> TileLayers => AllLayers.OfType<TiledTileLayer>();

    /// <summary>Every object layer in drawing order.</summary>
    public IEnumerable<TiledObjectLayer> ObjectLayers => AllLayers.OfType<TiledObjectLayer>();

    /// <summary>Every object of every object layer, layer by layer.</summary>
    public IEnumerable<TiledObject> Objects => ObjectLayers.SelectMany(l => l.Objects);

    private static IEnumerable<TiledLayer> Flatten(IEnumerable<TiledLayer> layers)
    {
        foreach (var layer in layers)
        {
            yield return layer;
            if (layer is TiledGroupLayer group)
                foreach (var inner in Flatten(group.Layers)) yield return inner;
        }
    }

    /// <summary>The first layer called <paramref name="name"/> of type <typeparamref name="T"/>, groups included.</summary>
    /// <exception cref="KeyNotFoundException">There is none; the message lists the map's layers.</exception>
    public T Layer<T>(string name) where T : TiledLayer =>
        AllLayers.OfType<T>().FirstOrDefault(l => l.Name == name)
        ?? throw new KeyNotFoundException($"{Path} has no {typeof(T).Name} '{name}'. Layers: {string.Join(", ", AllLayers.Select(l => $"{l.Name} ({l.GetType().Name})"))}.");

    /// <summary>The tile layer called <paramref name="name"/> (see <see cref="Layer{T}"/>).</summary>
    public TiledTileLayer TileLayer(string name) => Layer<TiledTileLayer>(name);

    /// <summary>The object layer called <paramref name="name"/> (see <see cref="Layer{T}"/>).</summary>
    public TiledObjectLayer ObjectLayer(string name) => Layer<TiledObjectLayer>(name);

    /// <summary>The tile with global id <paramref name="gid"/>, or null when no tileset holds it (0 included).</summary>
    public TiledTile? Tile(int gid) => _tiles.GetValueOrDefault(gid);

    /// <summary>The object with id <paramref name="id"/> (what an object property refers to), or null.</summary>
    public TiledObject? Object(int id) => _objectsById.GetValueOrDefault(id);

    /// <summary>
    /// A collision grid over <paramref name="layer"/> whose flags come from each tile's <paramref name="property"/>:
    /// a bool property set to true is <see cref="TileFlags.Solid"/>; a text property lists flag names separated by
    /// commas, bars or spaces (<c>solid</c>, <c>oneway</c>, <c>hazard</c>, <c>water</c>, <c>custom1</c> to
    /// <c>custom4</c>, any case); a whole number is the flags' bits. Tiles without the property have no flags.
    /// </summary>
    /// <exception cref="NotSupportedException">The map's cells are not square.</exception>
    /// <exception cref="InvalidDataException">A tile's property names an unknown flag or has another type; the message names the tile.</exception>
    public TileGrid CollisionGrid(TiledTileLayer layer, string property)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        return CollisionGrid(layer, tile => FlagsOf(tile, property));
    }

    /// <summary>A collision grid over <paramref name="layer"/> whose flags <paramref name="flagsOf"/> chooses for each tile.</summary>
    /// <exception cref="NotSupportedException">The map's cells are not square.</exception>
    public TileGrid CollisionGrid(TiledTileLayer layer, Func<TiledTile, TileFlags> flagsOf)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(flagsOf);
        if (TileWidth != TileHeight) throw new NotSupportedException($"{Path}: collision grids need square cells, not {TileWidth} × {TileHeight}.");
        var flags = new Dictionary<int, TileFlags>();
        foreach (var tile in _tiles.Values)
            if (flagsOf(tile) is var f && f != TileFlags.None) flags[tile.Gid] = f;
        return new TileGrid(layer.Tiles, TileWidth, flags);
    }

    private static TileFlags FlagsOf(TiledTile tile, string property)
    {
        if (!tile.Properties.TryGet(property, out var p)) return TileFlags.None;
        switch (p.Value)
        {
            case bool b: return b ? TileFlags.Solid : TileFlags.None;
            case int bits when p.Type == TiledPropertyType.Int: return (TileFlags)bits;
            case string text when p.Type is TiledPropertyType.String or TiledPropertyType.Enum:
                var flags = TileFlags.None;
                foreach (var name in text.Split([',', '|', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
                    flags |= name.ToLowerInvariant() switch
                    {
                        "solid" => TileFlags.Solid,
                        "oneway" => TileFlags.OneWay,
                        "hazard" => TileFlags.Hazard,
                        "water" => TileFlags.Water,
                        "custom1" => TileFlags.Custom1,
                        "custom2" => TileFlags.Custom2,
                        "custom3" => TileFlags.Custom3,
                        "custom4" => TileFlags.Custom4,
                        _ => throw new InvalidDataException($"Tile {tile.Id} of tileset '{tile.Tileset.Name}': '{property}' names an unknown flag '{name}' (solid, oneway, hazard, water, custom1 to custom4)."),
                    };
                return flags;
            default:
                throw new InvalidDataException($"Tile {tile.Id} of tileset '{tile.Tileset.Name}': '{property}' is {p.Type}; a collision property is a bool, a whole number or a list of flag names.");
        }
    }
}

/// <summary>A layer of a <see cref="TiledMap"/>: what every kind of layer has.</summary>
public abstract class TiledLayer
{
    private protected TiledLayer(TiledLayerInfo info) =>
        (Id, Name, Class, Visible, Opacity, Tint, Offset, Parallax, Properties) =
        (info.Id, info.Name, info.Class, info.Visible, info.Opacity, info.Tint, info.Offset, info.Parallax, info.Properties);

    /// <summary>The layer's id, unique in the map.</summary>
    public int Id { get; }
    /// <summary>The layer's name.</summary>
    public string Name { get; }
    /// <summary>The layer's class; empty when unset.</summary>
    public string Class { get; }
    /// <summary>Whether it is shown; a hidden group hides its layers.</summary>
    public bool Visible { get; }
    /// <summary>Opacity from 0 to 1, a group's included.</summary>
    public float Opacity { get; }
    /// <summary>The tint colour, a group's included (white when unset).</summary>
    public Color Tint { get; }
    /// <summary>Where the layer is drawn relative to the map, in pixels, a group's offset included.</summary>
    public Vector2 Offset { get; }
    /// <summary>The parallax factor, a group's included ((1, 1) moves with the map). The engine does not apply it when drawing.</summary>
    public Vector2 Parallax { get; }
    /// <summary>The layer's custom properties.</summary>
    public TiledProperties Properties { get; }

    /// <summary>The layer's name and type.</summary>
    public override string ToString() => $"{GetType().Name} '{Name}'";
}

internal sealed record TiledLayerInfo(int Id, string Name, string Class, bool Visible, float Opacity, Color Tint, Vector2 Offset, Vector2 Parallax, TiledProperties Properties);

/// <summary>A layer of tiles: one global tile id per cell (0 for none) and how each cell's tile is turned.</summary>
public sealed class TiledTileLayer : TiledLayer
{
    private readonly TileOrientation[] _orientations;

    internal TiledTileLayer(TiledLayerInfo info, TileLayer tiles, TileOrientation[] orientations) : base(info) =>
        (Tiles, _orientations) = (tiles, orientations);

    /// <summary>
    /// The global tile ids, the size of the map: <see cref="TiledMap.Tile"/> gives each one's tileset, properties and
    /// animation. Changing a cell here changes what is drawn and collided with.
    /// </summary>
    public TileLayer Tiles { get; }

    /// <summary>How the tile at (<paramref name="x"/>, <paramref name="y"/>) is mirrored or turned (none outside the map).</summary>
    public TileOrientation OrientationAt(int x, int y) => Tiles.InBounds(x, y) ? _orientations[y * Tiles.Width + x] : TileOrientation.None;

    /// <summary>Sets how the tile at (<paramref name="x"/>, <paramref name="y"/>) is mirrored or turned; ignored outside the map.</summary>
    public void SetOrientation(int x, int y, TileOrientation orientation)
    {
        if (Tiles.InBounds(x, y)) _orientations[y * Tiles.Width + x] = orientation;
    }
}

/// <summary>A layer of objects: placements, areas and points with their data.</summary>
public sealed class TiledObjectLayer : TiledLayer
{
    internal TiledObjectLayer(TiledLayerInfo info, IReadOnlyList<TiledObject> objects, Color? color) : base(info) =>
        (Objects, Color) = (objects, color);

    /// <summary>The objects in the order Tiled stores them.</summary>
    public IReadOnlyList<TiledObject> Objects { get; }
    /// <summary>The colour Tiled shows the layer's objects in, if set.</summary>
    public Color? Color { get; }
}

/// <summary>A layer showing one image.</summary>
public sealed class TiledImageLayer : TiledLayer
{
    internal TiledImageLayer(TiledLayerInfo info, string? image, bool repeatX, bool repeatY) : base(info) =>
        (Image, RepeatX, RepeatY) = (image, repeatX, repeatY);

    /// <summary>The image's content path, or null when the layer has none.</summary>
    public string? Image { get; }
    /// <summary>Whether the image repeats horizontally.</summary>
    public bool RepeatX { get; }
    /// <summary>Whether the image repeats vertically.</summary>
    public bool RepeatY { get; }
}

/// <summary>A group of layers.</summary>
public sealed class TiledGroupLayer : TiledLayer
{
    internal TiledGroupLayer(TiledLayerInfo info, IReadOnlyList<TiledLayer> layers) : base(info) => Layers = layers;

    /// <summary>The group's layers, bottom to top.</summary>
    public IReadOnlyList<TiledLayer> Layers { get; }
}

/// <summary>The shape of a <see cref="TiledObject"/>.</summary>
public enum TiledShape
{
    /// <summary>A rectangle of <see cref="TiledObject.Size"/> from <see cref="TiledObject.Position"/>.</summary>
    Rectangle,
    /// <summary>An ellipse inside the rectangle.</summary>
    Ellipse,
    /// <summary>A point: the size is zero.</summary>
    Point,
    /// <summary>A closed polygon through <see cref="TiledObject.Points"/>.</summary>
    Polygon,
    /// <summary>An open line through <see cref="TiledObject.Points"/>.</summary>
    Polyline,
    /// <summary>Text (<see cref="TiledObject.Text"/>) in the rectangle.</summary>
    Text,
    /// <summary>A tile (<see cref="TiledObject.Gid"/>) stretched to the rectangle.</summary>
    Tile,
}

/// <summary>An object placed on an object layer, or a collision shape of a tile, with its data.</summary>
/// <param name="Id">The object's id, unique in the map (0 for a tile's collision shapes).</param>
/// <param name="Name">Its name; empty when unset.</param>
/// <param name="Class">Its class (Tiled's "Class", formerly "Type"); for a tile object without one, the tile's class.</param>
/// <param name="Shape">What kind of object it is.</param>
/// <param name="Position">Where it is in map pixels, its layer's offset included: the top-left of its rectangle, the point, or the origin of its points.</param>
/// <param name="Size">Its width and height in pixels (zero for points, polygons and polylines).</param>
/// <param name="Rotation">Clockwise rotation in radians about <paramref name="Position"/>.</param>
/// <param name="Visible">Whether Tiled shows it.</param>
/// <param name="Points">The points of a polygon or polyline, relative to <paramref name="Position"/>; empty otherwise.</param>
/// <param name="Gid">For a tile object, the global id of its tile; 0 otherwise.</param>
/// <param name="Orientation">For a tile object, how its tile is mirrored.</param>
/// <param name="Text">For a text object, its text; null otherwise.</param>
/// <param name="Properties">Its custom properties; a tile object's include its tile's, its own winning.</param>
public sealed record TiledObject(int Id, string Name, string Class, TiledShape Shape, Vector2 Position, Vector2 Size, float Rotation,
    bool Visible, IReadOnlyList<Vector2> Points, int Gid, TileOrientation Orientation, string? Text, TiledProperties Properties)
{
    /// <summary>
    /// The rectangle the object covers without rotation: its rectangle, the box around its points, or an empty
    /// rectangle at a point. A tile object's rectangle goes up from its position, as Tiled draws it.
    /// </summary>
    public RectF Bounds
    {
        get
        {
            if (Points.Count > 0)
            {
                float x0 = Points.Min(p => p.X), y0 = Points.Min(p => p.Y), x1 = Points.Max(p => p.X), y1 = Points.Max(p => p.Y);
                return new RectF(Position.X + x0, Position.Y + y0, x1 - x0, y1 - y0);
            }
            return Shape == TiledShape.Tile
                ? new RectF(Position.X, Position.Y - Size.Y, Size.X, Size.Y)
                : new RectF(Position.X, Position.Y, Size.X, Size.Y);
        }
    }

    /// <summary>The centre of <see cref="Bounds"/>.</summary>
    public Vector2 Center => Bounds.Center;

    /// <summary>
    /// The axis-aligned rectangle around the object as Tiled shows it turned by <see cref="Rotation"/> about
    /// <see cref="Position"/>; <see cref="Bounds"/> when it is not turned. A quarter turn of a box gives its exact box.
    /// </summary>
    public RectF TurnedBounds
    {
        get
        {
            var b = Bounds;
            if (Rotation == 0) return b;
            var (sin, cos) = MathF.SinCos(Rotation);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var corner in (ReadOnlySpan<Vector2>)[new(b.X, b.Y), new(b.Right, b.Y), new(b.X, b.Bottom), new(b.Right, b.Bottom)])
            {
                var d = corner - Position;
                // Rounded to 1/1000 pixel so a quarter turn of a box gives whole numbers back.
                float x = MathF.Round(Position.X + d.X * cos - d.Y * sin, 3), y = MathF.Round(Position.Y + d.X * sin + d.Y * cos, 3);
                (x0, y0, x1, y1) = (MathF.Min(x0, x), MathF.Min(y0, y), MathF.Max(x1, x), MathF.Max(y1, y));
            }
            return new RectF(x0, y0, x1 - x0, y1 - y0);
        }
    }
}

/// <summary>A tileset of a <see cref="TiledMap"/>: one image cut into tiles, or a collection of images.</summary>
public sealed class TiledTileset
{
    internal TiledTileset(string name, int firstGid, int tileWidth, int tileHeight, int columns, string? image, Vector2 tileOffset,
        string className, TiledProperties properties)
    {
        (Name, FirstGid, TileWidth, TileHeight, Columns, Image, TileOffset, Class, Properties) =
            (name, firstGid, tileWidth, tileHeight, columns, image, tileOffset, className, properties);
        Tiles = [];
    }

    /// <summary>The tileset's name.</summary>
    public string Name { get; }
    /// <summary>The global id of its first tile.</summary>
    public int FirstGid { get; }
    /// <summary>Width of its tiles in pixels (the largest, for a collection of images).</summary>
    public int TileWidth { get; }
    /// <summary>Height of its tiles in pixels (the largest, for a collection of images).</summary>
    public int TileHeight { get; }
    /// <summary>Tiles per row of its image (0 for a collection of images).</summary>
    public int Columns { get; }
    /// <summary>The content path of its image, or null for a collection of images (each tile has its own).</summary>
    public string? Image { get; }
    /// <summary>Pixels every tile is drawn shifted by.</summary>
    public Vector2 TileOffset { get; }
    /// <summary>The tileset's class; empty when unset.</summary>
    public string Class { get; }
    /// <summary>The tileset's custom properties.</summary>
    public TiledProperties Properties { get; }
    /// <summary>Its tiles by increasing id: every cell of its image, or the images of a collection (whose ids may have gaps).</summary>
    public IReadOnlyList<TiledTile> Tiles { get; internal set; }

    /// <summary>Whether <paramref name="gid"/> is one of this tileset's tiles.</summary>
    public bool Contains(int gid) => Tiles.Count > 0 && gid >= FirstGid && gid <= Tiles[^1].Gid && Tiles.Any(t => t.Gid == gid);
}

/// <summary>One frame of a tile animation.</summary>
/// <param name="Gid">The global id of the tile shown.</param>
/// <param name="Seconds">How long it is shown.</param>
public readonly record struct TiledFrame(int Gid, float Seconds);

/// <summary>A tile of a tileset: where its pixels are and its data.</summary>
/// <param name="Tileset">The tileset holding it.</param>
/// <param name="Id">Its id in the tileset.</param>
/// <param name="Gid">Its global id in the map.</param>
/// <param name="Image">The content path of the image its pixels are in.</param>
/// <param name="Source">Its rectangle in that image, in pixels.</param>
/// <param name="Class">Its class; empty when unset.</param>
/// <param name="Properties">Its custom properties.</param>
/// <param name="Animation">Its animation frames; empty when it does not animate.</param>
/// <param name="Shapes">Its collision shapes drawn in Tiled's tile collision editor, in tile pixels; empty when none.</param>
public sealed record TiledTile(TiledTileset Tileset, int Id, int Gid, string Image, RectF Source, string Class, TiledProperties Properties,
    IReadOnlyList<TiledFrame> Animation, IReadOnlyList<TiledObject> Shapes)
{
    /// <summary>The length of one loop of <see cref="Animation"/> in seconds (0 when it does not animate).</summary>
    public float AnimationLength => Animation.Sum(f => f.Seconds);

    /// <summary>The global id shown <paramref name="seconds"/> into the animation (looping), or <see cref="Gid"/> when it does not animate.</summary>
    public int FrameAt(double seconds)
    {
        float length = AnimationLength;
        if (length <= 0) return Gid;
        double t = seconds % length;
        if (t < 0) t += length;
        foreach (var frame in Animation)
        {
            if (t < frame.Seconds) return frame.Gid;
            t -= frame.Seconds;
        }
        return Animation[^1].Gid;
    }
}
