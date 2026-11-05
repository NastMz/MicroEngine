using TrailDuel.Rules;
using TrailDuel.Visuals;

namespace TrailDuel.World;

/// <summary>
/// The whole world of Trail Duel: Sprout Town, Route 1, Hollow Wood and Brookhaven, plus interiors.
/// Legend: . grass  , flowers  ; tall grass  = path  T tree  P pine  W water  F fence  v ledge  R rock  b bush
/// Interiors: # wall  _ floor  m exit mat  B bookshelf  d bed  t table  p plant  c counter  x tv  k kitchen
/// a altar  o pedestal  r rug  S shelf  M machine  u statue  C carpet.
/// </summary>
public static class Maps
{
    public const string MentorName = "Mentor Ilse";
    public const string MasterName = "Master Odessa";

    private static readonly Dictionary<string, MapDef> All = Build().ToDictionary(m => m.Id);

    public static MapDef Get(string id) => All.TryGetValue(id, out var m) ? m : throw new KeyNotFoundException($"Unknown map '{id}'.");
    public static IEnumerable<MapDef> Every => All.Values;

    private static IEnumerable<MapDef> Build()
    {
        yield return Home();
        yield return Neighbor();
        yield return Lab();
        yield return SproutTown();
        yield return Route1();
        yield return HollowWood();
        yield return Brookhaven();
        yield return Shrine();
        yield return Shop();
        yield return Hall();
    }

    private static MapDef Home() => new()
    {
        Id = "home", Name = "Tavi's House", Indoor = true, Music = "town", Backdrop = Backdrop.Indoor,
        Rows =
        [
            "##########",
            "##########",
            "_kk_x___d_",
            "________d_",
            "___tt_____",
            "p________p",
            "__________",
            "____mm____",
        ],
        Warps = [new(4, 7, "sprout", 5, 7, Direction.Down), new(5, 7, "sprout", 5, 7, Direction.Down)],
        Npcs =
        [
            new("mom", 2, 4, Direction.Right, CharacterLook.Mom, ["Good morning, Tavi! Did you sleep well?", "Mentor Ilse stopped by. She has a surprise for you at the lab!", "It's the big building north-east of here."],
                NpcRole.Mom, Speaker: "Mom"),
        ],
    };

    private static MapDef Neighbor() => new()
    {
        Id = "house2", Name = "Sprout Town", Indoor = true, Music = "town", Backdrop = Backdrop.Indoor,
        Rows =
        [
            "########",
            "########",
            "B__x__d_",
            "______d_",
            "_tt_____",
            "_tt____p",
            "___m____",
        ],
        Warps = [new(3, 6, "sprout", 6, 15, Direction.Down)],
        Npcs =
        [
            new("elder", 4, 3, Direction.Down, CharacterLook.Elder, ["Oh ho! Off on your own journey, are you?", "When I was young I walked the trail all the way to Brookhaven.", "Take this. It saved my hide more than once."],
                NpcRole.Gift, Speaker: "Grandpa Moss"),
        ],
    };

    private static MapDef Lab() => new()
    {
        Id = "lab", Name = "Ilse's Lab", Indoor = true, Music = "town", Backdrop = Backdrop.Indoor,
        Rows =
        [
            "############",
            "############",
            "BBB_MM_MMBBB",
            "____________",
            "____ooo_____",
            "____________",
            "_p________p_",
            "BB________BB",
            "____________",
            "_____mm_____",
        ],
        Warps = [new(5, 9, "sprout", 17, 7, Direction.Down), new(6, 9, "sprout", 17, 7, Direction.Down)],
        Npcs =
        [
            new("mentor", 8, 3, Direction.Down, CharacterLook.Mentor, [], NpcRole.Mentor, Speaker: MentorName),
            new("rival", 2, 5, Direction.Right, CharacterLook.Rival, [], NpcRole.Rival, HideIf: "rival1", Speaker: GameState.RivalName),
            new("aide", 9, 8, Direction.Left, CharacterLook.Clerk, ["I'm Ilse's aide. Did you know?", "Flame beats Leaf, Leaf beats Tide, and Tide beats Flame.", "Volt is great against Tide and Gale, but Stone just shrugs it off."], Wander: true, Speaker: "Aide"),
        ],
    };

