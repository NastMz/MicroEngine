using RoomExplorer.Model;

namespace TurnExplorer.Model;

public sealed record TurnCheckpoint(int Version, Room Room, float X, float Y,
    bool MarkerActivated, bool MenuOpen, MenuOption SelectedOption, int TurnPhase, bool EncounterCompleted = false);
