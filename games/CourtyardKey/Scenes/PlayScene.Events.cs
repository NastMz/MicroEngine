using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using MicroEngine.Scenes;

namespace CourtyardKey.Scenes;

/// <summary>Talking, reading, treasure, item fanfares, death and the ending hand-off.</summary>
public sealed partial class PlayScene
{
    /// <summary>The item currently held above the hero's head (drawn by the HUD layer).</summary>
    public Item PresentedItem { get; private set; }

    public bool TryInteract(Hero hero)
    {
        var probe = hero.InteractProbe;
        foreach (var a in Actors)
            if (a is IInteractable { CanInteract: true } i && a.Bounds.Inflate(2).Intersects(probe))
            {
                i.Interact(hero);
                return true;
            }
        return false;
    }

    public void ShowText(string? speaker, params string[] pages) => ShowText(speaker, pages, null);

    public void ShowText(string? speaker, string[] pages, Action? closed)
    {
        Game.Scenes.Push(new DialogueScene(speaker, pages, closed));
    }

    public void Talk(Npc npc)
    {
        var talk = npc.Def.Talk(Progress);
        ShowText(npc.Def.Name, talk.Pages, () =>
        {
            switch (talk.Effect)
            {
                case TalkEffect.GiveSword: GiveItem(Item.Sword, 1, null); break;
                case TalkEffect.OpenShop: Game.Scenes.Push(new ShopScene(this)); break;
            }
        });
    }

    public void OpenChest(Chest chest)
    {
        chest.Opened = true;
        Progress.Set(Spawner.ChestFlag(Room, chest.Index));
        Audio.Play("chest");
        Hero.Facing = DirExtensions.FromVector(chest.Center - Hero.Center);
        Coroutines.Start(OpenChestRoutine(chest));
    }

    private System.Collections.IEnumerator OpenChestRoutine(Chest chest)
    {
        Mode = PlayMode.Cutscene;
        yield return Wait.Seconds(0.35f);
        Burst(chest.Center + new Vector2(0, -6), World.Fx.Sparkle, 10, additive: true);
        GiveItem(chest.Contents.Item, chest.Contents.Amount, null);
    }

    /// <summary>The classic "item get" moment: pose, fanfare, caption; then the reward applies.</summary>
    public void GiveItem(Item item, int amount, Action? done) => Coroutines.Start(ItemGetRoutine(item, amount, done));

    private System.Collections.IEnumerator ItemGetRoutine(Item item, int amount, Action? done)
    {
        Mode = PlayMode.Cutscene;
        Hero.SetState(HeroState.Pose);
        Hero.Facing = Dir.Down;
        PresentedItem = item;
        bool major = item is Item.Sword or Item.Boomerang or Item.BossKey or Item.HeartContainer or Item.CourtyardKey;
        if (major) Audio.PauseMusic(true);
        Audio.Play(major ? "fanfare_big" : item == Item.Gems ? "gem" : "fanfare");
        Burst(Hero.Position + new Vector2(0, -24), World.Fx.Sparkle, 18, additive: true);
        yield return Wait.Seconds(major ? 1.3f : 0.6f);
        Apply(item, amount);
        bool closed = false;
        ShowText(null, ItemText(item, amount), () => closed = true);
        yield return Wait.Until(() => closed);
        PresentedItem = Item.None;
        if (major) Audio.PauseMusic(false);
        if (item == Item.CourtyardKey) { yield return Ending(); yield break; }
        Hero.SetState(HeroState.Walk);
        Mode = PlayMode.Play;
        if (major) SaveGame();
        done?.Invoke();
    }

    private void Apply(Item item, int amount)
    {
        switch (item)
        {
            case Item.Sword: Progress.HasSword = true; break;
            case Item.Boomerang:
                Progress.HasBoomerang = true;
                G.Achievements.Unlock(CourtyardAchievements.WindCatcher);
                break;
            case Item.SmallKey: Progress.SmallKeys += amount; Audio.Play("key"); break;
            case Item.BossKey: Progress.HasBossKey = true; break;
            case Item.HeartContainer:
                Hero.Health.RaiseMax(2, fill: true);
                Progress.MaxHealth = Hero.Health.Max;
                break;
            case Item.Gems: Progress.AddGems(amount); break;
            case Item.CourtyardKey: Progress.HasCourtyardKey = true; break;
        }
    }

    public static string[] ItemText(Item item, int amount) => item switch
    {
        Item.Sword => ["You got Oswin's Sword!", "Press {confirm} to swing it. Cut bushes, break pots, and stand your ground."],
        Item.Boomerang => ["You got the Wind Boomerang!", "Press {action} to throw it. It stuns foes, fetches treasure and strikes switches out of reach."],
        Item.SmallKey => ["You found a Small Key! Walk into a locked door to open it."],
        Item.BossKey => ["You found the Warden's Key! It opens the great door of the Pillar Hall."],
        Item.HeartContainer => ["You got a Heart Container! Your life grows by one whole heart."],
        Item.Gems => [$"You found {amount} gems!"],
        Item.CourtyardKey => ["You recovered the Courtyard Key!", "Its light hums warmly. The bells beneath the ruins fall silent at last."],
        _ => ["..."],
    };

    public void TakeCourtyardKey(Pedestal pedestal)
    {
        pedestal.HasKey = false;
        GiveItem(Item.CourtyardKey, 1, null);
    }

    private System.Collections.IEnumerator Ending()
    {
        Audio.StopMusic(1f);
        SaveGame(toast: false);
        yield return FadeOut(1.4f, Visuals.Pal.White);
        Game.Scenes.Replace(new EndingScene(G, Progress), Transition.Fade(0.6f, Visuals.Pal.White));
    }

    // ------------------------------------------------------------------ death

    private System.Collections.IEnumerator DeathSequence()
    {
        Mode = PlayMode.Cutscene;
        Audio.StopMusic(0.2f);
        Audio.Play("death");
        // The hero's fall plays on the engine's step, the one the coroutine itself advances by.
        for (float t = 0; t < 1.8f; t += EngineStep)
        {
            Hero.Update(EngineStep);
            yield return null;
        }
        Audio.Play("gameover");
        Game.Scenes.Push(new GameOverScene(this));
    }

    /// <summary>Respawns at the last entrance with full hearts; progress (keys, items, flags) is kept.</summary>
    public void Continue()
    {
        Progress.Deaths++;
        Objects.Destroy(_room);
        Particles.Clear();
        Glows.Clear();
        Hero.Revive(Hero.Health.Max);
        Hero.Controllable = true;
        PlaceAtCheckpoint(Progress.Entrance);
        SaveGame(toast: false);
        Coroutines.Start(FadeIn(0.6f));
    }
}
