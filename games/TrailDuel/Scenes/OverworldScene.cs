using System.Collections;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Scenes;

/// <summary>
/// Exploration: grid movement with turn-in-place and bump feedback, tall-grass encounters,
/// ledges, warps with fades, NPC dialogue, trainer line-of-sight and story cutscenes.
/// Cutscenes run as coroutines; while one runs the player has no free control.
/// </summary>
internal sealed partial class OverworldScene : Scene
{
    private const float TurnDelay = 0.09f;

    private readonly TrailDuelGame _game;
    private readonly bool _intro;
    private readonly Camera2D _camera = new(320, 180) { FollowSharpness = 0 };
    /// <summary>The camera's position in the scene's objects, so the world draws it between steps like the actors.</summary>
    private EntityId _cameraEntity;
    private readonly ParticleSystem _dust = new(128, 17);
    private readonly List<(int X, int Y, float Time)> _rustles = [];
    private readonly SeededRandom _rng;
    private MapRuntime _map = null!;
    private Actor _player = null!;
    private TextPanel _text = null!;
    private ChoicePrompt _choice = null!;
    private int _busy;
    private float _turnTimer, _bumpTimer, _bannerTime = -10, _fade, _npcClock;
    private string _bannerText = "";
    private Actor? _bubbleOn;
    private float _bubbleTime;
    private string? _previewSpecies;
    private bool _flash;
    private bool _hopped;

    public OverworldScene(TrailDuelGame game, bool intro = false)
    {
        _game = game;
        _intro = intro;
        _rng = new SeededRandom(game.Random.Next());
    }

    private GameState State => _game.State;
    public MapRuntime Map => _map;
    public Actor Player => _player;
    public override string? Describe() => _player is null ? null : FormattableString.Invariant($"x={_player.X} y={_player.Y}");
    public bool Busy => _busy > 0;
    /// <summary>On touch screens: a question keeps the d-pad (up and down choose) with A and B; plain text keeps A and B only.</summary>
    public override IReadOnlyCollection<string>? TouchActions =>
        _choice is { Menu: not null } ? ChoiceActions : _text is { Active: true } ? TextActions : null;
    internal static readonly IReadOnlyCollection<string> TextActions = ["confirm", "cancel"];
    internal static readonly IReadOnlyCollection<string> ChoiceActions = ["up", "down", "confirm", "cancel"];
    public TextPanel Text => _text;

