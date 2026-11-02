using System.Numerics;
using MicroEngine;
using MicroEngine.World;
using MicroEngine.Input;

var layer = new TileLayer(40, 40);
var grid = new TileGrid(layer, 16, new Dictionary<int, TileFlags>()) { OutsideFlags = TileFlags.None };

// E1: thin extra solid vs fast thin body (bullet-like 2x2 box moving 7.9 px/step along Y; solid 4px thick)
var thin = new List<RectF> { new(0, 100, 200, 4) };
var bullet = new RectF(50, 90, 2, 2);
var r1 = TileMotion.Move(bullet, new Vector2(0, 15.9f), grid, thin);
Console.WriteLine($"E1 thin solid: start y=90 delta=15.9 -> y={r1.Bounds.Y} hitFloor={r1.HitFloor} (solid spans y 100..104)");

// E2: moving platform rising into a resting box: box already overlapping is ignored -> falls through/embedded
var box = new RectF(50, 80, 10, 10);           // bottom = 90
var platform = new RectF(40, 88, 40, 8);       // platform moved up into the box this step
var r2 = TileMotion.Move(box, new Vector2(0, 2), grid, new List<RectF> { platform });
Console.WriteLine($"E2 platform overlap: box bottom {r2.Bounds.Bottom} platform top {platform.Top} hitFloor={r2.HitFloor} onGround={TileMotion.IsOnGround(r2.Bounds, grid, new List<RectF>{platform})}");

// E3: tile tunneling sanity with tiles (body 2px, tile 16, delta 30)
var layer2 = new TileLayer(10, 10); layer2[2, 5] = 1;
var grid2 = new TileGrid(layer2, 16, new Dictionary<int, TileFlags> { [1] = TileFlags.Solid }) { OutsideFlags = TileFlags.None };
var r3 = TileMotion.Move(new RectF(36, 40, 2, 2), new Vector2(0, 60), grid2);
Console.WriteLine($"E3 tile: y={r3.Bounds.Y} hitFloor={r3.HitFloor} (tile spans 80..96)");

// E4: EntityList.Clear() called from inside an entity's Update
var list = new EntityList<Probe>();
list.Add(new Probe(list)); list.Add(new Probe(list));
try { list.Update(1 / 60f); Console.WriteLine("E4 Clear during Update: no exception"); }
catch (Exception e) { Console.WriteLine($"E4 Clear during Update: {e.GetType().Name}: {e.Message}"); }

// E5: one InputMap, two 'players' - keyboard and pad are merged into the same action; no per-device query
var map = InputMap.CreateDefault();
var src = new FakeInput { Keys = { Key.Left }, Pads = { PadButton.DPadRight } };
map.Sample(src);
Console.WriteLine($"E5 left={map.Down("left")} right={map.Down("right")} Axis={map.Axis("left","right")} LastDevice={map.LastDevice}");

sealed class Probe(EntityList<Probe> owner) : Entity { public override void Update(float dt) => owner.Clear(); }
sealed class FakeInput : IInputSource
{
    public HashSet<Key> Keys { get; } = [];
    public HashSet<PadButton> Pads { get; } = [];
    public bool IsKeyDown(Key key) => Keys.Contains(key);
    public bool IsPadDown(PadButton b) => Pads.Contains(b);
    public Vector2 LeftStick => Vector2.Zero;
}
