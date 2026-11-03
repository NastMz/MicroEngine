using System.Collections;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.World;

namespace TrailDuel.Scenes;

/// <summary>Story cutscenes, dialogue and battle hand-offs of the overworld.</summary>
internal sealed partial class OverworldScene
{
    private const string Mentor = Maps.MentorName;
    private const string Rival = GameState.RivalName;

    private IEnumerator IntroScript()
    {
        yield return Wait.Seconds(0.6f);
        var mom = _map.Npcs.First(n => n.Def?.Id == "mom");
        Face(mom, _player);
        yield return Say("Tavi! You're finally up!", "Mom");
        yield return Read(mom.Def!.Lines.Skip(1).ToArray(), "Mom");
        State.Set("intro");
    }

    private IEnumerator Read(string[] pages, string? speaker)
    {
        foreach (var p in pages) yield return Say(p, speaker);
    }

    private IEnumerator? TriggerScript(string id) => id switch
    {
        "need-starter" when !State.Has("starter") => NeedStarter(),
        "rival2" when State.Has("rival1") && !State.Has("rival2") => RivalTwo(),
        _ => null,
    };

    private IEnumerator NeedStarter()
    {
        yield return Say("Wait, Tavi! Don't wander into the tall grass without a partner!", Mentor);
        yield return Say("Come see me at my lab first. It's the teal roof just east of here.", Mentor);
        yield return WalkActor(_player, Direction.Down, 1);
    }

    private IEnumerator WarpTo(WarpDef warp)
    {
        var target = Maps.Get(warp.To);
        bool door = target.Indoor || _map.Def.Indoor;
        if (door) Audio.Play("door", 0.8f);
        yield return FadeOut(0.22f);
        LoadMap(warp.To, warp.ToX, warp.ToY, warp.Facing);
        yield return Wait.Seconds(0.08f);
        yield return FadeIn(0.25f);
    }

    private IEnumerator PickUp(ItemBallDef item)
    {
        var data = ItemDex.Get(item.ItemId);
        State.Set(item.Flag);
        State.Bag.Add(item.ItemId, item.Count);
        Audio.Play("jingle:item", 0.8f);
        yield return Say(item.Count > 1 ? $"Tavi found {item.Count} {data.Name}s!" : $"Tavi found a {data.Name}!");
        yield return Say($"Tavi put it in the Bag.");
    }

    private IEnumerator Talk(Actor npc)
    {
        var def = npc.Def!;
        Face(npc, _player);
        if (def.Trainer is { } trainer)
        {
            if (!State.Has(def.DefeatFlag)) { yield return TrainerEncounter(npc); yield break; }
            yield return Read(trainer.After, def.Speaker);
            if (trainer.IsMaster && State.Has("master")) yield return Say("Come back any time. My door is always open to the Champion.", def.Speaker);
            yield break;
        }
        switch (def.Role)
        {
            case NpcRole.Mom: yield return MomTalk(def); break;
            case NpcRole.Mentor: yield return MentorTalk(); break;
            case NpcRole.Rival: yield return Say("Go on, pick first. I already know which one I want!", Rival); break;
            case NpcRole.Keeper: yield return KeeperTalk(def); break;
            case NpcRole.Clerk: yield return ClerkTalk(def); break;
            case NpcRole.Gift: yield return GiftTalk(def); break;
            default: yield return Read(def.Lines, def.Speaker); break;
        }
        npc.Facing = def.Facing;
    }

    private IEnumerator MomTalk(NpcDef def)
    {
        if (!State.Has("starter"))
        {
            yield return Read(def.Lines.Skip(1).ToArray(), "Mom");
            yield break;
        }
        yield return Say(State.Has("master") ? "My child, the Trail Champion! Let me fix you something warm." : "You look worn out, sweetie. Have a little rest.", "Mom");
        yield return HealSequence();
        State.RespawnMap = "home";
        (State.RespawnX, State.RespawnY) = (4, 6);
        yield return Say("There! Your creatures are bursting with energy again.", "Mom");
    }

    private IEnumerator HealSequence()
    {
        yield return FadeOut(0.3f);
        State.HealParty();
        Audio.Play("jingle:heal");
        yield return Wait.Seconds(1.6f);
        yield return FadeIn(0.3f);
    }

