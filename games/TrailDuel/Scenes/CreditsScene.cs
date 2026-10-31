using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

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
        "CREATURE & TRAINER ART", "", "Openmon collection", "by Screen Smith and artists (CC0)", "", "BATTLE EFFECTS & JINGLES", "", "Ninja Adventure", "by Pixel-boy and AAA (CC0)", "", "WORLD, MUSIC & SOUND", "", "Generated in code at load time", "", "",
        "Thank you for walking the trail!", "", "", "", "THE END",
    ];

    private const float LineHeight = 14;
    private float _scroll;
    private bool _champion = true;
    private float _phase;
    private bool _leaving;

    public override void Enter() => Audio.PlayMusic("victory", 0f);

    public override void Update(float dt)
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
            r.Draw(game.Sprite("cshadow:" + party[i].SpeciesId), new Vector2(MathF.Round(x), 149), Color.White.WithAlpha(1 - hop / 12f));
            r.Draw(game.Sprite("front:" + party[i].SpeciesId), new Vector2(MathF.Round(x), MathF.Round(150 - hop)));
        }
        if (_champion)
        {
            UiKit.Title(r, Font, "TRAIL CHAMPION", new Vector2(160, 40), 2, Pal.Gold);
            UiKit.Title(r, Font, $"Congratulations, {GameState.PlayerName}!", new Vector2(160, 66));
            var t = TimeSpan.FromSeconds(game.State.PlaySeconds);
            UiKit.Title(r, Font, $"Time {(int)t.TotalHours}:{t.Minutes:00}   Caught {game.State.Caught.Count}/{SpeciesDex.All.Count}", new Vector2(160, 80), 1, Pal.Cream);
            for (int i = 0; i < 8; i++)
            {
                float a = Age * 2 + i * MathF.Tau / 8;
                r.Draw(game.Sprite("sparkle"), new Vector2(160 + MathF.Cos(a) * 90, 52 + MathF.Sin(a) * 26));
            }
            return;
        }
        for (int i = 0; i < Lines.Length; i++)
        {
            float y = 180 + i * LineHeight - _scroll;
            if (y < -10 || y > 140) continue;
            bool header = Lines[i] == Lines[i].ToUpperInvariant() && Lines[i].Length > 0;
            UiKit.Title(r, Font, Lines[i], new Vector2(160, y), Lines[i] is "TRAIL DUEL" or "THE END" ? 2 : 1, header ? Pal.Gold : Pal.White);
        }
    }
}
