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
    public bool EncounterCompleted { get; private set; }
    public TurnSession() { }
    private TurnSession(Explorer exploration) => _exploration = exploration;
    public TurnCheckpoint Capture() => new(2, Room, X, Y, MarkerActivated, MenuOpen, SelectedOption, TurnPhase, EncounterCompleted);
    public static TurnSession Restore(TurnCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (checkpoint.Version is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(checkpoint.Version));
        if (!Enum.IsDefined(checkpoint.SelectedOption)) throw new ArgumentOutOfRangeException(nameof(checkpoint.SelectedOption));
        if (checkpoint.TurnPhase is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(checkpoint.TurnPhase));
        if (checkpoint.EncounterCompleted && (checkpoint.Version == 1 || checkpoint.MenuOpen || checkpoint.TurnPhase != 0))
            throw new ArgumentException("Completed encounters require version 2, phase zero and a closed menu.", nameof(checkpoint));
        var exploration = Explorer.Create(checkpoint.Room, checkpoint.X, checkpoint.Y, checkpoint.MarkerActivated);
        return new TurnSession(exploration)
        {
            MenuOpen = checkpoint.MenuOpen,
            SelectedOption = checkpoint.SelectedOption,
            TurnPhase = checkpoint.TurnPhase,
            EncounterCompleted = checkpoint.EncounterCompleted
        };
    }
    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        TurnResolved = false;
        if (!MenuOpen)
        {
            var explorationInput = EncounterCompleted && input.Pressed.Contains("jump")
                ? new InputSnapshot(input.Held, input.Pressed.Where(edge => edge != "jump")) : input;
            _exploration.Update(delta, explorationInput);
            if (!EncounterCompleted && _exploration.InteractionOccurred)
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
                    if (TurnPhase == 0) { EncounterCompleted = true; MenuOpen = false; }
                }
                else MenuOpen = false;
                return; // Confirm at most once; neither later edges nor movement leak through.
            }
        }
    }
}
