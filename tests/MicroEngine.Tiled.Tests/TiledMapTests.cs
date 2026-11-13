using System.IO.Compression;
using System.Numerics;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace MicroEngine.Tiled.Tests;

[TestClass]
public sealed class TiledMapTests
{
    private static readonly string Maps = Path.Combine(AppContext.BaseDirectory, "Maps");

    private static AssetCache Assets(bool pixels = false) => new(new HeadlessGraphicsDevice { KeepsPixels = pixels }, Maps);

    [TestMethod]
    [DataRow("maps/level.tmx")]
    [DataRow("maps/level.tmj")]
    public void The_map_its_tilesets_and_its_tile_layer_read_the_same_from_both_formats(string path)
    {
        var map = Assets().TiledMap(path);
        Assert.AreEqual(path, map.Path);
        Assert.AreEqual((4, 3, 16, 16, 64, 48), (map.Width, map.Height, map.TileWidth, map.TileHeight, map.PixelWidth, map.PixelHeight));
        Assert.AreEqual((0, 0), map.Origin);
        Assert.AreEqual(new Color(0x20, 0x30, 0x40), map.Background);
        Assert.AreEqual("dungeon", map.Class);
        Assert.AreEqual("cave", map.Properties.String("music"));
        CollectionAssert.AreEqual(new[] { "ground", "decor", "entities", "spawns", "sky" }, map.AllLayers.Select(l => l.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "ground", "decor", "entities", "sky" }, map.Layers.Select(l => l.Name).ToArray());

        var ground = map.TileLayer("ground");
        int[] gids = [3, 0, 0, 0, 4, 0, 0, 5, 1, 1, 2, 2];
        for (int i = 0; i < gids.Length; i++) Assert.AreEqual(gids[i], ground.Tiles[i % 4, i / 4], $"cell {i}");
        Assert.AreEqual(TileOrientation.Diagonal, ground.OrientationAt(0, 0));
        Assert.AreEqual(TileOrientation.FlipX, ground.OrientationAt(1, 2));
        Assert.AreEqual(TileOrientation.FlipX | TileOrientation.FlipY, ground.OrientationAt(3, 2));
        Assert.AreEqual(TileOrientation.None, ground.OrientationAt(0, 2));

        var terrain = map.Tilesets[0];
        Assert.AreEqual(("terrain", 1, 16, 16, 2, "tilesets/terrain.png"), (terrain.Name, terrain.FirstGid, terrain.TileWidth, terrain.TileHeight, terrain.Columns, terrain.Image));
        Assert.AreEqual("cave", terrain.Properties.String("biome"));
        Assert.HasCount(4, terrain.Tiles);
        Assert.AreEqual(new RectF(16, 16, 16, 16), map.Tile(4)!.Source, $"{map.Tile(2)!.Source} {map.Tile(3)!.Source} {map.Tile(4)!.Source}");
        Assert.AreEqual("lava", map.Tile(4)!.Class);
        CollectionAssert.AreEqual(new[] { new TiledFrame(3, 0.1f), new TiledFrame(4, 0.3f) }, map.Tile(3)!.Animation.ToArray());
        var props = map.Tilesets[1];
        Assert.AreEqual((5, (string?)null), (props.FirstGid, props.Image));
        var chest = map.Tile(5)!;
        Assert.AreEqual(("props/chest.png", new RectF(0, 0, 16, 32), "chest", 10), (chest.Image, chest.Source, chest.Class, chest.Properties.Int("loot")));
        Assert.IsNull(map.Tile(0));
        Assert.IsNull(map.Tile(6), "a gap in the collection's ids");
        Assert.AreEqual((7, "props/chest.png"), (map.Tile(7)!.Gid, map.Tile(7)!.Image));
        CollectionAssert.AreEqual(new[] { 0, 2 }, props.Tiles.Select(t => t.Id).ToArray());
        Assert.IsTrue(props.Contains(7));
        Assert.IsFalse(props.Contains(6));
        Assert.IsNull(map.Tile(8));

        var sky = map.Layer<TiledImageLayer>("sky");
        Assert.AreEqual(("props/chest.png", true, false, 0.5f), (sky.Image, sky.RepeatX, sky.RepeatY, sky.Opacity));
    }

