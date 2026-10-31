using static NeonRift.Content.SpawnKind;

namespace NeonRift.Content;

/// <summary>
/// The three stages. Each one escalates the enemy mix, ends in a locked 30-column arena with a boss,
/// and offers two or three weapon carriers. Columns are 16 px; ground heights count from the bottom.
/// </summary>
public static class Stages
{
    public static readonly IReadOnlyList<StageDef> All = [Rooftops(), Grotto(), Lab()];
    public static int Count => All.Count;

    /// <summary>Two ledges on the left of every boss arena give room to dodge and to aim at high targets.</summary>
    private static void ArenaLedges(StageBuilder b)
    {
        int arena = b.Cursor - StageBuilder.ArenaColumns;
        b.PlatformAt(arena + 3, 7, 5).PlatformAt(arena + 11, 10, 4);
    }

    /// <summary>Stage 1: neon rooftops. Teaches running, jumping gaps, aiming up and the first weapons.</summary>
    private static StageDef Rooftops()
    {
        var b = new StageBuilder();
        b.Floor(28, 3)
            .PlatformAt(13, 7, 6).At(15, 7, Gunner)
            .At(9, 3, Carrier, param: "S")
            .At(22, 3, TrooperGate, 3);
        b.Pit(3).Floor(20, 5)
            .BlockAt(39, 5, 2, 2).At(39, 7, Turret)
            .At(44, 5, Drones, 3)
            .At(48, 5, Trooper);
        b.Pit(4).Floor(16, 3)
            .At(56, 3, Checkpoint)
            .PlatformAt(58, 7, 5).At(60, 7, Gunner).PlatformAt(65, 10, 4)
            .At(70, 3, Egg);
        b.Floor(12, 4).At(78, 4, TrooperGate, 4);
        b.Pit(3).Floor(26, 3)
            .At(88, 3, Skiffs, 4)
            .BlockAt(95, 3, 2, 3).At(95, 6, Gunner)
            .PlatformAt(100, 7, 6).At(98, 3, Carrier, param: "L")
            .At(106, 3, Drones, 4);
        b.Pit(2).Floor(30, 5)
            .At(116, 5, Checkpoint)
            .At(124, 5, Egg)
            .PlatformAt(127, 9, 6).At(130, 9, Gunner)
            .At(134, 5, TrooperGate, 4)
            .BlockAt(139, 5, 2, 2).At(139, 7, Turret);
        b.Pit(3).Floor(14, 3)
            .At(150, 3, Skiffs, 5)
            .At(153, 3, Carrier, param: "R");
        b.Floor(40, 3).At(163, 3, Checkpoint);
        ArenaLedges(b);
        return b.Build("NEON ROOFTOPS", "Halcyon district, 02:40", "city", "city", BossKind.Gunship);
    }

    /// <summary>Stage 2: the drowned grotto under the city. Lava pools, egg pods and denser flyers.</summary>
    private static StageDef Grotto()
    {
        var b = new StageBuilder();
        b.Floor(24, 3).Decor(4, 3, "prop_palm").Decor(12, 3, "prop_plant-big")
            .At(10, 3, Carrier, param: "S")
            .At(18, 3, TrooperGate, 3)
            .PlatformAt(15, 7, 4);
        b.Lava(5, 2).Floor(18, 4)
            .PlatformAt(24, 6, 5)
            .At(33, 4, Egg).Decor(37, 4, "prop_plant")
            .At(40, 4, Drones, 4);
        b.Lava(4, 2).Floor(6, 3).Lava(4, 2).Floor(20, 4)
            .PlatformAt(47, 6, 3).PlatformAt(57, 6, 3)
            .At(62, 4, Checkpoint).Decor(64, 4, "prop_palm")
            .BlockAt(68, 4, 3, 3).At(69, 7, Turret)
            .At(74, 4, TrooperGate, 5);
        b.Floor(10, 3).At(82, 3, Skiffs, 5).At(86, 3, Carrier, param: "R");
        b.Lava(5, 2).PlatformAt(92, 5, 2).Floor(23, 4)
            .PlatformAt(100, 8, 6).At(103, 8, Gunner)
            .At(98, 4, Egg).At(108, 4, Egg)
            .At(112, 4, Drones, 4).Decor(115, 4, "prop_plant-big");
        b.Lava(3, 2).Floor(24, 5)
            .At(122, 5, Checkpoint)
            .BlockAt(128, 5, 2, 3).At(128, 8, Gunner)
            .At(133, 5, TrooperGate, 5)
            .PlatformAt(136, 9, 5).At(138, 9, Turret)
            .At(142, 5, Carrier, param: "L");
        b.Lava(4, 2).Floor(12, 3).At(152, 3, Skiffs, 6).At(156, 3, Egg);
        b.Floor(42, 3).At(166, 3, Checkpoint).Decor(168, 3, "prop_palm");
        ArenaLedges(b);
        return b.Build("DROWNED GROTTO", "Under the Halcyon foundations", "grotto", "grotto", BossKind.Interceptor);
    }

    /// <summary>Stage 3: the Halcyon lab. Platform climbs, crossfire from turrets and the Leviathan.</summary>
    private static StageDef Lab()
    {
        var b = new StageBuilder();
        b.Floor(26, 3).Decor(6, 3, "prop_tank-1").Decor(9, 3, "prop_tank-2")
            .At(12, 3, Carrier, param: "S")
            .PlatformAt(16, 7, 6).At(18, 7, Turret)
            .At(22, 3, TrooperGate, 4);
        b.Pit(3).Floor(8, 4).Pit(3).Floor(20, 5)
            .PlatformAt(29, 8, 3).PlatformAt(40, 8, 3)
            .At(45, 5, Egg).At(52, 5, Drones, 4)
            .Decor(48, 5, "prop_tank-3");
        b.Floor(14, 3).At(62, 3, Checkpoint)
            .PlatformAt(64, 7, 4).PlatformAt(69, 10, 4).At(70, 10, Gunner)
            .At(72, 3, Skiffs, 6);
        b.Pit(4).PlatformAt(78, 4, 3).Floor(24, 4)
            .At(84, 4, TrooperGate, 5)
            .BlockAt(90, 4, 2, 4).At(90, 8, Turret)
            .At(96, 4, Egg).At(102, 4, Carrier, param: "R")
            .Decor(99, 4, "prop_tank-1");
        b.Pit(3).Floor(26, 5)
            .At(112, 5, Checkpoint)
            .PlatformAt(115, 9, 5).At(117, 9, Gunner).PlatformAt(123, 12, 4).At(124, 12, Turret)
            .At(120, 5, Egg).At(128, 5, Drones, 5)
            .At(132, 5, TrooperGate, 6);
        b.Pit(3).Floor(14, 3).At(144, 3, Skiffs, 6).At(148, 3, Carrier, param: "L").Decor(152, 3, "prop_tank-2");
        b.Floor(44, 3).At(160, 3, Checkpoint).Decor(162, 3, "prop_support");
        ArenaLedges(b);
        return b.Build("HALCYON LAB", "Leviathan assembly floor", "lab", "lab", BossKind.Dreadnought);
    }
}
