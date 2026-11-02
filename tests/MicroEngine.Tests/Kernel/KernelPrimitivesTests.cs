using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>Lifecycle events, generational handles and the linear colour type.</summary>
[TestClass]
public sealed class KernelPrimitivesTests
{
    [TestMethod]
    public void Lifecycle_events_reach_handlers_in_subscription_order()
    {
        var lifecycle = new EngineLifecycle();
        IEngineLifecycle events = lifecycle;
        var seen = new List<string>();
        events.Suspending += () => seen.Add("suspending 1");
        events.Suspending += () => seen.Add("suspending 2");
        events.Resumed += () => seen.Add("resumed");
        events.Resized += size => seen.Add($"resized {size.Width}x{size.Height}");
        events.FocusChanged += focused => seen.Add($"focus {focused}");
        lifecycle.RaiseFocusChanged(false);
        lifecycle.RaiseSuspending();
        lifecycle.RaiseResumed();
        lifecycle.RaiseResized(new SurfaceResized(1280, 720));
        lifecycle.RaiseFocusChanged(true);
        CollectionAssert.AreEqual(new[] { "focus False", "suspending 1", "suspending 2", "resumed", "resized 1280x720", "focus True" }, seen);
    }

    [TestMethod]
    public void A_quit_request_goes_ahead_unless_a_handler_vetoes_it()
    {
        var lifecycle = new EngineLifecycle();
        Assert.IsTrue(lifecycle.RaiseQuitRequested(), "nobody listening: quit");
        int handlers = 0;
        lifecycle.QuitRequested += _ => handlers++;
        Assert.IsTrue(lifecycle.RaiseQuitRequested());
        lifecycle.QuitRequested += request => request.Veto();
        lifecycle.QuitRequested += request => { handlers++; Assert.IsTrue(request.Vetoed, "later handlers see the veto"); };
        Assert.IsFalse(lifecycle.RaiseQuitRequested());
        Assert.AreEqual(3, handlers, "every handler runs, even after a veto");
    }

    private sealed class Texture(string name) { public string Name { get; } = name; }

    [TestMethod]
    public void Handles_find_their_item_until_it_is_removed()
    {
        var pool = new HandlePool<Texture>();
        var a = pool.Add(new Texture("a"));
        var b = pool.Add(new Texture("b"));
        Assert.AreEqual("a", pool.Get(a).Name);
        Assert.IsTrue(pool.TryGet(b, out var found) && found.Name == "b");
        Assert.AreEqual(2, pool.Count);
        Assert.IsTrue(pool.Remove(a));
        Assert.IsFalse(pool.Remove(a), "removing twice");
        Assert.IsFalse(pool.Contains(a));
        Assert.AreEqual(1, pool.Count);
    }

    [TestMethod]
    public void A_reused_slot_gets_a_new_generation_so_old_handles_are_stale()
    {
        var pool = new HandlePool<Texture>();
        var old = pool.Add(new Texture("old"));
        pool.Remove(old);
        var reused = pool.Add(new Texture("new"));
        Assert.AreEqual(old.Index, reused.Index, "the slot is reused");
        Assert.AreNotEqual(old.Generation, reused.Generation);
        Assert.IsFalse(pool.TryGet(old, out _), "the old handle does not reach the new item");
        var stale = Assert.ThrowsExactly<InvalidOperationException>(() => pool.Get(old));
        StringAssert.Contains(stale.Message, "is stale");
        Assert.AreEqual("new", pool.Get(reused).Name);
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => pool.Get(Handle<Texture>.None)).Message, "refers to nothing");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => pool.Get(new Handle<Texture>(42, 1))).Message, "not issued");
        Assert.IsTrue(default(Handle<Texture>).IsNone);
        Assert.AreEqual("Texture:0#2", reused.ToString());
    }

    [TestMethod]
    public void Every_8_bit_colour_survives_the_round_trip_through_linear_light()
    {
        for (int v = 0; v < 256; v++)
        {
            var color = new Color((byte)v, (byte)(255 - v), (byte)(v / 2), (byte)v);
            Assert.AreEqual(color, ColorF.FromSrgb(color).ToSrgb(), $"value {v}");
        }
        Assert.AreEqual(0.2158605f, ColorF.FromSrgb(new Color(128, 0, 0)).R, 1e-5f, "sRGB 128 is 21% linear light");
        Assert.AreEqual(ColorF.White, ColorF.FromSrgb(Color.White));
        Assert.AreEqual(new Color(255, 0, 0), new ColorF(2f, -1f, 0).ToSrgb(), "out-of-range channels clamp");
    }

    [TestMethod]
    public void Premultiplying_scales_colour_by_opacity_and_undoing_it_restores_the_colour()
    {
        var red = new ColorF(1f, 0.5f, 0.25f, 0.5f);
        Assert.AreEqual(new ColorF(0.5f, 0.25f, 0.125f, 0.5f), red.Premultiply());
        Assert.AreEqual(red, red.Premultiply().Unpremultiply());
        Assert.AreEqual(ColorF.Transparent, new ColorF(0.3f, 0.3f, 0.3f, 0).Unpremultiply());
        // A red-to-transparent edge sampled halfway, premultiplied, stays red over white.
        var edge = ColorF.Lerp(new ColorF(1, 0, 0, 1).Premultiply(), ColorF.Transparent, 0.5f);
        var overWhite = edge + ColorF.White * (1 - edge.A);
        Assert.AreEqual(new ColorF(1f, 0.5f, 0.5f, 1f), overWhite);
        Assert.AreEqual(new ColorF(0.5f, 0.5f, 0.5f, 0.5f), ColorF.White * 0.5f);
        Assert.AreEqual(new ColorF(0.5f, 0, 0, 1), new ColorF(1, 1, 1) * new ColorF(0.5f, 0, 0));
        Assert.AreEqual(ColorF.White, ColorF.Lerp(ColorF.Black, ColorF.White, 2f), "t is clamped");
    }
}
