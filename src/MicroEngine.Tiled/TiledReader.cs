using System.Numerics;
using DotTiled;
using DotTiled.Serialization;
using MicroEngine.World;
using DT = DotTiled;

namespace MicroEngine.Tiled;

/// <summary>Turns DotTiled's model of a map into the engine's types. Nothing of DotTiled leaves this file.</summary>
internal static class TiledReader
{
    /// <summary>Reads the map at <paramref name="path"/>; <paramref name="read"/> returns the text of any content file.</summary>
    public static TiledMap Read(string path, Func<string, string> read)
    {
        var files = new Files(read);
        DT.Map map;
        try
        {
            using var reader = new MapReader(read(path), files.TilesetsFrom(Folder(path)), files.TemplatesFrom(Folder(path)), Files.NoCustomTypes);
            map = reader.ReadMap();
        }
        catch (Exception e) when (e is System.Xml.XmlException or System.Text.Json.JsonException or FormatException or NotSupportedException or KeyNotFoundException or ArgumentException)
        {
            throw new InvalidDataException($"{path}: not a Tiled map Tiled can read: {e.Message}", e);
        }
        if (map.Orientation != MapOrientation.Orthogonal)
            throw new NotSupportedException($"{path}: {map.Orientation} maps are not supported; only orthogonal maps load.");
        return new Builder(path, map, files).Build();
    }

    /// <summary>
    /// The files a map references, each resolved against the folder of the file that names it (DotTiled 1.0.0's own
    /// loader resolves a template's tileset against the map's folder). Templates are read once; tilesets are read for
    /// every reference, because the reader writes each reference's first global id into the tileset it gets.
    /// </summary>
    private sealed class Files(Func<string, string> read)
    {
        public static Optional<ICustomTypeDefinition> NoCustomTypes(string name) => Optional.Empty;

        private readonly Dictionary<string, DT.Template> _templates = new(StringComparer.Ordinal);

        public Func<string, DT.Tileset> TilesetsFrom(string folder) => source =>
        {
            string file = Join(folder, source);
            using var reader = new TilesetReader(read(file), TilesetsFrom(Folder(file)), TemplatesFrom(Folder(file)), NoCustomTypes);
            return reader.ReadTileset();
        };

        public Func<string, DT.Template> TemplatesFrom(string folder) => source =>
        {
            string file = Join(folder, source);
            if (_templates.TryGetValue(file, out var template)) return template;
            using var reader = new TemplateReader(read(file), TilesetsFrom(Folder(file)), TemplatesFrom(Folder(file)), NoCustomTypes);
            return _templates[file] = reader.ReadTemplate();
        };

        /// <summary>The template at <paramref name="source"/> relative to <paramref name="folder"/>, if it was read.</summary>
        public DT.Template? Template(string folder, string source) => _templates.GetValueOrDefault(Join(folder, source));
    }

    private static string Folder(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash < 0 ? "" : path[..slash];
    }

    private static string Join(string folder, string relative) =>
        MicroEngine.Content.AssetPath.Normalize(folder.Length == 0 ? relative : folder + "/" + relative);

    private static T Or<T>(Optional<T> optional, T fallback) => optional.HasValue ? optional.Value : fallback;

    private static Color ToColor(TiledColor c) => new(c.R, c.G, c.B, c.A);

    private static TileOrientation ToOrientation(FlippingFlags flags) =>
        ((flags & FlippingFlags.FlippedHorizontally) != 0 ? TileOrientation.FlipX : 0)
        | ((flags & FlippingFlags.FlippedVertically) != 0 ? TileOrientation.FlipY : 0)
        | ((flags & FlippingFlags.FlippedDiagonally) != 0 ? TileOrientation.Diagonal : 0);

    private sealed class Builder(string path, DT.Map map, Files files)
    {
        private readonly string _folder = Folder(path);
        private readonly List<TiledTileset> _tilesets = [];
        private readonly Dictionary<int, TiledTile> _tiles = [];
        private int _originX, _originY, _width, _height;

