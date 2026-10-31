using CourtyardKey.Model;

namespace CourtyardKey.Content;

/// <summary>The lands around Willowmere: a 3x3 grid of screens from the village to the ruins.</summary>
public static class Overworld
{
    public const string Id = "overworld";

    public static AreaDef Create() => new()
    {
        Id = Id,
        Name = "Willowmere Vale",
        IsDungeon = false,
        Music = "overworld",
        Rooms = AreaDef.Grid(
            [DeepWoods(), Hilltop(), RuinsGate()],
            [ForestEdge(), NorthRoad(), BridgeCrossing()],
            [WestMeadow(), Village(), Riverside()]),
        Links = [new AreaLink(2, 0, null, Dungeon.EntranceCheckpoint)],
    };

    /// <summary>Where the hero appears when climbing out of the dungeon.</summary>
    public static readonly Checkpoint DungeonExit = new(Id, 2, 0, 14f, 4.9f, Dir.Down);

    private static RoomDef Village() => new()
    {
        Name = "Willowmere",
        Ambience = Ambience.Pollen,
        LeaveNeedsSword = true,
        Map =
        [
            "Ttffffffr==rffffffTt",
            "ttAhhh...==..Ahhh.tt",
            "..hhhh..,==..hhhh..r",
            "..hhhh...==S.hhhh..r",
            "====================",
            "====================",
            ".,b...........,..b.r",
            "r.,..p.....,..p..,.r",
            "TtTtTtTtTtTtTtTtTtTt",
            "tttttttttttttttttttt",
        ],
        Signs = ["WILLOWMERE\n↑ Old Road   → Riverside\n← West Meadow"],
        Npcs =
        [
            new NpcDef("elder", NpcLook.Elder, "Elder Oswin", 7.5f, 3.75f, ElderTalk),
            new NpcDef("merchant", NpcLook.Merchant, "Marla", 17.6f, 3.75f, _ => new Conversation(
                ["Welcome to Marla's! Fresh tonics and hearty pies, all for honest gems."], TalkEffect.OpenShop)),
            new NpcDef("lina", NpcLook.Girl, "Lina", 5.5f, 7.0f, p => new Conversation(p.HasSword
                ? ["Pots and bushes sometimes hide gems. Grandpa says that's how he paid for his first shield!"]
                : ["Grandpa Oswin was looking for you, Wren. He's by the north road!"])),
            new NpcDef("pip", NpcLook.Kid, "Pip", 12.5f, 6.9f, p => new Conversation(p.HasBoomerang
                ? ["Whoa, a boomerang! Can it catch gems from across the water?"]
                : ["The bats in Whisperwood fly all wobbly. Let them come close, then swing!"])),
        ],
    };

    private static Conversation ElderTalk(Progress p)
    {
        if (!p.HasSword)
            return new Conversation(
            [
                "Wren, there you are. The bells beneath the old ruins rang again last night.",
                "Something stirs in the Sunken Courtyard. Its Warden has woken, and the river runs dark.",
                "Take this. It was my sword, long ago. It still remembers the way.",
            ], TalkEffect.GiveSword);
        if (p.HasCourtyardKey)
            return new Conversation(["You did it, child. The bells are quiet at last."]);
        if (!p.WasVisited(Dungeon.Id, 1, 3))
            return new Conversation(["Follow the old road north, then east across the river bridge. The ruins wait beyond."]);
        if (!p.HasBoomerang)
            return new Conversation(["Legend tells of a wind-bound boomerang hidden below. It strikes what no blade can reach."]);
        return new Conversation(["The Warden is stone and moss. Stun it, then strike its glowing heart!"]);
    }

    private static RoomDef WestMeadow() => new()
    {
        Name = "West Meadow",
        Ambience = Ambience.Pollen,
        Map =
        [
            "TtTtTtTtr==rTtTtTtTt",
            "ttttttttr==rtttttttt",
            "Tt.,..b.b==.;;;..,..",
            "tt;;..g..==..;;g....",
            "r,.......===========",
            "r..b..,..===========",
            "Tt..;;;.b.,.g..C.,..",
            "tt.,;;;..b.....b...r",
            "TtTtTtTtTtTtTtTtTtTt",
            "tttttttttttttttttttt",
        ],
        Chests = [new ChestDef(Item.Gems, 30)],
    };

    private static RoomDef Riverside() => new()
    {
        Name = "Riverside",
        Ambience = Ambience.Spray,
        Map =
        [
            "Tt..r.~~~TtTtTtTtTtr",
            "tt..,.~~~ttttttttttr",
            "Tt....~~~TtTtTtTtTtr",
            "tt.b..~~~ttttttttttr",
            "......~~~~~~~~~~~~~r",
            "....,.~~~~~~~~~~~~~~",
            "Tt.b..;~~~~~~~~~~~~~",
            "tt..g.;;~~~~~~~~~~~~",
            "Tt.C...;;~~~~~~~~~~~",
            "tt....rr~~~~~~~~~~~~",
        ],
        Chests = [new ChestDef(Item.Gems, 50)],
        Npcs =
        [
            new NpcDef("bram", NpcLook.Fisher, "Old Bram", 4.6f, 5.6f, p => new Conversation(p.WasVisited(Dungeon.Id, 1, 3)
                ? ["Knights below hide behind their shields. Get around their side, or knock 'em silly first."]
                : ["The river's run dark since the bells. The bridge is north of here, if you're bound for the ruins."]), Dir.Right),
        ],
    };

