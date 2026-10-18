using MapExplorer;
using MapExplorer.Model;
using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;

try
{
    using (var window = new DesktopPlatform(new(640, 360, "MicroEngine map viewport verification")))
    {
        void Draw(MapWalker game, params (int X, int Y, Rgba Expected)[] samples)
        {
            for (int frame = 0; frame < 2; frame++) { window.Begin(); MapRenderer.Draw(window.Canvas, game); window.End(); }
            var image = Raylib.LoadImageFromScreen();
            try
            {
                foreach (var (x, y, expected) in samples)
                {
                    var actual = Raylib.GetImageColor(image, x, y);
                    if (actual.R != expected.R || actual.G != expected.G || actual.B != expected.B)
                        throw new InvalidOperationException($"Map pixel {x},{y}: {actual.R},{actual.G},{actual.B}.");
                }
            }
            finally { Raylib.UnloadImage(image); }
        }
        var initial = new MapWalker(); var scrolled = MapWalker.Create(Zone.Meadow, 508, 304);
        if (initial.CameraX != 0 || scrolled.CameraX != 200 || initial.CameraY != 140 || scrolled.CameraY != 140)
            throw new InvalidOperationException("Probe camera fixture mismatch.");
        Draw(initial, (410, 110, new(180, 100, 70)), (210, 110, new(24, 40, 32)), (45, 170, new(240, 180, 65)));
        Draw(scrolled, (210, 110, new(180, 100, 70)), (410, 110, new(24, 40, 32)), (310, 170, new(240, 180, 65)));
        var clipped = MapWalker.Create(Zone.Meadow, 558, 40);
        if (clipped.CameraX != 250 || clipped.CameraY != 0) throw new InvalidOperationException("Clipping fixture mismatch.");
        Draw(clipped, (0, 90, new(95, 130, 165)), (49, 90, new(95, 130, 165)), (50, 90, new(24, 40, 32)));
        Draw(MapWalker.Create(Zone.Ruins, 8, 304), (2, 110, new(70, 200, 210)), (170, 10, new(95, 130, 165)), (500, 300, new(32, 24, 48)));
        Console.WriteLine("map:viewport:verified");
        var journey = new MapWalker();
        void Move(int seconds, string direction, Zone zone, float x, float y, float cameraX, float cameraY)
        {
            journey.Update(TimeSpan.FromSeconds(seconds), new([direction], []));
            if ((journey.Zone, journey.X, journey.Y, journey.CameraX, journey.CameraY) != (zone, x, y, cameraX, cameraY))
                throw new InvalidOperationException($"Navigation state mismatch after {direction}: {journey.Zone},{journey.X},{journey.Y}.");
            foreach (var wall in MapLayout.For(zone).Solids)
                if (x < wall.X + wall.Width && x + 24 > wall.X && y < wall.Y + wall.Height && y + 32 > wall.Y)
                    throw new InvalidOperationException("Navigation penetrated a wall.");
        }
        Move(10, "right", Zone.Meadow, 696, 304, 388, 140);
        Draw(journey, (310, 170, new(240, 180, 65)), (334, 170, new(110, 120, 140)), (334, 40, new(24, 40, 32)));
        Move(1, "up", Zone.Meadow, 696, 124, 388, 0);
        Draw(journey, (334, 170, new(24, 40, 32)), (310, 130, new(240, 180, 65)));
        Move(1, "right", Zone.Meadow, 876, 124, 568, 0);
        Move(1, "down", Zone.Meadow, 876, 304, 568, 140);
        Move(10, "right", Zone.Ruins, 8, 304, 0, 140);
        Draw(journey, (402, 110, new(110, 120, 140)), (2, 110, new(70, 200, 210)), (10, 170, new(240, 180, 65)));
        Move(1, "left", Zone.Meadow, 1248, 304, 640, 140);
        Draw(journey, (82, 100, new(110, 120, 140)), (610, 170, new(240, 180, 65)));
        Console.WriteLine("map:navigation:verified");
        var gate = MapWalker.Create(Zone.Meadow, 650, 304);
        if (gate.GateOpen || gate.CameraX != 342 || gate.CameraY != 140) throw new InvalidOperationException("Switch fixture mismatch.");
        Draw(gate, (280, 150, new(90, 140, 220)), (380, 170, new(110, 120, 140)), (310, 170, new(240, 180, 65)));
        var closedView = gate.BlockingSolids;
        gate.Update(TimeSpan.FromTicks(1), new(["jump"], []));
        if (gate.GateOpen) throw new InvalidOperationException("Held input opened gate.");
        gate.Update(TimeSpan.FromTicks(1), new([], ["jump", "jump"]));
        if (!gate.GateOpen || gate.BlockingSolids.Count != 0 || closedView.Count != 1 || gate.X != 650 || gate.Y != 304)
            throw new InvalidOperationException("Switch did not change only future topology.");
        Draw(gate, (280, 150, new(80, 200, 120)), (380, 170, new(24, 40, 32)), (310, 170, new(240, 180, 65)));
        gate.Update(TimeSpan.FromSeconds(1), new(["right"], []));
        if (gate.X != 830 || gate.Y != 304 || gate.CameraX != 522 || gate.CameraY != 140) throw new InvalidOperationException("Open shortcut remained blocked.");
        gate.Update(TimeSpan.FromSeconds(10), new(["right"], []));
        if (gate.Zone != Zone.Ruins || gate.X != 8 || gate.Y != 304 || gate.CameraX != 0 || gate.CameraY != 140 || !gate.GateOpen || gate.BlockingSolids.Count != 1)
            throw new InvalidOperationException("Gate state or Ruins wall was lost.");
        gate.Update(TimeSpan.FromSeconds(1), new(["left"], []));
        if (gate.Zone != Zone.Meadow || gate.X != 1248 || gate.Y != 304 || gate.CameraX != 640 || gate.CameraY != 140
            || !gate.GateOpen || gate.BlockingSolids.Count != 0 || closedView.Count != 1 || new MapWalker().GateOpen)
            throw new InvalidOperationException("Gate state failed return or new-session isolation.");
        Console.WriteLine("map:switch-gate:verified");
    }
    Console.WriteLine("map:closed"); return 0;
}
catch (Exception error) { Console.Error.WriteLine($"map:failed:{error.GetType().Name}:{error.Message}"); return 1; }
