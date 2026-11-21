using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>What a post-processing effect does on a graphics device (<see cref="PostEffect.SupportOn"/>).</summary>
public enum PostEffectSupport
{
    /// <summary>The effect as its type describes it.</summary>
    Full,
    /// <summary>
    /// A simpler form the effect's type states, made of what every device has (render targets, the four blend states,
    /// linear sampling): what an effect with a shader draws where shaders do not run.
    /// </summary>
    Reduced,
    /// <summary>Nothing: the picture passes through the effect unchanged.</summary>
    None,
}

/// <summary>
/// One full-screen effect of a <see cref="PostProcess"/> chain: it receives the picture so far and leaves a new one.
/// The ready effects are <see cref="ColorGrade"/>, <see cref="Blur"/>, <see cref="Bloom"/>, <see cref="Vignette"/>,
/// <see cref="Scanlines"/>, <see cref="Crt"/> and <see cref="ShaderEffect"/> (a material of the game's own); a game
/// derives its own from this class. Pictures are premultiplied, like the canvas, and an effect keeps their alpha
/// unless changing it is its purpose.
/// </summary>
public abstract class PostEffect
{
    /// <summary>Whether the chain runs the effect. A disabled effect costs nothing.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// What the effect draws with the textures of <paramref name="graphics"/>, with its current values: an effect with
    /// a shader is <see cref="PostEffectSupport.Full"/> where shaders run (<see cref="IGraphicsDevice.SupportsShaders"/>)
    /// and, elsewhere, <see cref="PostEffectSupport.Reduced"/> or <see cref="PostEffectSupport.None"/> as its type states.
    /// </summary>
    public abstract PostEffectSupport SupportOn(IGraphicsDevice graphics);

    /// <summary>
    /// Runs the effect: reads <see cref="PostPass.Source"/> and leaves the new picture with <see cref="PostPass.Copy"/>,
    /// <see cref="PostPass.Replace"/> or by drawing over it (<see cref="PostPass.BeginOver"/>). An effect that does
    /// nothing leaves the picture as it is. Never called for an effect whose support is <see cref="PostEffectSupport.None"/>.
    /// </summary>
    protected internal abstract void Apply(PostPass pass);
}

/// <summary>
/// A chain of full-screen effects over a picture: what was drawn between <see cref="Begin"/> and <see cref="End"/>
/// (or a picture given to <see cref="Apply"/>) goes through every enabled effect in order, and the result is drawn
/// where the picture would have been. The chain owns the render targets, textures and built-in materials its effects
/// need, sized to the picture, and releases them with <see cref="Dispose"/>. One chain can run on pictures of
/// different sizes, in one frame too: it keeps what it made for each size, and releases what no run has used for
/// <see cref="KeptForRuns"/> runs (so a chain may run up to that many times in one frame). A scene has one for its own picture
/// (<c>Scene.PostProcess</c>), and a game has one for the canvas and one for the window (<c>Game.PostProcess</c>,
/// <c>Game.ScreenPostProcess</c>); the engine begins, ends and releases those.
/// </summary>
/// <remarks>
/// Effects are made of the drawing API (render targets, states, materials), so a chain runs on every platform. Where
/// the graphics device runs no shaders (SDL's default device, raylib, browser builds, the headless platform), each
/// effect draws what its <see cref="PostEffect.SupportOn"/> says: <see cref="Vignette"/> and <see cref="Scanlines"/> are the
/// same everywhere, the others have a stated simpler form or are left out. A chain with no enabled effect does
/// nothing and costs nothing: the picture is drawn straight to where it goes. Post-processing only draws; it is not
/// part of a game's simulation state.
/// </remarks>
/// <example>
/// <code>
/// // In a scene's Enter: the scene's picture glows and darkens towards the corners.
/// PostProcess.Add(new Bloom { Threshold = 0.6f, Strength = 1.2f });
/// PostProcess.Add(new Vignette { Strength = 0.6f });
/// </code>
/// </example>
public sealed class PostProcess : IDisposable
{
    private readonly List<PostEffect> _effects = [];
    /// <summary>How many runs of the chain a target or a texture is kept for after the last run that used it.</summary>
    public const int KeptForRuns = 8;