    private static MapDef SproutTown() => new()
    {
        Id = "sprout", Name = "Sprout Town", Music = "town", Backdrop = Backdrop.Meadow,
        Rows =
        [
            "TTTTTTTTTTT==TTTTTTTTTTT",
            "TT,,.......==.......,,TT",
            "TT.........==.........TT",
            "TT.........==.........TT",
            "TT.........==.........TT",
            "TT,........==.........TT",
            "TT.........==.........TT",
            "TT...=.....==....=....TT",
            "TT...==============...TT",
            "TT,,.......==....,,,..TT",
            "TT.........==.........TT",
            "TT.........==.........TT",
            "TT.........==..WWWWW..TT",
            "TT.........==.WWWWWWW.TT",
            "TTb........==.WWWWWWW.TT",
            "TT....=....==..WWWWW..TT",
            "TT,...=======,,.....,,TT",
            "TTTTTTTTTTTTTTTTTTTTTTTT",
        ],
        Buildings =
        [
            new(3, 3, 5, 4, BuildingStyle.House, 2),
            new(14, 2, 7, 5, BuildingStyle.Lab, 3),
            new(4, 12, 5, 3, BuildingStyle.HouseBlue, 2),
        ],
        Warps =
        [
            new(5, 6, "home", 4, 6, Direction.Up),
            new(17, 6, "lab", 5, 8, Direction.Up),
            new(6, 14, "house2", 3, 5, Direction.Up),
            new(11, 0, "route1", 9, 28, Direction.Up),
            new(12, 0, "route1", 10, 28, Direction.Up),
        ],
        Signs =
        [
            new(10, 9, "SPROUT TOWN", "Where every journey takes root."),
            new(13, 7, "MENTOR ILSE'S CREATURE LAB"),
            new(8, 7, "TAVI'S HOUSE"),
        ],
        Triggers = [new(11, 1, "need-starter"), new(12, 1, "need-starter")],
        Npcs =
        [
            new("girl", 8, 10, Direction.Down, CharacterLook.Girl, ["Wild creatures hide in tall grass.", "If you walk through it, one might jump out at you!"], Wander: true),
            new("fisher", 13, 13, Direction.Right, CharacterLook.Fisher, ["The pond is so calm today.", "Someday I'll hook something legendary. Someday!"]),
        ],
    };

