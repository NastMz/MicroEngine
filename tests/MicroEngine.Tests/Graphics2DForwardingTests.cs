namespace MicroEngine.Tests;

/// <summary>
/// The legacy 2D model lives in MicroEngine.Graphics2D.dll, and MicroEngine.dll forwards every moved type there, so code
/// compiled against an earlier MicroEngine.dll still finds each type under its old assembly name.
/// </summary>
[TestClass]
public sealed class Graphics2DForwardingTests
{
    public static IEnumerable<object[]> MovedTypes =>
    [
        ["MicroEngine.Graphics.IRenderer"], ["MicroEngine.Graphics.DrawOptions"], ["MicroEngine.Graphics.BlendMode"],
        ["MicroEngine.Graphics.TextureFilter"], ["MicroEngine.Graphics.Texture"],
        ["MicroEngine.Graphics.IGraphicsDevice"], ["MicroEngine.Graphics.PixelImage"], ["MicroEngine.Graphics.PngEncoder"],
        ["MicroEngine.Graphics.Art"], ["MicroEngine.Graphics.Sprite"], ["MicroEngine.Graphics.SpriteAtlas"],
        ["MicroEngine.Graphics.SpriteAtlasBuilder"], ["MicroEngine.Graphics.AtlasOptions"], ["MicroEngine.Graphics.AtlasData"],
        ["MicroEngine.Graphics.AtlasRegion"], ["MicroEngine.Graphics.AtlasAnimation"], ["MicroEngine.Graphics.Animator"],
        ["MicroEngine.Graphics.AnimationClip"], ["MicroEngine.Graphics.AnimationDirection"], ["MicroEngine.Graphics.ParticleSystem"],
        ["MicroEngine.Graphics.ParticleSpec"], ["MicroEngine.Graphics.LightMap"], ["MicroEngine.Graphics.ParallaxLayer"],
        ["MicroEngine.Graphics.RendererExtensions"], ["MicroEngine.Graphics.PanelStyle"], ["MicroEngine.Graphics.Camera2D"],
    ];

    [TestMethod]
    [DynamicData(nameof(MovedTypes))]
    public void Each_moved_type_resolves_through_MicroEngine_dll_and_lives_in_Graphics2D(string name)
    {
        var type = Type.GetType($"{name}, MicroEngine", throwOnError: true)!;
        Assert.AreEqual("MicroEngine.Graphics2D", type.Assembly.GetName().Name, $"{name} lives in the 2D layer");
        Assert.AreEqual(name, type.FullName, "its namespace is unchanged");
        Assert.Contains(type, typeof(Core.Game).Assembly.GetForwardedTypes(), $"MicroEngine.dll forwards {name}");
    }
}