    [TestMethod]
    [DataRow("maps/level.tmx")]
    [DataRow("maps/level.tmj")]
    public void Objects_keep_their_shape_data_and_typed_properties_with_their_layers_offsets(string path)
    {
        var map = Assets().TiledMap(path);
        var spawns = map.ObjectLayer("spawns");
        Assert.AreEqual(new Vector2(8, 4), spawns.Offset, "the group's offset plus the layer's");
        Assert.HasCount(9, spawns.Objects);
        var offset = new Vector2(8, 4);

        var door = map.Object(1)!;
        Assert.AreEqual(("door", "door", TiledShape.Rectangle), (door.Name, door.Class, door.Shape));
        Assert.AreEqual(new Vector2(16, 16) + offset, door.Position);
        Assert.AreEqual(new Vector2(16, 32), door.Size);
        Assert.AreEqual(MathF.PI / 2, door.Rotation, 1e-5f);
        Assert.AreEqual(new RectF(24, 20, 16, 32), door.Bounds);
        var p = door.Properties;
        Assert.AreEqual(("cave", true, 3, 1.5f, 2), (p.String("target"), p.Bool("locked"), p.Int("cost"), p.Float("speed"), p.Int("partner")));
        Assert.AreEqual(3f, p.Float("cost"), "a whole number reads as a number");
        Assert.AreEqual(new Color(0xff, 0x00, 0xff, 0x80), p.Color("glow"));
        Assert.AreEqual("props/chest.png", p.String("sprite"), "a file property is a content path");
        Assert.AreEqual(TiledPropertyType.File, p["sprite"].Type);
        Assert.AreSame(map.Object(2), map.Object(p.Int("partner")));

        Assert.AreEqual((TiledShape.Point, new Vector2(32, 40) + offset, Vector2.Zero), (map.Object(2)!.Shape, map.Object(2)!.Position, map.Object(2)!.Size));
        Assert.AreEqual(TiledShape.Ellipse, map.Object(3)!.Shape);
        var polygon = map.Object(4)!;
        Assert.AreEqual(TiledShape.Polygon, polygon.Shape);
        CollectionAssert.AreEqual(new[] { new Vector2(0, 0), new Vector2(8, 0), new Vector2(8, 8) }, polygon.Points.ToArray());
        Assert.AreEqual(new RectF(18, 14, 8, 8), polygon.Bounds);
        Assert.AreEqual((TiledShape.Polyline, false), (map.Object(5)!.Shape, map.Object(5)!.Visible));
        Assert.AreEqual((TiledShape.Text, "Hello"), (map.Object(6)!.Shape, map.Object(6)!.Text));

        var tile = map.Object(7)!;
        Assert.AreEqual((TiledShape.Tile, 5, TileOrientation.FlipX), (tile.Shape, tile.Gid, tile.Orientation));
        Assert.AreEqual("chest", tile.Class, "a tile object without a class takes its tile's");
        Assert.AreEqual(10, tile.Properties.Int("loot"), "and its tile's properties");
        Assert.AreEqual(new RectF(56, 20, 16, 32), tile.Bounds, "a tile object's rectangle goes up from its position");

        var enemy = map.Object(8)!;
        Assert.AreEqual(("enemy", "enemy", new Vector2(8, 8)), (enemy.Name, enemy.Class, enemy.Size), "from the template");
        Assert.AreEqual(5, enemy.Properties.Int("hp"), "the instance's value wins");
        Assert.AreEqual("slime", enemy.Properties.String("kind"), "the template's other properties stay");

        var torch = map.Object(9)!;
        Assert.AreEqual(("torch", TiledShape.Tile, 2), (torch.Name, torch.Shape, torch.Gid), "a template whose tileset path is relative to the template's folder");
        Assert.HasCount(9, map.Objects);
    }

    [TestMethod]
    public void A_class_property_holds_its_members()
    {
        var stats = Assets().TiledMap("maps/level.tmx").Object(1)!.Properties;
        Assert.AreEqual(("Stats", TiledPropertyType.Class), (stats["stats"].CustomType, stats["stats"].Type));
        Assert.AreEqual(7, stats.Class("stats").Int("hp"));
    }

    [TestMethod]
    public void Property_getters_give_the_fallback_when_missing_and_refuse_another_type()
    {
        var p = Assets().TiledMap("maps/level.tmx").Object(1)!.Properties;
        Assert.AreEqual((9, "none", false), (p.Int("missing", 9), p.String("missing", "none"), p.Bool("missing")));
        Assert.AreSame(TiledProperties.Empty, p.Class("missing"));
        var e = Assert.ThrowsExactly<InvalidCastException>(() => p.Int("target"));
        StringAssert.Contains(e.Message, "'target' is String");
        var missing = Assert.ThrowsExactly<KeyNotFoundException>(() => p["nothing"]);
        StringAssert.Contains(missing.Message, "cost");
    }

    [TestMethod]
    public void A_collision_grid_reads_its_flags_from_the_tile_property_the_game_names()
    {
        var map = Assets().TiledMap("maps/level.tmj");
        var grid = map.CollisionGrid(map.TileLayer("ground"), "collision");
        Assert.AreEqual(16, grid.TileSize);
        Assert.AreEqual(TileFlags.Solid, grid.FlagsAt(0, 2), "gid 1: solid");
        Assert.AreEqual(TileFlags.Solid, grid.FlagsAt(1, 2), "a flipped tile keeps its flags");
        Assert.AreEqual(TileFlags.OneWay, grid.FlagsAt(2, 2));
        Assert.AreEqual(TileFlags.Solid | TileFlags.Hazard, grid.FlagsAt(0, 1), "gid 4: 'solid, hazard'");
        Assert.AreEqual(TileFlags.None, grid.FlagsAt(0, 0), "gid 3 has no collision property");
        Assert.AreEqual(TileFlags.None, grid.FlagsAt(1, 0), "an empty cell");
        Assert.AreEqual(TileFlags.Solid, grid.FlagsAt(-1, 0), "outside the map is solid, as every TileGrid");

        map.TileLayer("ground").Tiles[1, 0] = 1;
        Assert.AreEqual(TileFlags.Solid, grid.FlagsAt(1, 0), "the grid reads the layer, so changed cells collide at once");

        var custom = map.CollisionGrid(map.TileLayer("ground"), t => t.Class == "lava" ? TileFlags.Hazard : TileFlags.None);
        Assert.AreEqual(TileFlags.Hazard, custom.FlagsAt(0, 1));
        Assert.AreEqual(TileFlags.None, custom.FlagsAt(0, 2));
    }