    private enum Kind : byte { Target, Image, Material }

    /// <summary>
    /// What was made for an effect under a name, and for which variant of it: a target's size, an image's version.
    /// Several variants live side by side, so a chain that runs on pictures of two sizes in a frame keeps both.
    /// </summary>
    private readonly record struct Key(PostEffect? Effect, string Name, long Variant);

    private sealed class Held(IDisposable resource, Kind kind)
    {
        public IDisposable Resource { get; } = resource;
        public Kind Kind { get; } = kind;
        public long UsedAt { get; set; }
    }

    private readonly Dictionary<Key, Held> _resources = [];
    private readonly Dictionary<(PostEffect? Effect, string Name), Kind> _kinds = [];
    private readonly List<Key> _stale = [];
    private RenderTarget2D? _picture;
    private long _run;
    private readonly PostPass _pass;
    private IGraphicsDevice? _graphics;
    private Camera2D? _whole;
    private bool _begun;

    /// <summary>Creates an empty chain.</summary>
    public PostProcess() => _pass = new PostPass(this);

    /// <summary>Whether the chain runs at all; false draws the picture as it is, whatever effects the chain holds.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The effects, in the order they run.</summary>
    public IReadOnlyList<PostEffect> Effects => _effects;

    /// <summary>Whether the chain would change a picture now: it is enabled and holds at least one enabled effect.</summary>
    public bool IsActive
    {
        get
        {
            if (!Enabled) return false;
            foreach (var effect in _effects)
                if (effect.Enabled) return true;
            return false;
        }
    }

    /// <summary>Adds <paramref name="effect"/> at the end of the chain and returns the chain.</summary>
    /// <exception cref="ArgumentException">The chain already holds this effect.</exception>
    public PostProcess Add(PostEffect effect) => Insert(_effects.Count, effect);

    /// <summary>Adds <paramref name="effect"/> at <paramref name="index"/> (0 runs first) and returns the chain.</summary>
    /// <exception cref="ArgumentException">The chain already holds this effect.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a position of the chain.</exception>
    public PostProcess Insert(int index, PostEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (_effects.Contains(effect)) throw new ArgumentException("The chain already holds this effect; an effect runs once per chain.", nameof(effect));
        _effects.Insert(index, effect);
        return this;
    }

    /// <summary>Removes <paramref name="effect"/> and releases what the chain made for it. Returns whether the chain held it.</summary>
    public bool Remove(PostEffect effect)
    {
        if (!_effects.Remove(effect)) return false;
        foreach (var key in _resources.Keys.Where(key => ReferenceEquals(key.Effect, effect)).ToArray())
        {
            _resources[key].Resource.Dispose();
            _resources.Remove(key);
        }
        foreach (var name in _kinds.Keys.Where(name => ReferenceEquals(name.Effect, effect)).ToArray()) _kinds.Remove(name);
        return true;
    }

    /// <summary>Removes every effect and releases what the chain made for them.</summary>
    public void Clear()
    {
        _effects.Clear();
        Dispose();
    }

    /// <summary>The first effect of type <typeparamref name="T"/>, or null: to change its values while the game runs.</summary>
    public T? Get<T>() where T : PostEffect
    {
        foreach (var effect in _effects)
            if (effect is T found) return found;
        return null;
    }

    /// <summary>
    /// What each effect of the chain does with the textures of <paramref name="graphics"/>, in the chain's order
    /// (<see cref="PostEffect.SupportOn"/>): for a settings screen, or to say what a device leaves out.
    /// </summary>
    public IReadOnlyList<(PostEffect Effect, PostEffectSupport Support)> SupportOn(IGraphicsDevice graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        return [.. _effects.Select(effect => (effect, effect.SupportOn(graphics)))];
    }