    private static MapDef Route1() => new()
    {
        Id = "route1", Name = "Route 1", Music = "route", Backdrop = Backdrop.Meadow,
        Rows =
        [
            "TTTTTTTTT==TTTTTTTTT",
            "TT,,;;...==...,,TTTT",
            "TT;;;;...==....TTTTT",
            "TT;;;;...==.....TTTT",
            "TT,....,.======.,TTT",
            "TT.,.....;;;;.;;;TTT",
            "TT;;;;;;TTTT;;;;;TTT",
            "TT;;;;;;TTTT;;;;;.TT",
            "TT;;;;;;TTTT;;;;;.TT",
            "TT,......====...,.TT",
            "TT.......==.......TT",
            "TTvvvvvvv==vvvvvvvTT",
            "TT..=======.......TT",
            "TT;;;;;TTTTT......TT",
            "TT;;;;;TTTTT..,...TT",
            "TT.,.R.TTTTT......TT",
            "TT..,..TTTTT.R....TT",
            "TT;;;;;TTTTT......TT",
            "TT;;;;;TTTTT..,..TTT",
            "TT;;;;;TTTTT.....TTT",
            "TT...======T.....TTT",
            "TT.......==TvvvvvTTT",
            "TTTTT....==......TTT",
            "TT.......==.;;;;;;TT",
            "TT..;;...==.;;;;;;TT",
            "TT..;;...==.;;;;;;TT",
            "TTT......==...TTTTTT",
            "TT.R.....==.......TT",
            "TT,,.....==.....,,TT",
            "TTTTTTTTT==TTTTTTTTT",
        ],
        Warps =
        [
            new(9, 29, "sprout", 11, 1, Direction.Down),
            new(10, 29, "sprout", 12, 1, Direction.Down),
            new(9, 0, "forest", 11, 20, Direction.Up),
            new(10, 0, "forest", 12, 20, Direction.Up),
        ],
        Signs = [new(8, 27, "ROUTE 1", "North: Hollow Wood. South: Sprout Town.")],
        Items = [new(16, 15, "potion", 1), new(3, 5, "orb", 2)],
        Encounters = new(12, new("breezel", 2, 4, 40), new("puffmop", 2, 4, 35), new("pebblit", 3, 4, 25)),
        Npcs =
        [
            new("mira", 14, 5, Direction.Left, CharacterLook.Lass, [], Speaker: "Picnicker Mira",
                Trainer: new("Picnicker Mira", [("pebblit", 5)], 160, ["Our eyes met! That means we duel!"], "Aww, my Pebblit tried so hard...",
                    ["Pebblit is tough, but Tide and Leaf moves wash right over it."], Sight: 4)),
            new("bram", 3, 20, Direction.Right, CharacterLook.Youngster, [], Speaker: "Scout Bram",
                Trainer: new("Scout Bram", [("puffmop", 4), ("breezel", 5)], 140, ["I've trained all morning! Let me show you!"], "Whoa, you're really good!",
                    ["Weaken wild creatures before throwing an orb.", "Sleeping ones are even easier to catch!"], Sight: 7)),
        ],
    };

    private static MapDef HollowWood() => new()
    {
        Id = "forest", Name = "Hollow Wood", Music = "forest", Backdrop = Backdrop.Forest,
        Rows =
        [
            "PPPPPPPPPPPPPPPPPP==PPPP",
            "PPPPPPPPPPPPPPPP..==PPPP",
            "PPPPPPPPPPPPP.....==.PPP",
            "PPPPPPPPPPP,......==.PPP",
            "PPPPPPPPPP,.......==..PP",
            "PPPPPPPPPPP.;;;;..==..PP",
            "PPPPPPPPPPPP;;;;;.==..PP",
            "PPPPPPPPPPPPP;;;;.==..PP",
            "PPPP,..PPPPPPP;;;.==.;PP",
            "PP......PPPPPPPP;;==..PP",
            "PP;;;;;;.PPPPPPP;;==;;PP",
            "PP;;;;;;.PPPPPPPP.==;;PP",
            "PP;;;;;;.PPPPPPPPP==;PPP",
            "PP.;;;;..PPPPPPPPP==.PPP",
            "PP.......===========.PPP",
            "PPPP...TT..==..TTRR.PPPP",
            "PPPP;;;TT..==..TT;;;PPPP",
            "PPPP;;;;;;.==.;;;;;;PPPP",
            "PPPPP;;;;;;==;;;;;;PPPPP",
            "PPPPPPP;;;;==;;;;PPPPPPP",
            "PPPPPPPPP;;==;;PPPPPPPPP",
            "PPPPPPPPPPP==PPPPPPPPPPP",
        ],
        Warps =
        [
            new(11, 21, "route1", 9, 1, Direction.Down),
            new(12, 21, "route1", 10, 1, Direction.Down),
            new(18, 0, "brook", 12, 18, Direction.Up),
            new(19, 0, "brook", 13, 18, Direction.Up),
        ],
        Triggers = [new(18, 3, "rival2"), new(19, 3, "rival2")],
        Items = [new(3, 9, "superpotion", 1), new(4, 8, "superorb", 1)],
        Signs = [new(10, 15, "HOLLOW WOOD", "Mind the tall grass. Brookhaven lies to the north.")],
        Encounters = new(13, new("mossnail", 5, 7, 30), new("fizzbit", 5, 7, 25), new("breezel", 6, 7, 20), new("pebblit", 6, 8, 15), new("gustowl", 7, 8, 10)),
        Npcs =
        [
            new("doran", 8, 11, Direction.Down, CharacterLook.Hiker, [], Speaker: "Hiker Doran",
                Trainer: new("Hiker Doran", [("pebblit", 7), ("pebblit", 8)], 320, ["Hah! These woods make a hiker strong!", "Ready to rumble?"], "Crumbled like gravel!",
                    ["Stone creatures shrug off Flame...", "...but Tide and Leaf moves crack them wide open."], Sight: 3)),
            new("sola", 21, 9, Direction.Left, CharacterLook.Birder, [], Speaker: "Birdkeeper Sola",
                Trainer: new("Birdkeeper Sola", [("breezel", 7), ("gustowl", 8)], 300, ["The birds of the wood sing for battle!"], "My flock... grounded!",
                    ["Gale types fear Volt and Stone moves."], Sight: 3)),
            new("rival2", 18, 0, Direction.Down, CharacterLook.Rival, [], NpcRole.Rival, ShowIf: "rival2-active", HideIf: "rival2", Speaker: GameState.RivalName),
        ],
    };

