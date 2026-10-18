using MapExplorer.Model;
using MicroEngine.Loop;

namespace MapExplorer;

internal static class MapRenderer
{
    internal static void Draw(ICanvas canvas, MapWalker game)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(game);
        var map = MapLayout.For(game.Zone);
        float cameraX = game.CameraX, cameraY = game.CameraY;
        canvas.Clear(map.Background);
        void FillWorld(Rect world, Rgba color)
        {
            float x = world.X - cameraX, y = world.Y - cameraY;
            float left = MathF.Max(0, x), top = MathF.Max(0, y);
            float right = MathF.Min(640, x + world.Width), bottom = MathF.Min(360, y + world.Height);
            if (right > left && bottom > top) canvas.Fill(new(left, top, right - left, bottom - top), color);
        }
        foreach (var landmark in map.Landmarks) FillWorld(landmark.Bounds, landmark.Color);
        foreach (var solid in game.BlockingSolids) FillWorld(solid, new(110, 120, 140));
        if (map.Switch is Rect trigger) FillWorld(trigger, game.GateOpen ? new(80, 200, 120) : new(90, 140, 220));
        FillWorld(new(game.Zone == Zone.Meadow ? map.Width - 8 : 0, 240, 8, 160), new(70, 200, 210));
        FillWorld(new(game.X, game.Y, 24, 32), new(240, 180, 65));
        if (game.Zone == Zone.Meadow && canvas is ILabelCanvas labels)
            labels.Label(game.GateOpen ? "Gate open" : "Gate closed - Space at blue switch", 20, 20, 20, new(240, 240, 245));
    }
}