    /// <summary>
    /// Starts the chain's picture: draws made with <paramref name="renderer"/> until <see cref="End"/> go into a target
    /// of the renderer's size (its current target's), which starts as <paramref name="clear"/> (transparent when null).
    /// Like any pushed target, the picture starts with no camera, no clip and no state. An inactive chain
    /// (<see cref="IsActive"/> false) pushes nothing: the draws go where they would without it.
    /// </summary>
    /// <param name="renderer">The renderer the picture is drawn with, and the result drawn into.</param>
    /// <param name="graphics">The graphics device of the renderer (<c>Game.Graphics</c>).</param>
    /// <param name="clear">What the picture starts as; null is transparent, so the result is drawn over what is under it.</param>
    /// <returns>Whether a picture was started.</returns>
    /// <exception cref="InvalidOperationException">The chain's picture is already being drawn.</exception>
    public bool Begin(IRenderer renderer, IGraphicsDevice graphics, Color? clear = null)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(graphics);
        if (_begun) throw new InvalidOperationException("The chain's picture is already being drawn: call End before Begin.");
        if (!IsActive) return false;
        Use(graphics);
        var picture = _picture = Target(null, "picture", renderer.Width, renderer.Height, TextureFilter.Nearest);
        renderer.PushTarget(picture);
        renderer.Clear(clear ?? Color.Transparent);
        _begun = true;
        return true;
    }

    /// <summary>
    /// Ends the picture started by <see cref="Begin"/>, runs the effects on it and draws the result over the whole of
    /// what the renderer was drawing into, with normal transparency (or replacing what is there with
    /// <paramref name="opaque"/>). After a <see cref="Begin"/> that started nothing it does nothing.
    /// </summary>
    /// <exception cref="InvalidOperationException">A camera or a state pushed in the picture was not popped.</exception>
    public void End(IRenderer renderer, bool opaque = false)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        if (!_begun) return;
        _begun = false;
        renderer.PopTarget();
        var picture = _picture!;
        Present(renderer, Run(renderer, picture), new RectF(0, 0, renderer.Width, renderer.Height), opaque);
    }

    /// <summary>
    /// Runs the effects on an existing <paramref name="picture"/> (a render target's texture, or any texture) and draws
    /// the result into <paramref name="destination"/> of what the renderer is drawing into. An inactive chain draws
    /// the picture itself there.
    /// </summary>
    /// <param name="renderer">The renderer to draw with.</param>
    /// <param name="graphics">The graphics device of the renderer.</param>
    /// <param name="picture">The picture; it is not changed.</param>
    /// <param name="destination">Where the result lands, in the renderer's coordinates without a camera.</param>
    /// <param name="opaque">Whether the result replaces what is there (true) or is drawn over it with its transparency.</param>
    public void Apply(IRenderer renderer, IGraphicsDevice graphics, Texture picture, RectF destination, bool opaque = false)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(picture);
        if (_begun) throw new InvalidOperationException("The chain's picture is being drawn: call End first.");
        if (IsActive)
        {
            Use(graphics);
            picture = Run(renderer, picture);
        }
        Present(renderer, picture, destination, opaque);
    }

    /// <summary>
    /// Releases the render targets, textures and materials the chain made. The effects stay, and the chain makes what
    /// it needs again the next time it runs, so a released chain can be used again. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        foreach (var held in _resources.Values) held.Resource.Dispose();
        _resources.Clear();
        _kinds.Clear();
        _picture = null;
        _begun = false;
        _graphics = null;
    }

    /// <summary>What the chain holds for its effects now, in bytes of device memory (targets and textures).</summary>
    public long SizeInBytes
    {
        get
        {
            long bytes = 0;
            foreach (var held in _resources.Values)
                bytes += held.Resource switch { RenderTarget2D target => target.SizeInBytes, Texture texture => texture.SizeInBytes, _ => 0 };
            return bytes;
        }
    }

    private void Use(IGraphicsDevice graphics)
    {
        if (!ReferenceEquals(_graphics, graphics))
        {
            // Another device: what was made on the previous one cannot be drawn here.
            Dispose();
            _graphics = graphics;
        }
        // A new run. What no run used for a while is released now: a picture size or an image the chain stopped
        // needing. Never what an earlier run of this frame may still be drawn with (see KeptForRuns).
        _run++;
        foreach (var (key, held) in _resources)
            if (held.Kind != Kind.Material && _run - held.UsedAt > KeptForRuns) _stale.Add(key);
        foreach (var key in _stale)
        {
            _resources[key].Resource.Dispose();
            _resources.Remove(key);
        }
        _stale.Clear();
    }

    private Texture Run(IRenderer renderer, RenderTarget2D picture)
    {
        _pass.Start(renderer, _graphics!, picture.Texture, picture);
        return RunEffects();
    }

    private Texture Run(IRenderer renderer, Texture picture)
    {
        _pass.Start(renderer, _graphics!, picture, null);
        return RunEffects();
    }

    private Texture RunEffects()
    {
        foreach (var effect in _effects)
        {
            if (!effect.Enabled || effect.SupportOn(_graphics!) == PostEffectSupport.None) continue;
            _pass.Effect = effect;
            effect.Apply(_pass);
            _pass.Finish();
        }
        return _pass.Source;
    }

    /// <summary>The whole of <paramref name="picture"/> into <paramref name="destination"/>, in the target's own coordinates whatever camera is pushed.</summary>
    private void Present(IRenderer renderer, Texture picture, RectF destination, bool opaque)
    {
        if (_whole is null || _whole.ViewWidth != renderer.Width || _whole.ViewHeight != renderer.Height)
            _whole = new Camera2D(renderer.Width, renderer.Height) { PixelSnap = false };
        bool same = destination.Width == picture.Width && destination.Height == picture.Height;
        renderer.PushCamera(_whole);
        renderer.PushState((opaque ? DrawState.Opaque : DrawState.Alpha).Filtered(same ? TextureFilter.Nearest : TextureFilter.Linear));
        renderer.DrawStretched(picture, picture.Bounds, destination, Color.White);
        renderer.PopState();
        renderer.PopCamera();
    }

    /// <summary>The resource held under a key, marked as used by this run; refused when the name is another kind's.</summary>
    private T? Find<T>(in Key key, Kind kind) where T : class, IDisposable
    {
        var name = (key.Effect, key.Name);
        if (_kinds.TryGetValue(name, out var other) && other != kind)
            throw new InvalidOperationException($"{key.Effect?.GetType().Name ?? "The chain"} already uses the name '{key.Name}' for a {Word(other)}; " +
                $"give its {Word(kind)} another name (an effect's targets and textures are kept by name).");
        _kinds[name] = kind;
        if (!_resources.TryGetValue(key, out var held)) return null;
        held.UsedAt = _run;
        return (T)held.Resource;
    }

    private static string Word(Kind kind) => kind switch { Kind.Target => "render target", Kind.Image => "texture", _ => "material" };

    private T Keep<T>(in Key key, Kind kind, T resource) where T : class, IDisposable
    {
        _resources[key] = new Held(resource, kind) { UsedAt = _run };
        return resource;
    }

    internal RenderTarget2D Target(PostEffect? effect, string name, int width, int height, TextureFilter filter)
    {
        (width, height) = (Math.Max(1, width), Math.Max(1, height));
        var key = new Key(effect, name, ((long)width << 32) | (uint)height);
        if (Find<RenderTarget2D>(key, Kind.Target) is { IsDisposed: false } target)
        {
            target.Texture.Filter = filter;
            return target;
        }
        return Keep(key, Kind.Target, _graphics!.CreateRenderTarget(width, height, filter));
    }

    internal Texture Image<TState>(PostEffect effect, string name, long version, TState state, Func<TState, PixelImage> create, TextureFilter filter)
    {
        var key = new Key(effect, name, version);
        if (Find<Texture>(key, Kind.Image) is { IsDisposed: false } texture)
        {
            texture.Filter = filter;
            return texture;
        }
        return Keep(key, Kind.Image, _graphics!.CreateTexture(create(state), filter));
    }

    internal Material2D BuiltIn(string name)
    {
        var key = new Key(null, name, 0);
        if (Find<Material2D>(key, Kind.Material) is { IsDisposed: false } material) return material;
        return Keep(key, Kind.Material, _graphics!.CreateMaterial(PostShaders.Get(name)));
    }
}

