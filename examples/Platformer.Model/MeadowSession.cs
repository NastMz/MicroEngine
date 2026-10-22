using MicroEngine.Loop;
namespace Platformer.Model;

public enum SessionPhase { Title, Running, Paused }
public sealed class MeadowSession
{
    public CampaignGame Campaign { get; private set; } = new();
    public SessionPhase Phase { get; private set; } = SessionPhase.Title;
    public int Selection { get; private set; }
    public bool AdvancedThisUpdate { get; private set; }
    public void ClearJumpBuffer() => Campaign.ClearJumpBuffer();
    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero || delta > TimeSpan.FromMilliseconds(100)) throw new ArgumentOutOfRangeException(nameof(delta));
        AdvancedThisUpdate = false;
        if (input.Pressed.Contains("restart")) { Restart(); return; }
        if (Phase == SessionPhase.Title)
        {
            if (input.Pressed.Contains("jump")) Phase = SessionPhase.Running;
            return;
        }
        if (input.Pressed.Contains("pause"))
        {
            ClearJumpBuffer(); Selection = 0;
            Phase = Phase == SessionPhase.Running ? SessionPhase.Paused : SessionPhase.Running;
            return;
        }
        if (Phase == SessionPhase.Paused)
        {
            bool toggled = false;
            foreach (string edge in input.Pressed)
            {
                if (edge == "jump")
                {
                    if (Selection == 1) Restart();
                    else { ClearJumpBuffer(); Selection = 0; Phase = SessionPhase.Running; }
                    return;
                }
                if (!toggled && edge is "up" or "down") { Selection = 1 - Selection; toggled = true; }
            }
            return;
        }
        Campaign.Update(delta, input);
        AdvancedThisUpdate = true;
    }
    private void Restart()
    {
        Campaign = new CampaignGame(); Phase = SessionPhase.Running; Selection = 0;
    }
}
