using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MeadowRun.Gameplay;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MeadowRun.Scenes;

/// <summary>Plays one level attempt and routes the outcome: retry, game over, map or ending.</summary>
internal sealed class PlayScene(MeadowGame game, int levelIndex, int checkpoint = 0) : Scene
{
    private Stage _stage = null!;
    private StageView _view = null!;
    private bool _leaving;
    private readonly HudLabels _hud = new();
    private readonly NumberLabel _flag = new(), _bonus = new(), _total = new();
    private int _coinsAtStart, _scoreAtStart;

    public Stage Stage => _stage;
    public override string? Describe() => _stage is null ? null
        : FormattableString.Invariant($"x={_stage.Player.Position.X:0} y={_stage.Player.Position.Y:0}{(_stage.Player.Grounded ? " grounded" : "")}");
    public int LevelIndex => levelIndex;
    public LevelDef Def => Levels.All[levelIndex];

    /// <summary>
    /// Hashes the level and checkpoint, whether the scene is leaving, the session totals at the start (the clear
    /// record uses them) and the stage (the stage's actors are in the scene's world, which the engine hashes with the
    /// scene). Leaves out the stage view and HUD/tally labels: presentation writes them (squash, label text caches).
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(levelIndex);
        hash.Add(checkpoint);
        hash.Add(_leaving);
        hash.Add(_coinsAtStart);
        hash.Add(_scoreAtStart);
        hash.Add(_stage);
    }

    public override void Enter()
    {
        _stage = new Stage(new Level(Def), game.Session, Objects, checkpoint)
        {
            SoundSink = (name, volume, pitch, pan) => game.Sfx(name, volume, pitch, pan),
            MusicSink = game.Music,
            StompSink = () => game.Achievements.AddProgress(MeadowAchievements.Stomper),
        };
        _view = new StageView(game.Art, Font, game.Lights);
        _view.Bind(_stage);
        _coinsAtStart = game.Session.Coins;
        _scoreAtStart = game.Session.Score;
        game.Session.LifeGained -= OnLife;
        game.Session.LifeGained += OnLife;
        game.Session.CoinCollected -= OnCoin;
        game.Session.CoinCollected += OnCoin;
        game.Music(_stage.Boss is not null ? "castle" : Def.Music);
    }

    public override void Exit()
    {
        game.Session.LifeGained -= OnLife;
        game.Session.CoinCollected -= OnCoin;
    }

    private void OnLife() => game.Sfx("oneup", 0.9f);
    private void OnCoin() => game.Achievements.AddProgress(MeadowAchievements.CoinPurse);

    public override void Resume() => Audio.PauseMusic(false);

    /// <param name="dt">The step of the scene's world: 0 while a hit-stop freezes it (the stage reads that from the world's clock).</param>
    public override void Update(float dt)
    {
        if (_leaving) return;
        if (Input.Pressed("pause") && _stage.Phase == StagePhase.Playing)
        {
            game.Sfx("pause", 0.8f);
            Audio.PauseMusic(true);
            Game.Scenes.Push(new PauseScene(game, this));
            return;
        }
        var input = new PlayerInput(
            Input.Down("left"), Input.Down("right"), Input.Down("down"),
            Input.Pressed("jump"), Input.Down("jump"), Input.Down("run"), Input.Pressed("run"));
        _view.SetCrouch(input.Down && !input.Left && !input.Right);
        // Effects, shake and squash keep moving through a hit-stop: they advance by the engine's step.
        float step = EngineStep;
        _stage.Update(step, input);
        _view.Observe(_stage, step);
        if (_stage.Player.Form == PipForm.Ember) game.Achievements.Unlock(MeadowAchievements.EmberHeart);
        if (_stage.Outcome != StageOutcome.None) Finish(_stage.Outcome);
    }

    private void Finish(StageOutcome outcome)
    {
        _leaving = true;
        switch (outcome)
        {
            case StageOutcome.Died:
                bool over = game.Session.LoseLife();
                Game.Scenes.Replace(over ? new GameOverScene(game) : new LevelCardScene(game, levelIndex, _stage.CheckpointReached), Transition.Iris(new Vector2(160, 90), 0.9f, Pal.Ink));
                break;
            case StageOutcome.Cleared:
                if (levelIndex == 0) game.Achievements.Unlock(MeadowAchievements.FirstCourse);
                game.Session.Form = _stage.Player.Form;
                bool unlocked = game.RecordClear(levelIndex, game.Session.Score - _scoreAtStart, game.Session.Coins - _coinsAtStart);
                Game.Scenes.Replace(new MapScene(game, unlocked ? levelIndex + 1 : -1, levelIndex), Transition.Diamonds(0.8f, Pal.Ink));
                break;
            case StageOutcome.Won:
                game.Achievements.Unlock(MeadowAchievements.Sunstone);
                game.RecordClear(levelIndex, game.Session.Score - _scoreAtStart, game.Session.Coins - _coinsAtStart);
                Game.Scenes.Replace(new CreditsScene(game), Transition.Fade(1.6f, Pal.Cream));
                break;
        }
    }

    /// <summary>Restarts this level from the beginning (pause menu).</summary>
    public void Restart()
    {
        _leaving = true;
        Game.Scenes.Replace(new LevelCardScene(game, levelIndex, 0), Transition.Diamonds(0.6f, Pal.Ink));
    }

    public override void Draw(IRenderer r)
    {
        _view.Draw(r, _stage, DrawTime.Of(this), Objects.Blend);
        Ui.Hud(r, Font, game.Art, game.Session, _stage, Age, _hud);
        if (_stage.Phase == StagePhase.Tally) DrawTally(r);
        if (_stage.Phase == StagePhase.Victory)
            Ui.Centered(r, Font, "THE SUNSTONE IS HOME!", 160, 60, Pal.Butter, 2);
    }

    private void DrawTally(IRenderer r)
    {
        var box = new RectF(92, 48, 136, 66);
        r.Panel(box, Ui.Card);
        Ui.Centered(r, Font, "COURSE CLEAR!", 160, 54, Pal.Butter);
        Ui.Text(r, Font, "Flag", new Vector2(104, 70), Pal.Haze);
        string flag = _flag.For(_stage.FlagScore), bonus = _bonus.For(_stage.TimeBonus);
        Ui.Text(r, Font, flag, new Vector2(214 - Font.MeasureWidth(flag), 70));
        Ui.Text(r, Font, "Time", new Vector2(104, 82), Pal.Haze);
        Ui.Text(r, Font, bonus, new Vector2(214 - Font.MeasureWidth(bonus), 82));
        r.FillRect(new RectF(104, 94, 112, 1), Pal.Gold.WithAlpha(0.6f));
        string total = _total.For(game.Session.Score);
        Ui.Text(r, Font, "Score", new Vector2(104, 98), Pal.Gold);
        Ui.Text(r, Font, total, new Vector2(214 - Font.MeasureWidth(total), 98), Pal.Butter);
    }
}