    private static MapDef Brookhaven() => new()
    {
        Id = "brook", Name = "Brookhaven", Music = "town2", Backdrop = Backdrop.Meadow,
        Rows =
        [
            "TTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TT,,..................,,TT",
            "TT......................TT",
            "TT......................TT",
            "TT......................TT",
            "TT......................TT",
            "TT..........==..........TT",
            "TT..........==.......,,.TT",
            "TT,.........==..........TT",
            "TT..........==..........TT",
            "TT..........==..........TT",
            "TT..........==..........TT",
            "TT..........==..........TT",
            "TT..........==..........TT",
            "TT..=.......==......=...TT",
            "TT..=================...TT",
            "TT,,........==....WWWW..TT",
            "TT..........==...WWWWWW.TT",
            "TTTTTbb.....==...WWWW.TTTT",
            "TTTTTTTTTTTT==TTTTTTTTTTTT",
        ],
        Buildings =
        [
            new(8, 1, 9, 5, BuildingStyle.Hall, 4),
            new(2, 10, 6, 4, BuildingStyle.Shrine, 2),
            new(18, 10, 5, 4, BuildingStyle.Shop, 2),
            new(2, 2, 4, 4, BuildingStyle.House, 1, Locked: true),
            new(19, 3, 5, 3, BuildingStyle.HouseGreen, 2, Locked: true),
        ],
        Warps =
        [
            new(12, 5, "hall", 6, 14, Direction.Up),
            new(4, 13, "shrine", 5, 6, Direction.Up),
            new(20, 13, "shop", 4, 6, Direction.Up),
            new(12, 19, "forest", 18, 1, Direction.Down),
            new(13, 19, "forest", 19, 1, Direction.Down),
        ],
        Signs =
        [
            new(10, 16, "BROOKHAVEN", "A crossing of brooks and bold trainers."),
            new(14, 6, "TRAIL MASTER HALL", "Master: Odessa. Challengers welcome!"),
        ],
        Npcs =
        [
            new("boy", 9, 10, Direction.Down, CharacterLook.Boy, ["The Shrine heals your creatures for free.", "The Keeper there is really kind!"], Wander: true),
            new("fan", 17, 7, Direction.Left, CharacterLook.Lass, ["Master Odessa uses Gale, Volt and Stone creatures.", "Bring a team with a little variety!"]),
            new("angler", 16, 16, Direction.Right, CharacterLook.Fisher, ["Brookhaven's water comes all the way down from the mountains.", "It's cold enough to wake a sleeping Puffmop!"]),
        ],
    };

