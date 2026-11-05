using MicroEngine.Scenes;

namespace MeadowRun.Scenes;

/// <summary>The clock frames animate with.</summary>
internal static class DrawTime
{
    /// <summary>
    /// The scene's age as drawn this frame: one step back at its world's blend 0, exactly its age at blend 1. The blend
    /// is 1 whenever the scene did not step in the latest update (paused, frozen under an overlay, leaving), so a frozen
    /// scene animates nothing.
    /// </summary>
    public static float Of(Scene scene) => scene.Age - (1 - scene.Objects.Blend) * scene.EngineStep;
}
