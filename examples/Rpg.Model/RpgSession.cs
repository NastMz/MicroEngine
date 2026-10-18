using MicroEngine.Loop;
using MicroEngine.TwoD;
using RoomExplorer.Model;
namespace Rpg.Model;

public enum RpgMode { Exploration, Battle, Result }
public enum BattleAction { Attack, Guard, Retreat }
public enum BattleOutcome { None, Victory, Defeat, Retreated }
public readonly record struct BattleSnapshot(int PlayerHp,int OpponentHp,int Turn,BattleOutcome Outcome);

public sealed class RpgSession
{
    private readonly Explorer _world=new();
    public Room Room=>_world.Room;
    public float X=>_world.X;
    public float Y=>_world.Y;
    public Rect Body=>new(X,Y,24,32);
    public RpgMode Mode {get;private set;}
    public BattleAction SelectedAction {get;private set;}
    public BattleSnapshot Battle {get;private set;}=new(12,10,0,BattleOutcome.None);
    public bool EncounterCleared {get;private set;}
    public int EncounterNumber {get;private set;}
    public void Update(TimeSpan delta,InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(delta<=TimeSpan.Zero||delta>TimeSpan.FromMilliseconds(100))throw new ArgumentOutOfRangeException(nameof(delta));
        if(Mode==RpgMode.Result)
        {
            if(input.Pressed.Contains("jump"))Mode=RpgMode.Exploration;
            return;
        }
        if(Mode==RpgMode.Battle)
        {
            foreach(var edge in input.Pressed)
            {
                if(edge=="up")SelectedAction=(BattleAction)(((int)SelectedAction+2)%3);
                else if(edge=="down")SelectedAction=(BattleAction)(((int)SelectedAction+1)%3);
                else if(edge=="jump") { Resolve();return; }
            }
            return;
        }
        var room=Room;
        _world.Update(delta,new(input.Held,input.Pressed.Where(edge=>edge!="jump")));
        if(room!=Room)return;
        var marker=new Rect(Explorer.MarkerX,Explorer.MarkerY,Explorer.MarkerWidth,Explorer.MarkerHeight);
        if(!EncounterCleared&&Room==Explorer.MarkerRoom&&input.Pressed.Contains("jump")&&StaticWorld.Overlaps(Body,marker))
        {
            Battle=new(12,10,0,BattleOutcome.None);SelectedAction=BattleAction.Attack;
            EncounterNumber++;Mode=RpgMode.Battle;
        }
    }
    private void Resolve()
    {
        if(SelectedAction==BattleAction.Retreat)
        {
            Battle=Battle with { Outcome=BattleOutcome.Retreated };Mode=RpgMode.Result;return;
        }
        int opponent=SelectedAction==BattleAction.Attack?Math.Max(0,Battle.OpponentHp-4):Battle.OpponentHp;
        int player=opponent==0?Battle.PlayerHp:Math.Max(0,Battle.PlayerHp-(SelectedAction==BattleAction.Guard?1:5));
        var outcome=opponent==0?BattleOutcome.Victory:player==0?BattleOutcome.Defeat:BattleOutcome.None;
        Battle=new(player,opponent,Battle.Turn+1,outcome);
        if(outcome!=BattleOutcome.None)
        {
            Mode=RpgMode.Result;EncounterCleared=outcome==BattleOutcome.Victory;
        }
    }
}