    private static MapDef Shrine() => new()
    {
        Id = "shrine", Name = "Healing Shrine", Indoor = true, Music = "shrine", Backdrop = Backdrop.Indoor,
        Rows =
        [
            "###########",
            "###########",
            "p__a___a__p",
            "____ccc____",
            "_____C_____",
            "_____C_____",
            "u____C____u",
            "_____m_____",
        ],
        Warps = [new(5, 7, "brook", 4, 14, Direction.Down)],
        Npcs =
        [
            new("keeper", 5, 2, Direction.Down, CharacterLook.Keeper, [], NpcRole.Keeper, Speaker: "Keeper Rhea"),
            new("traveler", 8, 5, Direction.Left, CharacterLook.Hiker, ["Creatures you catch with a full party rest in the Shrine Box.", "The Keeper looks after them. Lucky critters!"]),
        ],
    };

    private static MapDef Shop() => new()
    {
        Id = "shop", Name = "Trail Shop", Indoor = true, Music = "town2", Backdrop = Backdrop.Indoor,
        Rows =
        [
            "##########",
            "##########",
            "_c__SS_SS_",
            "_c________",
            "cc_____p__",
            "____SS____",
            "__________",
            "____m_____",
        ],
        Warps = [new(4, 7, "brook", 20, 14, Direction.Down)],
        Npcs =
        [
            new("clerk", 0, 3, Direction.Right, CharacterLook.Clerk, [], NpcRole.Clerk, Speaker: "Clerk"),
            new("shopper", 7, 6, Direction.Up, CharacterLook.Girl, ["Super Orbs are pricey...", "...but they're worth it for a rare creature like Gustwing!"], Wander: true),
        ],
    };

    private static MapDef Hall() => new()
    {
        Id = "hall", Name = "Trail Master Hall", Indoor = true, Music = "hall", Backdrop = Backdrop.Hall,
        Rows =
        [
            "#############",
            "#############",
            "u____CCC____u",
            "_____CCC_____",
            "u____CCC____u",
            "_____CCC_____",
            "u____CCC____u",
            "_____CCC_____",
            "u____CCC____u",
            "_____CCC_____",
            "u____CCC____u",
            "_____CCC_____",
            "p____CCC____p",
            "_____CCC_____",
            "_____CCC_____",
            "______m______",
        ],
        Warps = [new(6, 15, "brook", 12, 6, Direction.Down)],
        Npcs =
        [
            new("lio", 4, 9, Direction.Right, CharacterLook.Apprentice, [], Speaker: "Apprentice Lio",
                Trainer: new("Apprentice Lio", [("fizzbit", 10), ("mossnail", 11)], 500, ["To face the Master, you must first get past me!"], "The Master won't go easy on you...",
                    ["Good luck. The Master's ace is a living boulder."], Sight: 3)),
            new("odessa", 6, 3, Direction.Down, CharacterLook.Master, [], Speaker: MasterName,
                Trainer: new(MasterName, [("gustowl", 11), ("voltusk", 12), ("bouldox", 13)], 2000,
                    ["So you are the one making waves along the trail.", "I am Odessa, Trail Master of Brookhaven.", "Show me the bond you share with your creatures!"],
                    "Your bond is true. The trail is yours.", ["You are the Trail Champion now. Walk proudly!"], Sight: 4, IsMaster: true)),
        ],
    };

    /// <summary>The rival's team for each encounter, built around the starter that counters the player's.</summary>
    public static TrainerDef RivalBattle(GameState state, int encounter)
    {
        string starter = GameState.RivalStarterFor(state.StarterId);
        return encounter == 1
            ? new(GameState.RivalName, [(starter, 5)], 300, [], "What?! I picked the strong one, though!", [], Smart: false, IsRival: true)
            : new(GameState.RivalName, [("breezel", 9), (EvolvedOrBase(starter, 11), 11)], 900, [], "Hmph. You've gotten really strong, Tavi.", [], IsRival: true);
    }

    private static string EvolvedOrBase(string species, int level)
    {
        var s = SpeciesDex.Get(species);
        return s.EvolvesInto is { } evo && level >= s.EvolveLevel ? evo : species;
    }
}
