using Adventure.Model;
using MicroEngine.Loop;
namespace Adventure.Checks;
public static class Course
{
    public static readonly TimeSpan Step=TimeSpan.FromSeconds(1d/60);
    public sealed class Driver(AdventureModel game, Action<TimeSpan,AdventureModel>? afterStep=null)
    {
        public int Steps { get; private set; }
        public void Tick(string[] held, string[] pressed)
        {
            if(++Steps>2500)throw new InvalidOperationException($"Adventure course bound exceeded at {game.Area}/{game.Body}.");
            game.Update(Step,new(held,pressed)); afterStep?.Invoke(Step,game);
        }
        public void MoveTo(float x,float y)
        {
            while(Math.Abs(game.Body.X-x)>1.3f || Math.Abs(game.Body.Y-y)>1.3f)
            {
                List<string> held=[];
                if(game.Body.X<x-1.3f)held.Add("right"); else if(game.Body.X>x+1.3f)held.Add("left");
                if(game.Body.Y<y-1.3f)held.Add("down"); else if(game.Body.Y>y+1.3f)held.Add("up");
                Tick(held.ToArray(),[]);
            }
        }
        public void Interact()=>Tick([],["jump"]);
        public void EnterShrine() { while(game.Area!=Area.Shrine)Tick(["right"],[]); }
        public void ReturnCourtyard() { while(game.Area!=Area.Courtyard)Tick(["left"],[]); }
        public void OpenAndEnter() { MoveTo(212,168); Interact(); EnterShrine(); }
        public void GetKey() { MoveTo(116,100); Interact(); }
        public void ReachAltar() { MoveTo(116,88); MoveTo(508,88); MoveTo(508,168); }
    }
    public static int Run(AdventureModel game,Action<TimeSpan,AdventureModel>? afterStep=null)
    {
        var driver=new Driver(game,afterStep); driver.OpenAndEnter(); driver.GetKey(); driver.ReachAltar(); driver.Interact();
        if(!game.Completed || !game.HasKey || !game.LeverOn)throw new InvalidOperationException("Adventure route did not complete.");
        return driver.Steps;
    }
}
