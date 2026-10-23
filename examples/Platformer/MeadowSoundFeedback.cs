using System.Collections.ObjectModel;
using MicroEngine.Audio;
using Platformer.Model;
namespace Platformer;

/// <summary>Game-owned event/cue mapping; publish once after each successful session update.</summary>
public sealed class MeadowSoundFeedback
{
    public static IReadOnlyDictionary<string,string> CueFiles { get; } = new ReadOnlyDictionary<string,string>(new Dictionary<string,string>
    {
        ["jump"]="jump_1.wav",["coin"]="button.wav",["defeat"]="playerhurt.wav",["level-complete"]="spring.wav"
    });
    private CampaignGame? _campaign;
    private LevelGame? _level;
    private SessionPhase _phase;
    private int _run;
    public void Publish(MeadowSession session,IOneShotAudio? audio)
    {
        ArgumentNullException.ThrowIfNull(session);
        var current=session.Campaign.Current;
        bool first=_campaign is null;
        bool replaced=!ReferenceEquals(_campaign,session.Campaign)||!ReferenceEquals(_level,current)||_run!=current.RunNumber;
        bool stop=!first&&(replaced||(_phase!=session.Phase&&session.Phase!=SessionPhase.Running));
        (_campaign,_level,_phase,_run)=(session.Campaign,current,session.Phase,current.RunNumber);
        if(first)return;
        if(stop){audio?.StopAll();return;}
        if(!session.AdvancedThisUpdate||session.Phase!=SessionPhase.Running)return;
        foreach(var item in current.EventsThisUpdate)
            audio?.Play(item switch
            {
                LevelEvent.Jumped=>"jump",LevelEvent.CoinCollected=>"coin",LevelEvent.Defeated=>"defeat",LevelEvent.LevelCompleted=>"level-complete",
                _=>throw new InvalidOperationException("Unmapped game event.")
            });
    }
}
