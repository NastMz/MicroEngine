using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>
/// The scene system behind <see cref="Scene.Effects"/>: it steps the scene's one-shot sprite effects on the scene's
/// time before <see cref="Scene.Update"/>, adds them to the scene's state hash and drops them when the scene leaves.
/// The scene draws them where they belong among its own drawing (<c>Effects.Draw(renderer)</c>).
/// </summary>
public sealed class SceneEffects : SceneSystem
{
    /// <summary>The effects this system steps.</summary>
    public SpriteEffects Effects { get; } = new();

    /// <inheritdoc/>
    protected override void Update(float dt) => Effects.Update(dt);

    /// <inheritdoc/>
    public override void HashState(StateHasher hash) => Effects.HashState(hash);

    /// <inheritdoc/>
    protected override void Stop() => Effects.Clear();
}