/// <summary>
/// What an effect works with while it runs (<see cref="PostEffect.Apply"/>): the picture so far, the renderer, and the
/// chain's own targets and textures, which the chain keeps between frames and releases itself.
/// </summary>
public sealed class PostPass
{
    private readonly PostProcess _chain;
    private RenderTarget2D? _owned;      // the chain's target Source is the picture of, when it is one
    private bool _over;

    internal PostPass(PostProcess chain) => _chain = chain;

    internal PostEffect Effect { get; set; } = null!;

    /// <summary>The renderer to draw with. It is drawing into the chain's destination: push a target before drawing.</summary>
    public IRenderer Renderer { get; private set; } = null!;
    /// <summary>The graphics device of the renderer.</summary>
    public IGraphicsDevice Graphics { get; private set; } = null!;
    /// <summary>Whether materials run their shaders here (<see cref="IGraphicsDevice.SupportsShaders"/>).</summary>
    public bool ShadersRun => Graphics.SupportsShaders;
    /// <summary>The picture so far: what the effect reads. Premultiplied.</summary>
    public Texture Source { get; private set; } = null!;
    /// <summary>The picture's width in pixels.</summary>
    public int Width => Source.Width;
    /// <summary>The picture's height in pixels.</summary>
    public int Height => Source.Height;

