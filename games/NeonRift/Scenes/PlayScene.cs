using NeonRift.Content;
using NeonRift.Gameplay;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace NeonRift.Scenes;

/// <summary>Plays one stage and routes the outcome: next stage card, ending, or game over.</summary>
internal sealed class PlayScene(RiftGame game, int stageIndex, int checkpoint = 0) : Scene
{
    private Stage _stage = null!;
    private StageView _view = null!;
    private bool _leaving;
    private readonly Autopilot? _autopilot = game.Launch.Auto ? new Autopilot() : null;
    private int _deathsAtStart;

    public Stage Stage => _stage;
    public override string? Describe() => _stage is null ? null
        : FormattableString.Invariant($"x={_stage.Player.Position.X:0} y={_stage.Player.Position.Y:0} shots={_stage.PlayerShots.Count()} aim={string.Join('/', _stage.PlayerShots.Select(s => $"{MathF.Sign(s.Velocity.X)},{MathF.Sign(s.Velocity.Y)}").Distinct())}");
    public int StageIndex => stageIndex;

    /// <summary>
    /// Adds the scene's own state (which stage, whether it is leaving, the deaths counted at its start, the autopilot's
    /// timers), then the stage's, which opens sections of its own (<see cref="Gameplay.Stage.HashState"/>). The view is
    /// left out: drawing only. The stage's actors are entities of the scene's <see cref="Scene.Objects"/>, which the
    /// engine hashes with the scene.
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(stageIndex);
        hash.Add(checkpoint);
        hash.Add(_leaving);
        hash.Add(_deathsAtStart);
        hash.Add(_autopilot);
        hash.Add(_stage);
    }

    public override void Enter()
    {
        var def = Stages.All[stageIndex];
        _stage = new Stage(def, game.Session, Objects, checkpoint, seed: 11 + stageIndex)
        {
            Final = stageIndex == Stages.Count - 1,
            SoundSink = (name, volume, pitch, pan) => game.Sfx(name, volume, pitch, pan),
            MusicSink = game.Music,
            Rigs = name => game.Art.Rig(name),
            Strips = name => game.Art.Frames(name).Count,
            DefeatSink = _ =>
            {
                game.Achievements.Unlock(RiftAchievements.FirstScrap);
                game.Achievements.AddProgress(RiftAchievements.ScrapDealer);
            },
        };
        _deathsAtStart = game.Session.Deaths;
        _stage.Player.God = game.Launch.God;
        _view = new StageView(game.Art, Font);
        game.Session.LifeGained -= OnLife;
        game.Session.LifeGained += OnLife;
        game.Music(def.Music);
    }

    public override void Exit() => game.Session.LifeGained -= OnLife;
    private void OnLife() => game.Sfx("one-up", 0.9f);
    public override void Resume() => Audio.PauseMusic(false);

    public override void Update(float dt)
    {
        if (_leaving) return;
        if (Input.Pressed("pause") && _stage.Phase is StagePhase.Playing or StagePhase.Boss or StagePhase.Warning)
        {
            game.Sfx("pause-in", 0.8f);
            Audio.PauseMusic(true);
            Game.Scenes.Push(new PauseScene(game));
            return;
        }
        var input = _autopilot?.Next(_stage, dt) ?? new PlayerInput(
            Input.Down("left"), Input.Down("right"), Input.Down("up"), Input.Down("down"),
            Input.Pressed("jump"), Input.Down("jump"), Input.Down("fire"), Input.Down("lock"));
        _stage.Update(dt, input);

        if (_stage.Finished)
        {
            _leaving = true;
            game.Session.Weapon = _stage.Player.Weapon.Kind;
            RecordClear();
            game.SaveHighScore();
            Scene next = stageIndex + 1 < Stages.Count ? new StageCardScene(game, stageIndex + 1) : new EndingScene(game);
            Game.Scenes.Replace(next, Transition.Fade(1.0f));
        }
        else if (_stage.Phase == StagePhase.GameOver && _stage.PhaseTime > 1.2f)
        {
            _leaving = true;
            Game.Scenes.Replace(new GameOverScene(game, stageIndex, _stage.CheckpointIndex), Transition.Fade(0.8f));
        }
    }

    private void RecordClear()
    {
        if (stageIndex == 0) game.Achievements.Unlock(RiftAchievements.Rooftops);
        if (checkpoint == 0 && game.Session.Deaths == _deathsAtStart) game.Achievements.Unlock(RiftAchievements.Untouched);
        if (stageIndex == Stages.Count - 1) game.Achievements.Unlock(RiftAchievements.Leviathan);
    }

    public override void Draw(IRenderer r)
    {
        _view.Draw(r, _stage);
        Ui.Scanlines(r);
    }
}

/// <summary>Pause overlay: resume or abandon the run.</summary>
internal sealed class PauseScene(RiftGame game) : Scene
{
    private readonly SelectionList _menu = new("RESUME", "QUIT TO TITLE");
    public override bool IsOverlay => true;

    /// <summary>Adds the menu cursor.</summary>
    public override void HashState(StateHasher hash) => hash.Add(_menu.Index);

    public override void Enter() => _menu.Moved += () => game.Sfx("menu-move", 0.6f);

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Close(); return; }
        if (_menu.Update(Input) is { } choice)
        {
            game.Sfx("menu-select", 0.7f);
            if (choice == 0) Close();
            else Game.Scenes.Reset(new TitleScene(game), Transition.Fade(0.6f));
        }
    }

    private void Close()
    {
        game.Sfx("pause-out", 0.8f);
        Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Ui.Ink.WithAlpha(0.6f));
        Ui.Neon(r, Font, "PAUSED", r.Width / 2f, 90, 3, Ui.Magenta, Age);
        Ui.Menu(r, Font, _menu, r.Width / 2f, 140, Age);
    }
}
