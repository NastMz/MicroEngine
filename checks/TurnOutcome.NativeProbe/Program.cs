using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;
using RoomExplorer.Model;
using TurnExplorer;
using TurnExplorer.Model;
using TurnExplorer.Storage;

try
{
    if (args.Length != 1 || !Directory.Exists(args[0])) throw new ArgumentException("Owned fixture directory required.");
    TurnCheckpoint completed;
    using (var window = new DesktopPlatform(new(640, 360, "MicroEngine turn outcome verification")))
    {
        var game = TurnSession.Restore(new(2, Room.Courtyard, 310, 164, false, false, MenuOption.Advance, 0));
        void Draw()
        {
            for (int frame = 0; frame < 2; frame++) { window.Begin(); window.Canvas.Clear(new(0, 0, 0)); MenuRenderer.Draw(window.Canvas, game); window.End(); }
        }
        game.Update(TimeSpan.FromTicks(1), new([], ["jump"]));
        if (!game.MenuOpen || game.TurnResolved) throw new InvalidOperationException("Opening consumed a turn.");
        Draw();
        for (int turn = 1; turn <= 4; turn++)
        {
            game.Update(TimeSpan.FromTicks(1), new([], ["jump", "jump"]));
            if (game.TurnPhase != turn % 4 || !game.TurnResolved || game.EncounterCompleted != (turn == 4) || game.MenuOpen != (turn < 4))
                throw new InvalidOperationException("Turn outcome sequence mismatch.");
            Draw();
        }
        var image = Raylib.LoadImageFromScreen();
        try
        {
            for (int i = 0; i < 4; i++)
            {
                var color = Raylib.GetImageColor(image, 25 + 20 * i, 25);
                if (color.R != 80 || color.G != 200 || color.B != 120) throw new InvalidOperationException("Completed pip mismatch.");
            }
            var overlay = Raylib.GetImageColor(image, 430, 50);
            if (overlay.R != 0 || overlay.G != 0 || overlay.B != 0) throw new InvalidOperationException("Completed menu remained visible.");
            int glyphs = 0;
            for (int y = 42; y < 65; y++) for (int x = 20; x < 170; x++)
            {
                var pixel = Raylib.GetImageColor(image, x, y); if (pixel.G > 0) glyphs++;
            }
            if (glyphs <= 10) throw new InvalidOperationException("Completed label absent.");
        }
        finally { Raylib.UnloadImage(image); }
        bool marker = game.MarkerActivated;
        game.Update(TimeSpan.FromTicks(1), new(["jump"], ["jump", "jump"]));
        if (game.MenuOpen || game.TurnResolved || game.MarkerActivated != marker) throw new InvalidOperationException("Completed encounter reopened.");
        completed = game.Capture(); Console.WriteLine("turn-outcome:native:verified");
    }
    var path = Path.Combine(args[0], "completed.bin");
    CheckpointFile.CreateNew(path, completed);
    var restored = TurnSession.Restore(CheckpointFile.Read(path));
    if (!restored.EncounterCompleted || restored.MenuOpen || restored.TurnResolved || restored.TurnPhase != 0 || restored.Capture() != completed
        || completed.Version != 2 || new FileInfo(path).Length != 24) throw new InvalidOperationException("Completed checkpoint restart mismatch.");
    Console.WriteLine("turn-outcome:durable:verified"); Console.WriteLine("turn-outcome:closed"); return 0;
}
catch (Exception error) { Console.Error.WriteLine($"turn-outcome:failed:{error.GetType().Name}:{error.Message}"); return 1; }
