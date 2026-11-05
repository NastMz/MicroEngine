using System.Runtime.CompilerServices;

// The backend-free math and colour types moved to MicroEngine.Kernel.dll with their namespace unchanged, so source
// compiles as before; these forwards keep code compiled against an earlier MicroEngine.dll finding them.
[assembly: TypeForwardedTo(typeof(MicroEngine.Color))]
[assembly: TypeForwardedTo(typeof(MicroEngine.RectF))]
[assembly: TypeForwardedTo(typeof(MicroEngine.MathX))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Ease))]

// The 2D camera moved to MicroEngine.Graphics2D.dll with its namespace unchanged.
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.Camera2D))]

// The legacy 2D model (the IRenderer API, textures, images, sprites, atlases, animation, particles, light, parallax)
// moved to MicroEngine.Graphics2D.dll with its namespace unchanged.
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.IRenderer))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.DrawOptions))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.BlendMode))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.TextureFilter))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.Texture))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.IGraphicsDevice))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.PixelImage))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.PngEncoder))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.Art))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.Sprite))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.SpriteAtlas))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.SpriteAtlasBuilder))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.AtlasOptions))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.AtlasData))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.AtlasRegion))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.AtlasAnimation))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.Animator))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.AnimationClip))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.AnimationDirection))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.ParticleSystem))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.ParticleSpec))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.LightMap))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.ParallaxLayer))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.RendererExtensions))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.PanelStyle))]