        public TiledMap Build()
        {
            foreach (var tileset in map.Tilesets.OrderBy(t => Or(t.FirstGID, 1u))) _tilesets.Add(Tileset(tileset));
            (_originX, _originY, _width, _height) = map.Infinite ? UsedBounds(map.Layers) : (0, 0, (int)map.Width, (int)map.Height);
            var root = new TiledLayerInfo(0, "", "", true, 1, Color.White, Vector2.Zero, Vector2.One, TiledProperties.Empty);
            var layers = map.Layers.Select(l => Layer(l, root)).ToList();
            return new TiledMap(path, _width, _height, (int)map.TileWidth, (int)map.TileHeight, (_originX, _originY),
                map.BackgroundColor is { A: > 0 } bg ? ToColor(bg) : null,
                Or(map.Class, ""), Properties(map.Properties), layers, _tilesets)
            {
                // An infinite map is cropped, so its parallax origin moves with it.
                ParallaxOrigin = new Vector2(map.ParallaxOriginX - _originX * (int)map.TileWidth, map.ParallaxOriginY - _originY * (int)map.TileHeight),
            };
        }

        // ---- Tilesets ----

        private TiledTileset Tileset(DT.Tileset t)
        {
            // An external tileset's files are relative to the tileset, an embedded one's to the map.
            string folder = t.Source.HasValue ? Folder(Join(_folder, t.Source.Value)) : _folder;
            string? image = t.Image.HasValue ? Join(folder, t.Image.Value.Source.Value) : null;
            var offset = t.TileOffset.HasValue ? new Vector2(t.TileOffset.Value.X, t.TileOffset.Value.Y) : Vector2.Zero;
            int firstGid = (int)Or(t.FirstGID, 1u);
            var tileset = new TiledTileset(Or(t.Name, ""), firstGid, (int)t.TileWidth, (int)t.TileHeight, (int)t.Columns, image, offset,
                Or(t.Class, ""), Properties(t.Properties));
            var described = t.Tiles.ToDictionary(x => (int)x.ID);
            // A tileset cut from one image has TileCount tiles; a collection of images has the ids it lists (maybe with gaps).
            var ids = image is not null ? Enumerable.Range(0, (int)t.TileCount) : described.Keys.Order();
            var tiles = new List<TiledTile>();
            foreach (int id in ids)
            {
                described.TryGetValue(id, out var d);
                string tileImage = image ?? (d?.Image.HasValue == true ? Join(folder, d.Image.Value.Source.Value) : "");
                RectF source;
                if (image is not null)
                {
                    // The cell of the image: columns, margin around the image and spacing between tiles.
                    int columns = Math.Max(1, (int)t.Columns), tw = (int)t.TileWidth, th = (int)t.TileHeight;
                    int margin = (int)t.Margin, spacing = (int)t.Spacing;
                    source = new RectF(margin + id % columns * (tw + spacing), margin + id / columns * (th + spacing), tw, th);
                }
                else if (d is not null && d.Image.HasValue)
                {
                    var img = d.Image.Value;
                    float w = d.Width > 0 ? d.Width : img.Width.HasValue ? img.Width.Value : 0, h = d.Height > 0 ? d.Height : img.Height.HasValue ? img.Height.Value : 0;
                    source = new RectF(d.X, d.Y, w, h);
                }
                else source = default;
                var animation = d is null ? [] : d.Animation.Select(f => new TiledFrame(firstGid + (int)f.TileID, f.Duration / 1000f)).ToList();
                var shapes = d?.ObjectLayer is { HasValue: true } group ? group.Value.Objects.Select(o => Object(o, Vector2.Zero, isShape: true)).ToList() : [];
                var tile = new TiledTile(tileset, id, firstGid + id, tileImage, source, d is null ? "" : d.Type, d is null ? TiledProperties.Empty : Properties(d.Properties),
                    animation, shapes);
                tiles.Add(tile);
                _tiles[firstGid + id] = tile;
            }
            tileset.Tiles = tiles;
            return tileset;
        }

