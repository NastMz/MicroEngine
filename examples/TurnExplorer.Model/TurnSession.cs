using MicroEngine.Loop;
using RoomExplorer.Model;

namespace TurnExplorer.Model;

public enum MenuOption { Advance, Return }

public sealed class TurnSession
{
    private readonly Explorer _exploration = new();
    public Room Room => _exploration.Room;
    public float X => _exploration.X;
    public float Y => _exploration.Y;
    public bool MarkerActivated => _exploration.MarkerActivated;
    public bool MenuOpen { get; private set; }
    public MenuOption SelectedOption { get; private set; }
    public int TurnPhase { get; private set; }
    public bool TurnResolved { get; private set; }
    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        TurnResolved = false;
        if (!MenuOpen)
        {
            _exploration.Update(delta, input);
            if (_exploration.InteractionOccurred)
            {
                MenuOpen = true;
                SelectedOption = MenuOption.Advance;
            }
            return; // The opening edge belongs to exploration, never to a turn.
        }

        foreach (var edge in input.Pressed)
        {
            if (edge is "up" or "down")
                SelectedOption = SelectedOption == MenuOption.Advance ? MenuOption.Return : MenuOption.Advance;
            else if (edge == "jump")
            {
                if (SelectedOption == MenuOption.Advance)
                {
                    TurnPhase = (TurnPhase + 1) % 4;
                    TurnResolved = true;
                }
                else MenuOpen = false;
                return; // Confirm at most once; neither later edges nor movement leak through.
            }
        }
    }
}
