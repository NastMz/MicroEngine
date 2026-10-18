using MicroEngine.Loop;
using MicroEngine.TwoD;
using AreaId = Adventure.Model.Area;
namespace Adventure.Model;

public enum Area { Courtyard, Shrine }
public enum Interaction { None, Lever, Key, Altar }

/// <summary>Courtyard Key content and rules; shared engine utilities own geometry movement.</summary>
public sealed class AdventureModel
{
    public static Rect Gate => new(320,64,16,280);
    public static Rect Lever => new(208,156,32,48);
    public static Rect Key => new(112,96,32,32);
    public static Rect Altar => new(496,156,48,48);
    private static readonly IReadOnlyList<Rect> OpenCourtyard = Array.AsReadOnly<Rect>([
        new(0,0,640,64),new(0,344,640,16),new(0,64,16,280),new(624,64,16,80),new(624,216,16,128)]);
    private static readonly IReadOnlyList<Rect> ClosedCourtyard = Array.AsReadOnly(OpenCourtyard.Append(Gate).ToArray());
    private static readonly IReadOnlyList<Rect> ShrineSolids = Array.AsReadOnly<Rect>([
        new(0,0,640,64),new(0,344,640,16),new(624,64,16,280),new(0,64,16,80),new(0,216,16,128),new(288,128,32,144)]);
    private static readonly StaticWorld OpenWorld = new(OpenCourtyard);
    private static readonly StaticWorld ClosedWorld = new(ClosedCourtyard);
    private static readonly StaticWorld ShrineWorld = new(ShrineSolids);
    public Area Area { get; private set; } = AreaId.Courtyard;
    public Rect Body { get; private set; } = new(64,168,24,24);
    public bool LeverOn { get; private set; }
    public bool HasKey { get; private set; }
    public bool Completed { get; private set; }
    public bool Moving { get; private set; }
    public int RunNumber { get; private set; }
    public Interaction LastInteraction { get; private set; }
    public IReadOnlyList<Rect> ActiveSolids => Area==AreaId.Shrine ? ShrineSolids : LeverOn ? OpenCourtyard : ClosedCourtyard;
    private StaticWorld World => Area==AreaId.Shrine ? ShrineWorld : LeverOn ? OpenWorld : ClosedWorld;

    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(delta<=TimeSpan.Zero || delta>TimeSpan.FromMilliseconds(100))throw new ArgumentOutOfRangeException(nameof(delta));
        LastInteraction=Interaction.None;
        bool interact=input.Pressed.Contains("jump");
        if(Completed)
        {
            if(interact)Reset();
            return;
        }
        float x=(input.IsHeld("right")?1:0)-(input.IsHeld("left")?1:0);
        float y=(input.IsHeld("down")?1:0)-(input.IsHeld("up")?1:0);
        float magnitude=MathF.Sqrt(x*x+y*y);
        if(magnitude>0) { x/=magnitude; y/=magnitude; }
        float distance=150*(float)delta.TotalSeconds;
        float dx=Math.Clamp(Body.X+x*distance,16,600)-Body.X;
        float dy=Math.Clamp(Body.Y+y*distance,64,320)-Body.Y;
        var next=World.Move(Body,dx,dy).Bounds;
        Moving=next!=Body; Body=next;
        bool inOpening=Body.Y>=144 && Body.Y+Body.Height<=216;
        if(Area==AreaId.Courtyard && x>0 && Body.X>=600 && inOpening)
        {
            Area=AreaId.Shrine; Body=new(24,Body.Y,24,24); Moving=false; return;
        }
        if(Area==AreaId.Shrine && x<0 && Body.X<=16 && inOpening)
        {
            Area=AreaId.Courtyard; Body=new(588,Body.Y,24,24); Moving=false; return;
        }
        if(!interact)return;
        if(Area==AreaId.Courtyard && StaticWorld.Overlaps(Body,Lever))
        {
            LeverOn=!LeverOn; LastInteraction=Interaction.Lever;
        }
        else if(Area==AreaId.Shrine && !HasKey && StaticWorld.Overlaps(Body,Key))
        {
            HasKey=true; LastInteraction=Interaction.Key;
        }
        else if(Area==AreaId.Shrine && HasKey && StaticWorld.Overlaps(Body,Altar))
        {
            Completed=true; Moving=false; LastInteraction=Interaction.Altar;
        }
    }
    private void Reset()
    {
        Area=AreaId.Courtyard; Body=new(64,168,24,24); LeverOn=false; HasKey=false; Completed=false; Moving=false;
        LastInteraction=Interaction.None; RunNumber++;
    }
}
