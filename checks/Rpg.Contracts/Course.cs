using MicroEngine.Loop;
using RoomExplorer.Model;
using Rpg.Model;
namespace Rpg.Checks;

public static class Course
{
    public static readonly TimeSpan Step=TimeSpan.FromSeconds(1d/60);
    public static void Tick(RpgSession game,string? held=null,string? edge=null,Action? after=null)
    { game.Update(Step,new(held is null?[]:[held],edge is null?[]:[edge]));after?.Invoke(); }
    public static void VisitMeadowAndReturn(RpgSession game,Action? after=null)
    {
        for(int i=0;game.Room==Room.Courtyard&&i<700;i++)Tick(game,"right",after:after);
        if(game.Room!=Room.Hall)throw new InvalidOperationException("Meadow route failed.");
        Tick(game,after:after);
        for(int i=0;game.Room==Room.Hall&&i<700;i++)Tick(game,"left",after:after);
        if(game.Room!=Room.Courtyard)throw new InvalidOperationException("Camp return failed.");
    }
    public static void Open(RpgSession game,Action? after=null)
    {
        for(int i=0;(game.X<300||game.X>310)&&i<700;i++)Tick(game,game.X<300?"right":"left",after:after);
        Tick(game,edge:"jump",after:after);
        if(game.Mode!=RpgMode.Battle)throw new InvalidOperationException("Trainer interaction failed.");
    }
    public static void Command(RpgSession game,BattleAction action,Action? after=null)
    {
        for(int i=0;game.SelectedAction!=action&&i<3;i++)Tick(game,edge:"down",after:after);
        Tick(game,edge:"jump",after:after);
    }
    public static void Finish(RpgSession game,BattleOutcome outcome,Action? after=null)
    {
        if(outcome==BattleOutcome.Victory)for(int i=0;i<3;i++)Command(game,BattleAction.Attack,after);
        else if(outcome==BattleOutcome.Defeat)
        { Command(game,BattleAction.Attack,after);Command(game,BattleAction.Attack,after);Command(game,BattleAction.Guard,after);Command(game,BattleAction.Guard,after); }
        else Command(game,BattleAction.Retreat,after);
        if(game.Mode!=RpgMode.Result||game.Battle.Outcome!=outcome)throw new InvalidOperationException("Encounter route failed.");
    }
}