    private IEnumerator MentorTalk()
    {
        if (!State.Has("met-mentor"))
        {
            yield return Say("Ah, Tavi! Right on time.", Mentor);
            yield return Say("Creatures live all along the trail, from Sprout Town to Brookhaven.", Mentor);
            yield return Say("Every trainer begins with a partner. On this table rest three orbs...", Mentor);
            yield return Say("...each holding a young creature. Go on, pick the one that speaks to you!", Mentor);
            State.Set("met-mentor");
            yield break;
        }
        if (!State.Has("starter")) { yield return Say("Take your time. Flame, Tide or Leaf. Each is wonderful in its own way.", Mentor); yield break; }
        if (!State.Has("gift:orbs")) { yield return MentorGift(); yield break; }
        if (State.Has("master")) { yield return Say("Champion! I always knew you two would make it.", Mentor); yield break; }
        string partner = State.Party.Count > 0 ? State.Party[0].Name : "your partner";
        yield return Say($"How is {partner} doing? You two look inseparable!", Mentor);
        yield return Say("Brookhaven is north, past Route 1 and Hollow Wood.", Mentor);
    }

    private IEnumerator MentorGift()
    {
        yield return Say("What a splendid first duel! Here, let me tend to your partner.", Mentor);
        State.HealParty();
        Audio.Play("jingle:heal");
        yield return Wait.Seconds(1.2f);
        yield return Say("Take these Capture Orbs. Weaken a wild creature, then throw one to catch it.", Mentor);
        State.Bag.Add("orb", 5);
        State.Set("gift:orbs");
        Audio.Play("jingle:item", 0.8f);
        yield return Say("Tavi received 5 Capture Orbs!");
        yield return Say("Head north along Route 1 and through Hollow Wood to reach Brookhaven.", Mentor);
        yield return Say("Trail Master Odessa waits there. Beat her, and you'll be a true Trail Champion!", Mentor);
    }

    private IEnumerator Pedestal(int index)
    {
        if (index < 0 || index >= SpeciesDex.Starters.Length) yield break;
        string species = SpeciesDex.Starters[index];
        if (!State.Has("met-mentor")) { yield return Say("Three orbs rest on the table. Better talk to Mentor Ilse first."); yield break; }
        if (State.Has("starter"))
        {
            yield return Say(species == State.StarterId || species == GameState.RivalStarterFor(State.StarterId)
                ? "The orb is empty now."
                : "The last orb. Mentor Ilse will raise this one herself.");
            yield break;
        }
        var data = SpeciesDex.Get(species);
        _previewSpecies = species;
        bool yes = false;
        yield return Ask($"Choose {data.Name}, the {data.Type} creature?", null, r => yes = r);
        _previewSpecies = null;
        if (!yes) yield break;

        var starter = Creature.Create(species, 5);
        State.StarterId = species;
        State.AddCreature(starter);
        State.Set("starter");
        Audio.Play("jingle:catch");
        yield return Say($"Tavi received {data.Name}!");
        yield return RivalOne();
    }

    private IEnumerator RivalOne()
    {
        var rival = _map.Npcs.FirstOrDefault(n => n.Def?.Role == NpcRole.Rival);
        string rivalSpecies = SpeciesDex.Get(GameState.RivalStarterFor(State.StarterId)).Name;
        if (rival is not null) Face(rival, _player);
        yield return Say($"Then I'll take {rivalSpecies}! It's strong against yours, you know.", Rival);
        yield return Say($"{Rival} received {rivalSpecies}!");
        yield return Say("Come on, Tavi. Let's see what our partners can do!", Rival);

        BattleOutcome outcome = BattleOutcome.None;
        yield return RunTrainerBattle(Maps.RivalBattle(State, 1), Rival, o => outcome = o, allowWhiteout: false);
        if (outcome == BattleOutcome.Lost)
        {
            State.HealParty();
            yield return Say("Ha! Told you mine was the strong one!", Rival);
        }
        else yield return Say("...Fine. You got lucky. I'll train harder than anyone!", Rival);
        yield return Say("See you on the trail, Tavi. Smell ya later!", Rival);
        if (rival is not null)
        {
            yield return WalkActor(rival, Direction.Down, 2, 0.16f);
            yield return WalkActor(rival, Direction.Right, 3, 0.16f);
            yield return WalkActor(rival, Direction.Down, 2, 0.16f);
            Audio.Play("door", 0.7f);
        }
        State.Set("rival1");
        _map.RefreshNpcs();
        yield return Wait.Seconds(0.3f);
        var mentor = _map.Npcs.First(n => n.Def?.Role == NpcRole.Mentor);
        Face(mentor, _player);
        yield return MentorGift();
        mentor.Facing = Direction.Down;
    }