    /// <summary>
    /// State hash, in three sections so a divergence names the part: <c>overworld</c> (map, cutscene count, walk and
    /// bump timers, the wander clock, the pending ledge landing, the scene's generator, the text box and the open
    /// choice), <c>overworld.player</c> and <c>overworld.npcs</c> (count then each actor). Left out: the camera (its
    /// position, like the actors', is in the scene's objects, which the engine hashes), dust and grass rustles, the area
    /// banner, fades, flashes, the heal glow, the "!" bubble and the starter preview (presentation that cutscene coroutines drive; the coroutines
    /// themselves are hashed by the engine), and the tile caches drawing fills.
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Section("overworld");
        hash.Add(_map?.Def.Id);
        hash.Add(_busy);
        hash.Add(_turnTimer);
        hash.Add(_bumpTimer);
        hash.Add(_npcClock);
        hash.Add(_hopped);
        hash.Add(_rng);
        hash.Add(_text);
        hash.Add(_choice);
        hash.Section("overworld.player");
        hash.Add(_player);
        hash.Section("overworld.npcs");
        if (_map is null) return;
        hash.Add(_map.Npcs.Count);
        foreach (var npc in _map.Npcs) hash.Add(npc);
    }

    public override void Enter()
    {
        _text = new TextPanel(Font, Audio);
        _choice = new ChoicePrompt(Font, Audio);
        _cameraEntity = Objects.Create();
        LoadMap(State.MapId, State.X, State.Y, State.Facing);
        if (_intro) RunScript(IntroScript());
    }

    public override void Resume()
    {
        Audio.PlayMusic(_map.Def.Music);
    }

    private void LoadMap(string id, int x, int y, Direction facing)
    {
        var def = Maps.Get(id);
        bool newArea = _map is null || _map.Def.Name != def.Name;
        _map?.Unload();
        _player?.Destroy();
        _map = new MapRuntime(def, State, Objects);
        _player = new Actor(CharacterLook.Player, x, y, facing, Objects);
        State.MapId = id;
        State.X = x;
        State.Y = y;
        State.Facing = facing;
        _camera.Bounds = new RectF(0, 0, def.Width * 16, def.Height * 16);
        SnapCamera();
        _rustles.Clear();
        _dust.Clear();
        Audio.PlayMusic(def.Music);
        if (newArea && !def.Indoor) { _bannerText = def.Name; _bannerTime = 0; }
    }

    private void SnapCamera()
    {
        _camera.CenterOn(_player.PixelPosition + new Vector2(8, 8));
        Objects.Local(_cameraEntity).Position2D = _camera.Position;
        Objects.Teleport(_cameraEntity);
    }

    public override void Update(float dt)
    {
        State.PlaySeconds += dt;
        _bannerTime += dt;
        _npcClock += dt;
        _dust.Update(dt);
        // Expired rustles removed in place, keeping the order (a lambda over Age allocated every update).
        for (int i = _rustles.Count - 1; i >= 0; i--)
            if (Age - _rustles[i].Time > 0.45f) _rustles.RemoveAt(i);
        _text.Update(dt, Input);
        _choice.Update(dt, Input);
        foreach (var npc in _map.Npcs) npc.Update(dt);
        bool arrived = _player.Update(dt);
        State.X = _player.X;
        State.Y = _player.Y;
        State.Facing = _player.Facing;
        _camera.Follow(_player.PixelPosition + new Vector2(8, 8), dt);
        _camera.Update(dt);
        Objects.Local(_cameraEntity).Position2D = _camera.Position;
        if (arrived) OnStepEnd();
        if (Busy || _player.Moving) return;

        if (Input.Pressed("pause"))
        {
            Audio.Play("menu");
            Game.Scenes.Push(new StartMenuScene(_game));
            return;
        }
        if (Input.Pressed("confirm")) { Interact(); if (Busy) return; }
        HandleWalkInput(dt);
        UpdateWanderers();
    }

    private void HandleWalkInput(float dt)
    {
        Direction? dir = Input.Down("up") ? Direction.Up : Input.Down("down") ? Direction.Down
            : Input.Down("left") ? Direction.Left : Input.Down("right") ? Direction.Right : null;
        if (dir is null) { _turnTimer = 0; return; }
        var d = dir.Value;
        if (_player.Facing != d && Input.Pressed(DirName(d)))
        {
            // A fresh tap toward a new direction turns in place first.
            _player.Facing = d;
            _turnTimer = TurnDelay;
            return;
        }
        if (_turnTimer > 0) { _turnTimer -= dt; if (_turnTimer > 0) return; }
        _player.Facing = d;
        switch (_map.CheckStep(_player, d, _player))
        {
            case StepKind.Step:
                _player.StartStep(d, Input.Down("cancel") ? Actor.RunStepTime : Actor.StepTime);
                break;
            case StepKind.Hop:
                _player.StartHop(d);
                _hopped = true;
                Audio.Play("ledge");
                break;
            default:
                _bumpTimer -= dt;
                if (_bumpTimer <= 0)
                {
                    Audio.Play("bump", 0.8f);
                    _bumpTimer = 0.32f;
                }
                break;
        }
    }

    private static string DirName(Direction d) => d switch { Direction.Up => "up", Direction.Down => "down", Direction.Left => "left", _ => "right" };

    private void UpdateWanderers()
    {
        if (_npcClock < 0.5f) return;
        _npcClock = 0;
        foreach (var npc in _map.Npcs)
        {
            if (npc.Def is not { Wander: true } def || npc.Moving || _rng.Next(5) != 0) continue;
            var dir = (Direction)_rng.Next(4);
            var (dx, dy) = dir.Delta();
            npc.Facing = dir;
            int nx = npc.X + dx, ny = npc.Y + dy;
            if (Math.Abs(nx - def.X) > 2 || Math.Abs(ny - def.Y) > 2) continue;
            if (_map.CheckStep(npc, dir, _player) == StepKind.Step) npc.StartStep(dir, Actor.StepTime * 1.4f);
        }
    }

    private void OnStepEnd()
    {
        var kind = _map.KindAt(_player.X, _player.Y);
        if (_hopped)
        {
            _hopped = false;
            Audio.Play("land", 0.7f);
            _dust.Emit(_player.PixelPosition + new Vector2(8, 15), DustSpec, 8);
        }
        if (kind == TileKind.TallGrass)
        {
            _rustles.Add((_player.X, _player.Y, Age));
            Audio.Play("grass", 0.6f, 1f, 0.1f);
        }
        if (_map.WarpAt(_player.X, _player.Y) is { } warp) { RunScript(WarpTo(warp)); return; }
        if (_map.TriggerAt(_player.X, _player.Y) is { } trigger && TriggerScript(trigger.Id) is { } script) { RunScript(script); return; }
        foreach (var npc in _map.Npcs)
        {
            if (npc.Def?.Trainer is null || State.Has(npc.Def.DefeatFlag)) continue;
            if (_map.TrainerSees(npc, _player.X, _player.Y)) { RunScript(TrainerEncounter(npc)); return; }
        }
        if (kind == TileKind.TallGrass && _map.Def.Encounters is { } table && State.AnyHealthy && _rng.Next(100) < table.RatePercent)
            RunScript(WildEncounter(table.Roll(_rng)));
    }

    private static readonly ParticleSpec DustSpec = new()
    {
        StartColor = Color.Hex("f4ead2"), EndColor = Color.Hex("f4ead200"), Life = (0.25f, 0.45f), Speed = (10, 30),
        Angle = (MathF.PI * 1.1f, MathF.PI * 1.9f), Size = (3, 1), Round = true,
    };

    private void Interact()
    {
        var (dx, dy) = _player.Facing.Delta();
        int fx = _player.X + dx, fy = _player.Y + dy;
        var kind = _map.KindAt(fx, fy);
        var npc = _map.NpcAt(fx, fy);
        if (npc is null && kind is TileKind.Counter or TileKind.Pedestal) npc = _map.NpcAt(fx + dx, fy + dy);
        if (npc is not null) { RunScript(Talk(npc)); return; }
        if (_map.SignAt(fx, fy) is { } sign) { RunScript(Read(sign.Pages)); return; }
        if (_map.ItemAt(fx, fy) is { } item) { RunScript(PickUp(item)); return; }
        if (_map.LockedDoorAt(fx, fy) is not null) { RunScript(Read(["The door is locked."])); return; }
        if (kind == TileKind.Pedestal && _map.Def.Id == "lab") { RunScript(Pedestal(fx - 4)); return; }
        string? flavor = kind switch
        {
            TileKind.Bookshelf => "The shelf is packed with books about creatures and their habitats.",
            TileKind.Tv => "A show about a legendary Trail Master is on. Tavi is glued to the screen!",
            TileKind.Kitchen => "Something delicious is simmering on the stove.",
            TileKind.Machine => "The machine hums softly, charting creature data.",
            TileKind.Altar => "A warm, gentle light glows from the altar.",
            TileKind.Statue => "A statue of a proud creature. It seems to watch over the hall.",
            TileKind.Shelf => "Neatly stacked potions and orbs.",
            TileKind.Water => "The water is crystal clear. Tiny fish dart about.",
            TileKind.Bed when _map.Def.Id == "home" => "Tavi's bed. Still warm.",
            _ => null,
        };
        if (flavor is not null) RunScript(Read([flavor]));
    }

    /// <summary>Starts a cutscene. Player control is suspended until it finishes.</summary>
    private void RunScript(IEnumerator script)
    {
        _busy++;
        Coroutines.Start(Wrap(script));
    }

    private IEnumerator Wrap(IEnumerator script)
    {
        yield return script;
        _busy--;
        // Holding a direction should not instantly walk after a dialogue closes.
        _turnTimer = 0.12f;
    }

    // ------------------------------------------------------------------ script helpers

    private IEnumerator Say(string text, string? speaker = null) => _text.Say(text, speaker);

    private IEnumerator Read(string[] pages)
    {
        foreach (var p in pages) yield return Say(p);
    }

    private IEnumerator Ask(string question, string? speaker, Action<bool> result)
    {
        _text.WaitForConfirmOnLast = false;
        _text.Show(question, speaker);
        while (!_text.PageComplete) yield return null;
        bool yes = false;
        yield return _choice.Choose(["YES", "NO"], i => yes = i == 0);
        _text.WaitForConfirmOnLast = true;
        _text.Close();
        result(yes);
    }

    private IEnumerator FadeOut(float seconds = 0.25f)
    {
        for (float t = 0; t < seconds; t += EngineStep) { _fade = t / seconds; yield return null; }
        _fade = 1;
    }

    private IEnumerator FadeIn(float seconds = 0.25f)
    {
        for (float t = 0; t < seconds; t += EngineStep) { _fade = 1 - t / seconds; yield return null; }
        _fade = 0;
    }

    private IEnumerator WaitFor(float seconds)
    {
        yield return Wait.Seconds(seconds);
    }

    private IEnumerator WalkActor(Actor actor, Direction dir, int steps, float stepTime = Actor.StepTime)
    {
        for (int i = 0; i < steps; i++)
        {
            actor.StartStep(dir, stepTime);
            while (actor.Moving) yield return null;
        }
    }

    private IEnumerator ShowBubble(Actor actor)
    {
        _bubbleOn = actor;
        _bubbleTime = 0;
        Audio.Play("exclaim");
        for (float t = 0; t < 0.7f; t += EngineStep) { _bubbleTime = t; yield return null; }
        _bubbleOn = null;
    }

    private void Face(Actor npc, Actor target)
    {
        int dx = target.X - npc.X, dy = target.Y - npc.Y;
        npc.Facing = Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? Direction.Right : Direction.Left) : (dy > 0 ? Direction.Down : Direction.Up);
    }
}