    private static RoomDef NorthRoad() => new()
    {
        Name = "Old Road",
        Ambience = Ambience.Pollen,
        Map =
        [
            "TtTtTtTtr==rTtTtTtTt",
            "ttttttttr==rtttttttt",
            "r.,..b...==...;;..,.",
            "r.;;.....==S.g.;;..r",
            "====================",
            "====================",
            "..,.g....==....b..,.",
            ".r;;..b..==..,..;;r.",
            "TtTtTtTtr==rTtTtTtTt",
            "ttttttttr==rtttttttt",
        ],
        Signs = ["↑ Lookout Hill   ← Whisperwood\n→ River Bridge and the Old Ruins"],
    };

    private static RoomDef ForestEdge() => new()
    {
        Name = "Whisperwood",
        Ambience = Ambience.Leaves,
        Darkness = 0.12f,
        Map =
        [
            "TtTtTtTtr..rTtTtTtTt",
            "ttttttttr..rtttttttt",
            "TtTt;;...v...;;.TtTt",
            "tttt;;.Tt..b;;..tttt",
            "......,tt...........",
            "Tt...;;...v....,....",
            "tt.b.;;..,..TtTt.;;.",
            "Tt..b.....,.tttt..;;",
            "ttTtTtTtr..rTtTtTtTt",
            "ttttttttr..rtttttttt",
        ],
    };

    private static RoomDef DeepWoods() => new()
    {
        Name = "Whisperwood Hollow",
        Ambience = Ambience.Fireflies,
        Darkness = 0.38f,
        Map =
        [
            "TtTtTtTtTtTtTtTtTtTt",
            "tttttttttttttttttttt",
            "TtTt;;.,....,.;;TtTt",
            "tttt;..v..C...v.tttt",
            "Tt;;...,.....,..;;Tt",
            "tt;..g....,.....g;tt",
            "TtTt;;....b.....;;Tt",
            "tttt;..,.......;..tt",
            "TtTtTtTtr..rTtTtTtTt",
            "ttttttttr..rtttttttt",
        ],
        Chests = [new ChestDef(Item.HeartContainer)],
    };

    private static RoomDef Hilltop() => new()
    {
        Name = "Lookout Hill",
        Ambience = Ambience.Pollen,
        Map =
        [
            "^^^^^^^^^^^^^^^^^^^^",
            "^^^^^^^^^^^^^^^^^^^^",
            "r..,..p.p...p.p..,.r",
            "r.;;.....S.....;;..r",
            "Tt..b...,..,...b..Tt",
            "tt.......g........tt",
            "r.;;,........,.;;..r",
            "TtTt..b......b..TtTt",
            "ttttTtTtr..rTtTttttt",
            "ttttttttr..rtttttttt",
        ],
        Signs = ["Here the Courtyard sank, and its Warden kept the Key.\nLet none wake the sleeping stone."],
        Npcs =
        [
            new NpcDef("ilse", NpcLook.Hermit, "Sage Ilse", 12.5f, 5.0f, p => new Conversation(p.HasBoomerang
                ? ["The wind obeys you now. When the Warden reels, strike its glowing heart!"]
                : ["The Warden is stone and moss. Steel glances off its hide...",
                   "...unless it is dazed. Lure it into the walls, child, then strike its glowing heart."])),
        ],
    };

    private static RoomDef BridgeCrossing() => new()
    {
        Name = "River Bridge",
        Ambience = Ambience.Spray,
        Map =
        [
            "TtTtrr~~~TtTtr==rTtr",
            "tttt..~~~tttt.==.ttr",
            "..b.,.~~~..,.x==x...",
            "r.;;..~~~.I..x==xz.r",
            "======HHH===========",
            "======HHH===========",
            "..,;..~~~..x.I..g..r",
            "..;;..~~~.,...b...Tt",
            "Tt..r.~~~TtTtTtTtrtt",
            "tt..r.~~~ttttttttrrr",
        ],
    };

    private static RoomDef RuinsGate() => new()
    {
        Name = "Old Ruins",
        Base = Tile.RuinFloor,
        Ambience = Ambience.Spray,
        Map =
        [
            "^^^^^^~~~^^^^^^^^^^^",
            "^^^^^^~~~^^^^^^^^^^^",
            "r.;;..~~~xIxxxxxxIxr",
            "r..,..~~~xxxxEExxxxr",
            "Tt.b..~~~xIxxxxxxIxr",
            "tt....~~~xxxxxxxxxxr",
            "r.,...~~~.xxg.z.xx.r",
            "r;;...~~~..,..xx...r",
            "TtTt..~~~TtTtr==rTtr",
            "ttttrr~~~ttttr==rttr",
        ],
    };
}
