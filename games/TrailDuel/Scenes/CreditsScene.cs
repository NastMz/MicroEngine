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

/// <summary>The champion celebration followed by scrolling credits over the player's party parade.</summary>
internal sealed class CreditsScene(TrailDuelGame game, Action onDone) : Scene
{
    private static readonly string[] Lines =
    [
        "TRAIL DUEL", "", "", "A creature trail adventure", "built on MicroEngine", "", "",
        "STARRING", "", "Tavi", "and partner", "", "", "WITH", "", "Wren, the rival", "Mentor Ilse", "Master Odessa",
        "Keeper Rhea", "Picnicker Mira", "Scout Bram", "Hiker Doran", "Birdkeeper Sola", "Apprentice Lio", "", "",
        "CREATURES", "", "Emberpup  Blazehound", "Puddlepip  Tidefang", "Sprigfawn  Grovestag", "Breezel  Puffmop  Pebblit",
        "Fizzbit  Mossback  Gustwing", "Voltiger  Bouldox", "", "",
        "CREATURE & TRAINER ART", "", "Openmon collection", "by Screen Smith and artists (CC0)", "", "BATTLE EFFECTS & JINGLES", "", "Ninja Adventure", "by Pixel-boy and AAA (CC0)", "", "VILLAGE ART", "", "Serene Village revamped", "by LimeZu (CC BY 4.0)", "",
        "MUSIC, SOUND & THE REST", "", "Generated in code at load time", "", "",
        "Thank you for walking the trail!", "", "", "", "THE END",
    ];

    /// <summary>Lines in capitals are headers (worked out once, not per line per frame).</summary>
    private static readonly bool[] Headers = Array.ConvertAll(Lines, line => line == line.ToUpperInvariant() && line.Length > 0);

    private const float LineHeight = 14;
    private CachedText _timeText;
    private float _scroll;
    /// <summary>The scroll in the scene's objects, so the world draws the rolling credits between steps.</summary>
    private EntityId _scrollEntity;
    private bool _champion = true;
    private float _phase;
    private bool _leaving;

    public override void Enter()
    {
        Audio.PlayMusic("victory", 0f);
        _scrollEntity = Objects.Create();
    }

    /// <summary>
    /// State hash: the champion card or the roll, its phase clock, the scroll (it decides when the credits end) and
    /// whether it is leaving. Left out: the time text cache (the scroll's entity is in the scene's objects, which the
    /// engine hashes).
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_champion);
        hash.Add(_phase);
        hash.Add(_scroll);
        hash.Add(_leaving);
    }

    public override void Update(float dt)
    {
        Advance(dt);
        Objects.Local(_scrollEntity).Position2D = new Vector2(0, _scroll);
    }

    private void Advance(float dt)
    {
        _phase += dt;
        if (_champion)
        {
            if (_phase > 2.5f && (Input.Pressed("confirm") || _phase > 7f))
            {
                _champion = false;
                _phase = 0;
                Audio.PlayMusic("credits", 0.6f);
            }
            return;
        }
        float end = Lines.Length * LineHeight + 120;
        float speed = Input.Down("confirm") ? 70 : 18;
        _scroll = MathF.Min(end - 60, _scroll + dt * speed);
        if (_scroll >= end - 60 && !_leaving && (Input.Pressed("confirm") || _phase > Lines.Length * LineHeight / 18 + 10))
        {
            _leaving = true;
            onDone();
            Game.Scenes.Reset(new TitleScene(game), Transition.Fade(1.5f, Pal.Ink));
        }
    }

    public override void Draw(IRenderer r)
    {
        r.Draw(game.Sprite("backdrop:sunset"), Vector2.Zero);
        for (int i = 0; i < 3; i++)
            r.Draw(game.Sprite("cloud"), new Vector2(MathF.Round((i * 140 + Age * 6) % 440 - 60), 10 + i * 18), Color.Hex("fde2c8").WithAlpha(0.75f));
        var party = game.State.Party;
        for (int i = 0; i < party.Count; i++)
        {
            float x = _champion ? 160 + (i - (party.Count - 1) / 2f) * MathF.Min(60, 280f / Math.Max(1, party.Count)) : (i * 60 + Age * 22) % 400 - 40;
            float hop = MathF.Abs(MathF.Sin(Age * 3 + i)) * (_champion ? 6 : 3);
            r.Draw(game.Sprite(SpriteKeys.Shadow[party[i].SpeciesId]), new Vector2(MathF.Round(x), 149), Color.White.WithAlpha(1 - hop / 12f));
            r.Draw(game.Sprite(SpriteKeys.Front[party[i].SpeciesId]), new Vector2(MathF.Round(x), MathF.Round(150 - hop)));
        }
        if (_champion)
        {
            UiKit.Title(r, Font, "TRAIL CHAMPION", new Vector2(160, 40), 2, Pal.Gold);
            UiKit.Title(r, Font, $"Congratulations, {GameState.PlayerName}!", new Vector2(160, 66));
            var t = TimeSpan.FromSeconds(game.State.PlaySeconds);
            string time = _timeText.Get((int)t.TotalHours * 60 + t.Minutes, game.State.Caught.Count,
                static (minutes, caught) => $"Time {minutes / 60}:{minutes % 60:00}   Caught {caught}/{SpeciesDex.All.Count}");
            UiKit.Title(r, Font, time, new Vector2(160, 80), 1, Pal.Cream);
            for (int i = 0; i < 8; i++)
            {
                float a = Age * 2 + i * MathF.Tau / 8;
                r.Draw(game.Sprite("sparkle"), new Vector2(160 + MathF.Cos(a) * 90, 52 + MathF.Sin(a) * 26));
            }
            return;
        }
        float scroll = Objects.DrawPosition2D(_scrollEntity).Y;
        for (int i = 0; i < Lines.Length; i++)
        {
            float y = TrailDuelGame.CanvasHeight + i * LineHeight - scroll;
            if (y < -10 || y > 140) continue;
            bool header = Headers[i];
            UiKit.Title(r, Font, Lines[i], new Vector2(160, y), Lines[i] is "TRAIL DUEL" or "THE END" ? 2 : 1, header ? Pal.Gold : Pal.White);
        }
    }
}