    [TestMethod]
    public void A_collision_property_naming_an_unknown_flag_is_refused_with_the_tile()
    {
        var map = Assets().TiledMap("maps/level.tmx");
        var e = Assert.ThrowsExactly<InvalidDataException>(() => map.CollisionGrid(map.TileLayer("ground"), "kind"));
        StringAssert.Contains(e.Message, "Tile 0 of tileset 'props'");
        StringAssert.Contains(e.Message, "'treasure'");
        var b = Assert.ThrowsExactly<InvalidDataException>(() => map.CollisionGrid(map.TileLayer("ground"), "bonus"));
        StringAssert.Contains(b.Message, "'bonus' is Float");
    }

    [TestMethod]
    public void An_infinite_map_is_cropped_to_the_cells_it_uses()
    {
        var map = Assets().TiledMap("maps/infinite.tmj");
        Assert.AreEqual((4, 3), (map.Width, map.Height), "cells (-1, -1) to (2, 1)");
        Assert.AreEqual((-1, -1), map.Origin);
        var ground = map.TileLayer("ground");
        Assert.AreEqual(1, ground.Tiles[0, 0]);
        Assert.AreEqual(2, ground.Tiles[3, 2]);
        int used = 0;
        for (int y = 0; y < 3; y++) for (int x = 0; x < 4; x++) if (ground.Tiles[x, y] != 0) used++;
        Assert.AreEqual(2, used);
        Assert.AreEqual(new Vector2(16, 16), map.Object(1)!.Position, "objects move with the crop");
    }

    [TestMethod]
    public void Maps_that_are_not_orthogonal_are_refused_by_name()
    {
        var e = Assert.ThrowsExactly<AssetLoadException>(() => Assets().TiledMap("maps/isometric.tmx"));
        StringAssert.Contains(e.Message, "maps/isometric.tmx");
        StringAssert.Contains(e.Message, "orthogonal");
    }

    [TestMethod]
    public void A_missing_tileset_is_reported_with_the_map_and_the_file()
    {
        var folder = Directory.CreateTempSubdirectory("tiled-missing");
        try
        {
            Directory.CreateDirectory(Path.Combine(folder.FullName, "maps"));
            File.Copy(Path.Combine(Maps, "maps/level.tmx"), Path.Combine(folder.FullName, "maps/level.tmx"));
            var assets = new AssetCache(new HeadlessGraphicsDevice(), folder.FullName);
            var e = Assert.ThrowsExactly<AssetLoadException>(() => assets.TiledMap("maps/level.tmx"));
            StringAssert.Contains(e.Message, "maps/level.tmx");
            StringAssert.Contains(e.Message, "tilesets/terrain.tsx");
        }
        finally { folder.Delete(recursive: true); }
    }

    [TestMethod]
    public void A_map_loads_once_per_scope_and_from_an_archive()
    {
        var assets = Assets();
        Assert.AreSame(assets.TiledMap("maps/level.tmj"), assets.TiledMap("maps/level.tmj"));

        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var file in Directory.EnumerateFiles(Maps, "*", SearchOption.AllDirectories))
                archive.CreateEntryFromFile(file, Path.GetRelativePath(Maps, file).Replace('\\', '/'));
        zip.Position = 0;
        using var store = new AssetStore(new ZipContentSource(zip, "maps.zip", leaveOpen: true), new AssetLoaderRegistry().AddTiledMaps());
        var map = store.Get(store.Root.Load<TiledMap>(TiledAssets.Kind, "maps/level.tmx"));
        Assert.AreEqual(5, map.Object(8)!.Properties.Int("hp"), "the tileset and the template come from the archive too");
    }

    [TestMethod]
    public void A_tile_animation_loops_over_its_frames()
    {
        var tile = Assets().TiledMap("maps/level.tmj").Tile(3)!;
        Assert.AreEqual(0.4f, tile.AnimationLength, 1e-6f);
        Assert.AreEqual(3, tile.FrameAt(0));
        Assert.AreEqual(3, tile.FrameAt(0.099));
        Assert.AreEqual(4, tile.FrameAt(0.11));
        Assert.AreEqual(4, tile.FrameAt(0.39));
        Assert.AreEqual(3, tile.FrameAt(0.41));
        Assert.AreEqual(1, Assets().TiledMap("maps/level.tmj").Tile(1)!.FrameAt(5), "a tile that does not animate");
    }
}
