using MicroEngine.Core;
using MicroEngine.Scenes;

namespace NeonRift.Scenes;

/// <summary>
/// Remembers whether a scene advanced in the engine's latest update. Between updates, moving things are drawn
/// <see cref="Game.FrameBlend"/> of the way from the previous update to the current one; a scene that did not advance
/// (under the pause overlay, during a transition) draws its current state, so nothing swings back and forth while frozen.
/// </summary>
internal struct DrawBlend
{
    private long _updatedAt;

    /// <summary>Call from the scene's <c>Update</c>: its state advanced in this update.</summary>
    public void Mark(Game game) => _updatedAt = game.UpdateCount;

    /// <summary>How far between the previous and the current update to draw: 1 (the current state) unless the scene advanced in the latest update.</summary>
    public readonly float Of(Game game) => _updatedAt == game.UpdateCount ? game.FrameBlend : 1;

    /// <summary>The scene's age at the drawn instant, for motion driven by time: exactly <see cref="Scene.Age"/> at blend 1.</summary>
    public readonly float Age(Scene scene) => scene.Age - (1 - Of(scene.Game)) / scene.Game.Settings.UpdateRate;
}