        // ---- Layers ----

        private TiledLayerInfo Info(BaseLayer l, TiledLayerInfo parent)
        {
            var tint = l.TintColor.HasValue ? ToColor(l.TintColor.Value) : Color.White;
            var combined = new Color((byte)(tint.R * parent.Tint.R / 255), (byte)(tint.G * parent.Tint.G / 255), (byte)(tint.B * parent.Tint.B / 255), (byte)(tint.A * parent.Tint.A / 255));
            return new TiledLayerInfo((int)l.ID, l.Name, Or(l.Class, ""), l.Visible && parent.Visible, l.Opacity * parent.Opacity, combined,
                parent.Offset + new Vector2(l.OffsetX, l.OffsetY), parent.Parallax * new Vector2(l.ParallaxX, l.ParallaxY), Properties(l.Properties));
        }

        private TiledLayer Layer(BaseLayer l, TiledLayerInfo parent)
        {
            var info = Info(l, parent);
            return l switch
            {
                DT.TileLayer t => TileLayer(t, info),
                ObjectLayer o => new TiledObjectLayer(info, o.Objects.Select(x => Object(x, info.Offset, isShape: false)).ToList(),
                    o.Color.HasValue ? ToColor(o.Color.Value) : null),
                ImageLayer i => new TiledImageLayer(info, i.Image.HasValue ? Join(_folder, i.Image.Value.Source.Value) : null, Or(i.RepeatX, false), Or(i.RepeatY, false)),
                Group g => new TiledGroupLayer(info, g.Layers.Select(x => Layer(x, info)).ToList()),
                _ => throw new NotSupportedException($"{path}: layer '{l.Name}' is a {l.GetType().Name}, which is not supported."),
            };
        }

        private TiledTileLayer TileLayer(DT.TileLayer t, TiledLayerInfo info)
        {
            var tiles = new World.TileLayer(_width, _height);
            var orientations = new TileOrientation[_width * _height];
            void Put(int x, int y, uint gid, FlippingFlags flags)
            {
                x -= _originX;
                y -= _originY;
                if (gid == 0 || x < 0 || y < 0 || x >= _width || y >= _height) return;
                tiles[x, y] = (int)gid;
                orientations[y * _width + x] = ToOrientation(flags);
            }
            if (!t.Data.HasValue) return new TiledTileLayer(info, tiles, orientations);
            var data = t.Data.Value;
            if (data.Chunks.HasValue)
            {
                foreach (var chunk in data.Chunks.Value)
                    for (int i = 0; i < chunk.GlobalTileIDs.Length; i++)
                        Put(chunk.X + i % (int)chunk.Width, chunk.Y + i / (int)chunk.Width, chunk.GlobalTileIDs[i], chunk.FlippingFlags[i]);
            }
            else if (data.GlobalTileIDs.HasValue)
            {
                var gids = data.GlobalTileIDs.Value;
                var flags = data.FlippingFlags.HasValue ? data.FlippingFlags.Value : null;
                int width = (int)t.Width;
                for (int i = 0; i < gids.Length; i++)
                    Put(Or(t.X, 0) + i % width, Or(t.Y, 0) + i / width, gids[i], flags is null ? FlippingFlags.None : flags[i]);
            }
            return new TiledTileLayer(info, tiles, orientations);
        }

