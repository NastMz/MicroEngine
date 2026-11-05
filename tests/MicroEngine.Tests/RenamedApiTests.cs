using System.Reflection;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace MicroEngine.Tests;

/// <summary>
/// The public names the SDK settled before its consumers moved to the new engine: the retro synthesizer has its own
/// namespace, the presentation is the only way to say how the canvas reaches the window, and the tile mover's result no
/// longer shares its name with the physics contract's. Looked up by name, so the old names are proven gone.
/// </summary>
[TestClass]
public sealed class RenamedApiTests
{
    private static readonly string[] RetroTypes = ["Synth", "SfxParams", "Wave", "Song", "Instrument"];

    [TestMethod]
    public void The_retro_synthesizer_types_live_in_MicroEngine_Audio_Retro()
    {
        var audio = typeof(MicroEngine.Audio.AudioMixer).Assembly;
        foreach (string name in RetroTypes)
        {
            var type = audio.GetType($"MicroEngine.Audio.Retro.{name}");
            Assert.IsNotNull(type, $"{name} is in MicroEngine.Audio.Retro");
            Assert.IsTrue(type.IsPublic, name);
            Assert.IsNull(audio.GetType($"MicroEngine.Audio.{name}"), $"{name} left MicroEngine.Audio");
            Assert.IsNull(typeof(Game).Assembly.GetType($"MicroEngine.Audio.{name}"), $"MicroEngine.dll no longer forwards MicroEngine.Audio.{name}");
        }
    }

    [TestMethod]
    public void CanvasScaling_and_GameSettings_Scaling_are_gone()
    {
        var graphics2D = typeof(Texture).Assembly;
        Assert.IsNull(graphics2D.GetType("MicroEngine.Graphics.CanvasScaling"));
        Assert.IsNull(typeof(Game).Assembly.GetType("MicroEngine.Graphics.CanvasScaling"), "no forward from MicroEngine.dll either");
        Assert.IsNull(typeof(GameSettings).GetProperty("Scaling"));
        Assert.IsNull(typeof(GameSettings).GetProperty("EffectivePresentation"), "the presentation is the one in effect");
    }

    [TestMethod]
    public void A_games_presentation_is_pixel_art_unless_it_chooses_another()
    {
        var property = typeof(GameSettings).GetProperty(nameof(GameSettings.Presentation))!;
        Assert.AreEqual(typeof(Presentation), property.PropertyType);
        Assert.AreEqual(NullabilityState.NotNull, new NullabilityInfoContext().Create(property).ReadState, "never null");
        Assert.AreSame(Presentation.PixelArt, new GameSettings("pixel").Presentation);
        Assert.AreSame(Presentation.HighResolution2D, new GameSettings("hd", 1280, 720) { Presentation = Presentation.HighResolution2D }.Presentation);
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new GameSettings("none") { Presentation = null! });
    }

    [TestMethod]
    public void The_tile_movers_result_is_TileMoveResult()
    {
        var tiles = typeof(TileMotion).Assembly;
        var result = tiles.GetType("MicroEngine.World.TileMoveResult");
        Assert.IsNotNull(result);
        Assert.AreEqual(result, typeof(TileMotion).GetMethod(nameof(TileMotion.Move))!.ReturnType);
        Assert.IsNull(tiles.GetType("MicroEngine.World.MoveResult"), "only the physics contract has a MoveResult");
        Assert.IsNull(typeof(Game).Assembly.GetType("MicroEngine.World.MoveResult"), "no forward from MicroEngine.dll either");
        Assert.IsNotNull(typeof(MicroEngine.Physics.MoveResult));
    }
}