    internal void Start(IRenderer renderer, IGraphicsDevice graphics, Texture source, RenderTarget2D? owned) =>
        (Renderer, Graphics, Source, _owned, _over) = (renderer, graphics, source, owned, false);

    internal void Finish()
    {
        if (_over) throw new InvalidOperationException($"{Effect.GetType().Name} called BeginOver without EndOver.");
    }

    /// <summary>
    /// Makes a new picture from the current one: the whole of <see cref="Source"/> copied, texel for texel, through
    /// <paramref name="material"/> (as it is when null) and multiplied by <paramref name="tint"/>. The copy becomes
    /// <see cref="Source"/>.
    /// </summary>
    public void Copy(Material2D? material = null, Color? tint = null) => Replace(Source, TextureFilter.Nearest, material, tint);

    /// <summary>
    /// Makes <paramref name="picture"/> the new picture: the whole of it stretched over a target of the picture's size,
    /// sampled with <paramref name="filter"/>, through <paramref name="material"/> and multiplied by
    /// <paramref name="tint"/>. An effect that built its result in a smaller target of its own ends with it.
    /// </summary>
    public void Replace(Texture picture, TextureFilter filter = TextureFilter.Linear, Material2D? material = null, Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(picture);
        // Two targets take turns, so a picture is never drawn into itself.
        var next = _chain.Target(null, ReferenceEquals(_owned, _chain.Target(null, "a", Width, Height, TextureFilter.Nearest)) ? "b" : "a", Width, Height, TextureFilter.Nearest);
        Draw(picture, next, DrawState.Opaque.Filtered(filter) with { Material = material }, tint);
        (Source, _owned) = (next.Texture, next);
    }