        private static (int X, int Y, int Width, int Height) UsedBounds(IEnumerable<BaseLayer> layers)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            void Visit(IEnumerable<BaseLayer> ls)
            {
                foreach (var l in ls)
                {
                    if (l is Group g) Visit(g.Layers);
                    if (l is not DT.TileLayer { Data.HasValue: true } t || !t.Data.Value.Chunks.HasValue) continue;
                    foreach (var c in t.Data.Value.Chunks.Value)
                        for (int i = 0; i < c.GlobalTileIDs.Length; i++)
                        {
                            if (c.GlobalTileIDs[i] == 0) continue;
                            int x = c.X + i % (int)c.Width, y = c.Y + i / (int)c.Width;
                            (x0, y0, x1, y1) = (Math.Min(x0, x), Math.Min(y0, y), Math.Max(x1, x), Math.Max(y1, y));
                        }
                }
            }
            Visit(layers);
            return x0 == int.MaxValue ? (0, 0, 0, 0) : (x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        // ---- Objects ----

        private TiledObject Object(DT.Object o, Vector2 layerOffset, bool isShape)
        {
            var shift = isShape ? Vector2.Zero : new Vector2(-_originX * (int)map.TileWidth, -_originY * (int)map.TileHeight);
            var position = new Vector2(o.X, o.Y) + layerOffset + shift;
            var size = new Vector2(o.Width, o.Height);
            IReadOnlyList<Vector2> points = [];
            int gid = 0;
            var orientation = TileOrientation.None;
            string? text = null;
            string className = o.Type;
            var properties = Properties(o.Properties);
            // An instance of a template has the template's properties, its own winning. DotTiled 1.0.0 merges them for
            // TMX but replaces them for TMJ, so they are merged here from the template read (a no-op for TMX).
            if (o.Template.HasValue && files.Template(_folder, o.Template.Value) is { } template)
                properties = new TiledProperties(Properties(template.Object.Properties).Concat(properties));
            TiledShape shape;
            switch (o)
            {
                case EllipseObject: shape = TiledShape.Ellipse; break;
                case PointObject: shape = TiledShape.Point; size = Vector2.Zero; break;
                case PolygonObject p: shape = TiledShape.Polygon; points = p.Points.ToList(); size = Vector2.Zero; break;
                case PolylineObject p: shape = TiledShape.Polyline; points = p.Points.ToList(); size = Vector2.Zero; break;
                case TextObject t: shape = TiledShape.Text; text = t.Text; break;
                case TileObject t:
                    shape = TiledShape.Tile;
                    gid = (int)t.GID;
                    orientation = ToOrientation(t.FlippingFlags);
                    if (_tiles.TryGetValue(gid, out var tile))
                    {
                        if (className.Length == 0) className = tile.Class;
                        properties = new TiledProperties(tile.Properties.Concat(properties));
                    }
                    break;
                default: shape = TiledShape.Rectangle; break;
            }
            return new TiledObject(isShape ? 0 : (int)o.ID.Value, o.Name, className, shape, position, size, o.Rotation * MathF.PI / 180,
                o.Visible, points, gid, orientation, text, properties);
        }

        // ---- Properties ----

        private TiledProperties Properties(IEnumerable<IProperty> properties) =>
            properties is null ? TiledProperties.Empty : new TiledProperties(properties.Select(Property));

        private TiledProperty Property(IProperty p) => p switch
        {
            StringProperty s => new TiledProperty(s.Name, TiledPropertyType.String, s.Value),
            IntProperty i => new TiledProperty(i.Name, TiledPropertyType.Int, i.Value),
            FloatProperty f => new TiledProperty(f.Name, TiledPropertyType.Float, f.Value),
            BoolProperty b => new TiledProperty(b.Name, TiledPropertyType.Bool, b.Value),
            ColorProperty c => new TiledProperty(c.Name, TiledPropertyType.Color, c.Value.HasValue ? ToColor(c.Value.Value) : default(Color)),
            FileProperty f => new TiledProperty(f.Name, TiledPropertyType.File, f.Value.Length == 0 ? "" : Join(_folder, f.Value)),
            ObjectProperty o => new TiledProperty(o.Name, TiledPropertyType.Object, (int)o.Value),
            EnumProperty e => new TiledProperty(e.Name, TiledPropertyType.Enum, string.Join(",", e.Value), e.PropertyType),
            ClassProperty c => new TiledProperty(c.Name, TiledPropertyType.Class, Properties(c.Value), c.PropertyType),
            _ => throw new NotSupportedException($"{path}: property '{p.Name}' has the unsupported type {p.Type}."),
        };
    }
}