/// <summary>"World N" card shown before every attempt: level name, subtitle and lives.</summary>
internal sealed class LevelCardScene(MeadowGame game, int levelIndex, int checkpoint) : Scene
{
    private const float Duration = 2.0f;
    private bool _started;
    private string _world = "", _name = "", _lives = "";

    /// <summary>Hashes the level, the checkpoint and whether the level has started; the card's texts are presentation.</summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(levelIndex);
        hash.Add(checkpoint);
        hash.Add(_started);
    }

    public override void Enter()
    {
        var def = Levels.All[levelIndex];
        _world = $"WORLD {def.Number}";
        _name = def.Name.ToUpperInvariant();
        _lives = "×  " + Ui.N(game.Session.Lives);
        game.Music("");
    }

    public override void Update(float dt)
    {
        if (_started) return;
        if (Age >= Duration || (Age > 0.4f && Input.Pressed("confirm")))
        {
            _started = true;
            Game.Scenes.Replace(new PlayScene(game, levelIndex, checkpoint), Transition.Iris(new Vector2(160, 100), 0.8f, Pal.Ink));
        }
    }

    public override void Draw(IRenderer r)
    {
        var def = Levels.All[levelIndex];
        r.FillRect(new RectF(0, 0, 320, 180), Pal.Ink);
        // Biome-coloured ribbon behind the title.
        var ribbon = def.Biome switch { Biome.Meadow => Pal.Fern, Biome.Caves => Pal.Plum, Biome.Sky => Pal.Ocean, _ => Pal.Wine };
        float slide = Ease.OutCubic(Math.Clamp(Age / 0.4f, 0, 1));
        r.FillRect(new RectF(0, 58, 320 * slide, 40), ribbon);
        r.FillRect(new RectF(0, 58, 320 * slide, 2), ribbon.Shade(1.4f));
        r.FillRect(new RectF(0, 96, 320 * slide, 2), ribbon.Shade(0.6f));
        Ui.Centered(r, Font, _world, 160, 46, Pal.Haze);
        Ui.Centered(r, Font, _name, 160, 66, Pal.Butter, 2);
        Ui.Centered(r, Font, def.Subtitle, 160, 86, Pal.Cream);
        var pip = game.Art.Pip(game.Session.Form, "idle")[(int)(Age * 1.6f) % 2];
        r.Draw(pip, new Vector2(142, 136));
        Ui.Text(r, Font, _lives, new Vector2(156, 124));
        if (checkpoint > 0) Ui.Centered(r, Font, "from the checkpoint", 160, 146, Pal.Dusk);
    }
}

/// <summary>Pause overlay: resume, restart the level or quit to the map.</summary>
internal sealed class PauseScene(MeadowGame game, PlayScene play) : Scene
{
    private readonly SelectionList _menu = new("Resume", "Restart level", "Quit to map");
    public override bool IsOverlay => true;

    /// <summary>Hashes the menu cursor.</summary>
    public override void HashState(StateHasher hash) => hash.Add(_menu.Index);

    public override void Enter() => _menu.Moved += () => game.Sfx("menu_move", 0.7f);

    public override void Update(float dt)
    {
        if (Input.Pressed("pause") || Input.Pressed("cancel")) { Close(); return; }
        if (_menu.Update(Input) is not { } choice) return;
        game.Sfx("menu_select", 0.8f);
        switch (choice)
        {
            case 0: Close(); break;
            case 1: Game.Scenes.Pop(); play.Restart(); break;
            case 2:
                Game.Scenes.Pop();
                Game.Scenes.Replace(new MapScene(game, -1, play.LevelIndex), Transition.Diamonds(0.6f, Pal.Ink));
                break;
        }
    }

    private void Close()
    {
        game.Sfx("menu_back", 0.7f);
        Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, 320, 180), Pal.Ink.WithAlpha(0.6f));
        var box = new RectF(100, 56, 120, 72);
        r.Panel(box, Ui.Card);
        Ui.Centered(r, Font, "PAUSED", 160, 63, Pal.Butter);
        Ui.Menu(r, Font, _menu, 160, 82, Age, 13, 104);
    }
}