    private IEnumerator RivalTwo()
    {
        State.Set("rival2-active");
        _map.RefreshNpcs();
        var rival = _map.Npcs.First(n => n.Def?.Id == "rival2");
        rival.Teleport(_player.X, 0);
        rival.Facing = Direction.Down;
        yield return ShowBubble(rival);
        yield return WalkActor(rival, Direction.Down, Math.Max(0, _player.Y - 2), 0.2f);
        _player.Facing = Direction.Up;
        yield return Say("Tavi! There you are. I've been training nonstop in these woods.", Rival);
        yield return Say("Odessa's hall is just past here. But first... a rematch!", Rival);
        BattleOutcome outcome = BattleOutcome.None;
        yield return RunTrainerBattle(Maps.RivalBattle(State, 2), Rival, o => outcome = o, allowWhiteout: true);
        if (outcome != BattleOutcome.Won)
        {
            State.Flags.Remove("rival2-active");
            yield break;
        }
        yield return Say("Hmph. You've gotten really strong, Tavi.", Rival);
        yield return Say("Go beat Odessa. Then I'll beat you. That's the plan!", Rival);
        yield return WalkActor(rival, Direction.Up, rival.Y, 0.16f);
        State.Set("rival2");
        _map.RefreshNpcs();
    }

    private IEnumerator KeeperTalk(NpcDef def)
    {
        yield return Say("Welcome to the Healing Shrine.", def.Speaker);
        bool yes = false;
        yield return Ask("Shall I restore your creatures to full health?", def.Speaker, r => yes = r);
        if (!yes) { yield return Say("Then may the trail be kind to you.", def.Speaker); yield break; }
        yield return Say("Please rest your orbs on the altar for a moment...", def.Speaker);
        _healGlow = 0;
        Audio.Play("jingle:heal");
        for (float t = 0; t < 1.8f; t += 1f / 60f) { _healGlow = t; yield return null; }
        _healGlow = -1;
        State.HealParty();
        State.RespawnMap = "shrine";
        (State.RespawnX, State.RespawnY) = (5, 5);
        yield return Say("Your creatures are fully restored!", def.Speaker);
        if (State.Box.Count > 0) yield return Say($"I'm also caring for {State.Box.Count} of your creatures in the Shrine Box.", def.Speaker);
        yield return Say("May the trail be kind to you.", def.Speaker);
    }

    private float _healGlow = -1;

    private IEnumerator ClerkTalk(NpcDef def)
    {
        yield return Say("Welcome to the Trail Shop! Take a look.", def.Speaker);
        bool done = false;
        Game.Scenes.Push(new ShopScene(_game, () => done = true));
        yield return Wait.Until(() => done);
        yield return Say("Thanks! Come again!", def.Speaker);
    }

    private IEnumerator GiftTalk(NpcDef def)
    {
        string flag = "gift:" + def.Id;
        if (State.Has(flag)) { yield return Say("Brookhaven's Trail Master is fearsome. Mind your type matchups!", def.Speaker); yield break; }
        yield return Read(def.Lines, def.Speaker);
        State.Bag.Add("superpotion", 1);
        State.Set(flag);
        Audio.Play("jingle:item", 0.8f);
        yield return Say("Tavi received a Super Potion!");
    }

    // ------------------------------------------------------------------ battles

    private IEnumerator TrainerEncounter(Actor npc)
    {
        var def = npc.Def!;
        var trainer = def.Trainer!;
        npc.Facing = def.Facing;
        yield return ShowBubble(npc);
        // Walk up until adjacent to the player.
        while (Math.Abs(npc.X - _player.X) + Math.Abs(npc.Y - _player.Y) > 1)
        {
            npc.StartStep(npc.Facing, Actor.StepTime);
            while (npc.Moving) yield return null;
        }
        Face(_player, npc);
        foreach (var line in trainer.Intro) yield return Say(line, def.Speaker);
        BattleOutcome outcome = BattleOutcome.None;
        yield return RunTrainerBattle(trainer, def.Speaker ?? trainer.Name, o => outcome = o, allowWhiteout: true);
        if (outcome != BattleOutcome.Won) yield break;
        State.Set(def.DefeatFlag);
        if (trainer.IsMaster) { yield return ChampionScript(def); yield break; }
        foreach (var line in trainer.After) yield return Say(line, def.Speaker);
    }

