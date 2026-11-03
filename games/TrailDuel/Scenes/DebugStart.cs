using MicroEngine.Kernel;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.World;

namespace TrailDuel.Scenes;

/// <summary>
/// Development shortcuts selected with TRAIL_DEBUG, used for screenshots and quick checks of later content.
/// Never reachable from normal play.
/// </summary>
internal static class DebugStart
{
    public static Scene? Create(TrailDuelGame game, string mode)
    {
        if (mode == "gallery") return new GalleryScene(game);
        if (mode == "cast") return new GalleryScene(game, cast: true);
        if (mode == "zoom") return new GalleryScene(game, zoom: true);
        if (mode == "orbs") return new GalleryScene(game, orbs: true);
        var s = GameState.NewGame();
        game.State = s;
        foreach (var f in new[] { "intro", "met-mentor", "starter", "rival1", "gift:orbs" }) s.Set(f);
        s.StarterId = "emberpup";
        s.Bag.Add("orb", 6);
        s.Bag.Add("potion", 3);
        s.Bag.Add("superpotion", 1);
        s.Money = 2400;
        s.PlaySeconds = 3600 * 1.2;
        void Team(params (string Species, int Level)[] team)
        {
            s.Party.Clear();
            foreach (var (sp, lv) in team) s.AddCreature(Creature.Create(sp, lv));
        }
        void At(string map, int x, int y, Direction f = Direction.Up) { s.MapId = map; (s.X, s.Y, s.Facing) = (x, y, f); }

        switch (mode)
        {
            case "lab":
                s.Flags.Clear();
                s.Set("intro"); s.Set("met-mentor");
                s.StarterId = "";
                s.Party.Clear();
                At("lab", 5, 5);
                return new OverworldScene(game);
            case "town":
                Team(("emberpup", 6));
                At("sprout", 11, 9, Direction.Down);
                return new OverworldScene(game);
            case "route":
                Team(("emberpup", 7), ("breezel", 5));
                At("route1", 9, 24);
                return new OverworldScene(game);
            case "route-mid":
            case "route-top":
                // Captures of the middle (the grass corridor and the ledge gap) and the north end of Route 1.
                Team(("emberpup", 7), ("breezel", 5));
                if (mode == "route-mid") At("route1", 4, 12, Direction.Down);
                else At("route1", 9, 3, Direction.Down);
                return new OverworldScene(game);
            case "defeat":
                // One creature on its last hit point below the route trainer: walking up loses a battle.
                Team(("emberpup", 5));
                s.Party[0].Hp = 1;
                At("route1", 9, 24);
                return new OverworldScene(game);
            case "forest":
                Team(("emberpup", 10), ("breezel", 8), ("pebblit", 7));
                At("forest", 11, 18);
                return new OverworldScene(game);
            case "brook":
            case "hall":
            case "master":
            case "evolve":
            case "credits":
            case "party":
            case "summary":
            case "bag":
            case "shop":
            case "battle":
            case "battle-meadow":
            case "battle-indoor":
            case "fx":
            case "learn":
            case "trainer":
                s.Set("rival2");
                foreach (var t in new[] { "mira", "bram", "doran", "sola" }) s.Set("trainer:" + t);
                Team(("blazehound", 14), ("puddlepip", 12), ("gustowl", 11), ("fizzbit", 10));
                s.Party[1].Hp = 9;
                s.Party[2].Status = StatusCondition.Poison;
                foreach (var c in s.Party) { s.Seen.Add(c.SpeciesId); s.Caught.Add(c.SpeciesId); }
                s.Seen.Add("bouldox");
                if (mode == "hall" || mode == "master") At("hall", 6, mode == "master" ? 5 : 13);
                else At("brook", 12, 16);
                break;
            default:
                return null;
        }

        var world = new OverworldScene(game);
        Scene? overlay = mode switch
        {
            "evolve" => Evolving(game),
            "credits" => new CreditsScene(game, () => { }),
            "party" => new PartyScene(game, PartyScene.Purpose.Field),
            "summary" => new SummaryScene(game, 0),
            "bag" => new BagScene(game, null),
            "shop" => new ShopScene(game, () => { }),
            "battle" => WildBattle(game, s, Backdrop.Forest),
            "battle-meadow" => WildBattle(game, s, Backdrop.Meadow),
            "battle-indoor" => WildBattle(game, s, Backdrop.Indoor),
            "learn" => new BattleScene(game, new Battle(s.Party, [Creature.Create("mossnail", 9)], null, s.Bag, new SeededRandom(7)), new BattleSetup(Backdrop.Meadow, "battle", null), _ => { })
            {
                LearnShowcase = "kindle",
            },
            "fx" => new BattleScene(game, new Battle(s.Party, [Creature.Create("bouldox", 12)], null, s.Bag, new SeededRandom(7)), new BattleSetup(Backdrop.Meadow, "battle", null), _ => { })
            {
                Showcase = ["tackle", "bodyslam", "ember", "blazerush", "bubble", "tidalcrash", "vinelash", "petalstorm", "spark", "thundercrash", "pebbletoss", "boulderdrop", "gust", "cyclone", "kindle", "soothe", "glare"],
            },
            "trainer" => MasterBattle(game, s),
            _ => null,
        };
        if (overlay is not null)
        {
            game.Scenes.Push(world);
            return overlay;
        }
        return world;
    }

    private static Scene Evolving(TrailDuelGame game)
    {
        // TRAIL_EVOLVE picks the species that evolves (default: Sprigfawn).
        var species = SpeciesDex.Get(Environment.GetEnvironmentVariable("TRAIL_EVOLVE") ?? "sprigbun");
        var c = Creature.Create(species.Id, species.EvolveLevel);
        game.State.Party.Insert(0, c);
        return new EvolutionScene(game, c, () => { });
    }

    private static Scene WildBattle(TrailDuelGame game, GameState s, Backdrop backdrop)
    {
        var battle = new Battle(s.Party, [Creature.Create("mossnail", 9)], null, s.Bag, new SeededRandom(7));
        return new BattleScene(game, battle, new BattleSetup(backdrop, "battle", null), _ => { });
    }

    private static Scene MasterBattle(TrailDuelGame game, GameState s)
    {
        var def = Maps.Get("hall").Npcs.First(n => n.Id == "odessa").Trainer!;
        var info = def.ToInfo();
        var battle = new Battle(s.Party, info.Team, info, s.Bag, new SeededRandom(7));
        return new BattleScene(game, battle, new BattleSetup(Backdrop.Hall, "master", def), _ => { });
    }
}