    /// <summary>
    /// Starts drawing on top of the current picture: draws made with the returned renderer until <see cref="EndOver"/>
    /// land on it, in its pixels. For effects that add to the picture or darken it (a glow, a vignette).
    /// </summary>
    /// <exception cref="InvalidOperationException">Already drawing over the picture.</exception>
    public IRenderer BeginOver()
    {
        if (_over) throw new InvalidOperationException("BeginOver called twice without EndOver.");
        // A picture the chain does not own (one given to Apply) is copied first, so it is never changed.
        if (_owned is null) Copy();
        Renderer.PushTarget(_owned!);
        _over = true;
        return Renderer;
    }

    /// <summary>Ends the drawing started by <see cref="BeginOver"/>.</summary>
    /// <exception cref="InvalidOperationException"><see cref="BeginOver"/> was not called.</exception>
    public void EndOver()
    {
        if (!_over) throw new InvalidOperationException("EndOver without BeginOver.");
        _over = false;
        Renderer.PopTarget();
    }

    /// <summary>
    /// A render target of the chain's for this effect, by <paramref name="name"/> and size: kept between frames,
    /// released with the chain or when no run has asked for that size for a while. It keeps its pixels until the
    /// effect draws into it. A name is a target's or a texture's (<c>Image</c>), not both.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A size that is not positive.</exception>
    /// <exception cref="InvalidOperationException">The effect already uses <paramref name="name"/> for a texture.</exception>
    public RenderTarget2D Scratch(string name, int width, int height, TextureFilter filter = TextureFilter.Linear)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "A scratch target needs a positive size.");
        return _chain.Target(Effect, name, width, height, filter);
    }

    /// <summary>
    /// A texture of the chain's for this effect, by <paramref name="name"/>, made from the image
    /// <paramref name="create"/> returns for <paramref name="state"/>: kept between frames, made again when
    /// <paramref name="version"/> differs from the one it was made with (the values that shape the image), released
    /// with the chain. Pass a static function and what it needs as <paramref name="state"/>, so nothing is allocated
    /// on the frames that reuse the texture.
    /// </summary>
    /// <exception cref="InvalidOperationException">The effect already uses <paramref name="name"/> for a render target.</exception>
    public Texture Image<TState>(string name, long version, TState state, Func<TState, PixelImage> create, TextureFilter filter = TextureFilter.Linear)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(create);
        return _chain.Image(Effect, name, version, state, create, filter);
    }

    /// <summary>
    /// Draws the whole of <paramref name="source"/> over the whole of <paramref name="target"/> with
    /// <paramref name="state"/> (its blend state, sampler and material), multiplied by <paramref name="tint"/>: one
    /// pass of an effect made of several.
    /// </summary>
    /// <exception cref="InvalidOperationException"><paramref name="source"/> is <paramref name="target"/>'s own picture.</exception>
    public void Draw(Texture source, RenderTarget2D target, in DrawState state, Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        Renderer.PushTarget(target);
        Renderer.PushState(state);
        Renderer.DrawStretched(source, source.Bounds, new RectF(0, 0, target.Width, target.Height), tint ?? Color.White);
        Renderer.PopState();
        Renderer.PopTarget();
    }

    /// <summary>One of the engine's own post-processing materials, made once per chain.</summary>
    internal Material2D BuiltIn(string name) => _chain.BuiltIn(name);
}

/// <summary>The fragment shaders of the ready effects, compiled ahead of time and carried by the engine.</summary>
internal static class PostShaders
{
    public const string Blur = "post-blur", Grade = "post-grade", Crt = "post-crt";
    private static readonly Dictionary<string, CompiledShader> Loaded = [];

    public static CompiledShader Get(string name)
    {
        lock (Loaded)
        {
            if (Loaded.TryGetValue(name, out var shader)) return shader;
            using var stream = typeof(PostShaders).Assembly.GetManifestResourceStream($"builtin/{name}.frag.shader")
                ?? throw new InvalidOperationException($"The built-in shader '{name}' is missing from MicroEngine.Graphics2D.");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return Loaded[name] = CompiledShader.Parse(memory.ToArray());
        }
    }
}