    private IEnumerator ChampionScript(NpcDef def)
    {
        yield return Say("Magnificent. You and your creatures move as one.", def.Speaker);
        yield return Say("By the authority of the Trail, I name you... Trail Champion!", def.Speaker);
        State.Set("master");
        _game.Achievements.Unlock(TrailAchievements.Champion);
        _game.SaveGame();
        bool done = false;
        Game.Scenes.Push(new CreditsScene(_game, () => done = true), Transition.Fade(1.5f, MicroEngine.Color.White));
        yield return Wait.Until(() => done);
    }

    private IEnumerator WildEncounter(Creature wild)
    {
        State.Seen.Add(wild.SpeciesId);
        var battle = new Battle(State.Party, [wild], null, State.Bag, new SeededRandom(_rng.Next()));
        yield return RunBattle(battle, "battle", null, _ => { }, allowWhiteout: true);
    }

    private IEnumerator RunTrainerBattle(TrainerDef def, string displayName, Action<BattleOutcome> result, bool allowWhiteout)
    {
        var info = def.ToInfo() with { Name = displayName };
        foreach (var c in info.Team) State.Seen.Add(c.SpeciesId);
        var battle = new Battle(State.Party, info.Team, info, State.Bag, new SeededRandom(_rng.Next()));
        string music = def.IsMaster ? "master" : "trainer";
        yield return RunBattle(battle, music, def, result, allowWhiteout);
    }

    private IEnumerator RunBattle(Battle battle, string music, TrainerDef? trainer, Action<BattleOutcome> result, bool allowWhiteout)
    {
        Audio.PlayMusic(music, 0f);
        Audio.Play("encounter", 0.8f);
        for (int i = 0; i < 6; i++)
        {
            _flash = i % 2 == 0;
            yield return Wait.Seconds(0.09f);
        }
        _flash = false;
        bool done = false;
        var outcome = BattleOutcome.None;
        var setup = new BattleSetup(_map.Def.Backdrop == Backdrop.Indoor && trainer?.IsMaster == true ? Backdrop.Hall : _map.Def.Backdrop, music, trainer);
        Game.Scenes.Push(new BattleScene(_game, battle, setup, o => { outcome = o; done = true; }), new BattleIntroTransition());
        yield return Wait.Until(() => done);
        if (outcome == BattleOutcome.Won)
        {
            State.BattlesWon++;
            _game.Achievements.Unlock(TrailAchievements.FirstWin);
            _game.Achievements.AddProgress(TrailAchievements.Seasoned);
            // The battle only announces the prize; the purse is paid here.
            if (battle.Trainer is { } paid) State.Money += paid.Prize;
        }
        result(outcome);
        if (outcome == BattleOutcome.Caught && battle.Caught is { } caught)
        {
            State.Caught.Add(caught.SpeciesId);
            _game.Achievements.Unlock(TrailAchievements.FirstCatch);
            if (battle.SentToBox) State.Box.Add(caught);
        }
        yield return EvolutionChecks();
        if (outcome == BattleOutcome.Lost && allowWhiteout) yield return WhiteOutScript();
    }

    private IEnumerator EvolutionChecks()
    {
        foreach (var c in State.Party.Where(c => c.CanEvolve && !c.Fainted).ToList())
        {
            bool done = false;
            Game.Scenes.Push(new EvolutionScene(_game, c, () => done = true), Transition.Fade(0.6f, MicroEngine.Color.White));
            yield return Wait.Until(() => done);
        }
    }

    private IEnumerator WhiteOutScript()
    {
        _fade = 1;
        int lost = State.WhiteOut();
        // Move to the rest point while the screen is dark, before any frame passes: Update copies the
        // player's tile into the state every frame, which would overwrite the respawn tile with the
        // old position if the old player were still in place during the narration.
        LoadMap(State.MapId, State.X, State.Y, State.Facing);
        yield return Say("Tavi hurried back to safety, carrying the exhausted creatures...");
        if (lost > 0) yield return Say($"Tavi dropped ${lost} in the rush...");
        yield return FadeIn(0.6f);
        string who = State.MapId == "shrine" ? "Keeper Rhea" : "Mom";
        yield return Say("Your creatures are rested now. Please be careful out there!", who);
    }
}
