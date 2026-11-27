using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests.Interface;

/// <summary>
/// The cursor of a list a scene draws itself (<see cref="MenuCursor"/>): moved by the direction actions, wrapping or
/// not, in one column or a grid, resting on options that cannot be chosen, choosing with confirm, with the window of a
/// list longer than its box. The scripts below replay the same presses through the cursor and through the rules of the
/// demo games' own helpers (<c>games/Shared</c>), update by update.
/// </summary>
[TestClass]
public sealed class MenuCursorTests
{
    /// <summary>A scene that gives every update's presses to <paramref name="update"/>, as a scene reading its own menu does.</summary>
    private sealed class CursorScene(Action<Scene> update) : Scene
    {
        public override void Update(float dt) => update(this);
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class CursorGame(Scene scene) : Game
    {
        public override GameSettings Settings { get; } = new("cursor", 320, 180, 1);
        protected override void Load() => Scenes.Push(scene);
    }

    private static readonly string[] Actions = ["up", "down", "left", "right", "confirm", "cancel"];

    /// <summary>
    /// Runs <paramref name="updates"/> updates with presses from a fixed sequence (the same on every machine): each
    /// action is pressed on about one update in five, then let go, so two can be pressed together.
    /// </summary>
    private static void Play(int updates, uint seed, Action<Scene> update)
    {
        var scene = new CursorScene(update);
        var game = new CursorGame(scene);
        var platform = HeadlessPlatform.For(game.Settings);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        uint state = seed;
        var held = new HashSet<string>();
        for (int i = 0; i < updates; i++)
        {
            foreach (var action in Actions)
            {
                state = state * 1664525u + 1013904223u;
                bool press = !held.Contains(action) && (state >> 24) % 5 == 0;
                if (press) held.Add(action); else held.Remove(action);
                game.Input.SetForced(action, press);
            }
            game.StepForTest();
        }
    }

    [TestMethod]
    [DataRow(true, DisplayName = "wrapping, as the demos' menus")]
    [DataRow(false, DisplayName = "not wrapping")]
    public void It_selects_and_chooses_what_the_demos_option_list_does_on_every_update(bool wrap)
    {
        // The rules of games/Shared/OptionList.cs: five options, the second cannot be chosen.
        const int count = 5;
        int index = 0, moves = 0, cursorMoves = 0, chosen = 0, cursorChosen = 0, update = 0;
        var cursor = new MenuCursor(count) { Wrap = wrap };
        cursor.SetEnabled(1, false);
        cursor.Moved += () => cursorMoves++;
        Play(600, 7, scene =>
        {
            update++;
            var input = scene.Input;
            // The helper's rules first, on the same presses, without consuming.
            int dir = (input.Pressed("down") ? 1 : 0) - (input.Pressed("up") ? 1 : 0);
            if (dir != 0)
            {
                int next = index + dir;
                next = wrap ? (next + count) % count : Math.Clamp(next, 0, count - 1);
                if (next != index) { index = next; moves++; }
            }
            int? expected = input.Pressed("confirm") && index != 1 ? index : null;
            if (expected is not null) chosen++;

            int? got = cursor.Update(input);
            Assert.AreEqual(index, cursor.Index, $"the highlighted option on update {update}");
            Assert.AreEqual(expected, got, $"the chosen option on update {update}");
            Assert.AreEqual(moves, cursorMoves, $"cursor sounds by update {update}");
            if (got is not null)
            {
                cursorChosen++;
                Assert.IsFalse(input.Pressed("confirm"), "the press that chooses is consumed");
            }
            else if (expected is null && index == 1 && input.Down("confirm"))
                Assert.IsFalse(input.IsConsumed("confirm"), "a confirm on an option that cannot be chosen is left for the scene (a refusal sound)");
        });
        Assert.AreEqual(chosen, cursorChosen);
        Assert.IsTrue(moves > 50 && chosen > 20, $"the script moved {moves} times and chose {chosen}");
    }

    [TestMethod]
    public void A_list_longer_than_its_box_scrolls_as_the_demos_achievement_list_does()
    {
        // The rules of games/Shared/AchievementList.cs: the cursor stops at both ends, and the first row shown moves
        // just enough to keep the cursor in a window of four rows.
        const int count = 11, visible = 4;
        int selected = 0, scroll = 0, update = 0;
        var cursor = new MenuCursor(count) { Wrap = false };
        var tops = new HashSet<int>();
        Play(900, 21, scene =>
        {
            update++;
            var input = scene.Input;
            int dir = (input.Pressed("down") ? 1 : 0) - (input.Pressed("up") ? 1 : 0);
            selected = Math.Clamp(selected + dir, 0, count - 1);
            scroll = Math.Clamp(scroll, Math.Max(0, selected - visible + 1), selected);

            cursor.Move(input);
            Assert.AreEqual(selected, cursor.Index, $"the highlighted row on update {update}");
            Assert.AreEqual(scroll, cursor.Top(visible), $"the first row shown on update {update}");
            tops.Add(scroll);
        });
        Assert.IsTrue(tops.Count > 4, "the list scrolled through most of its positions");
    }

    [TestMethod]
    public void A_grid_moves_by_column_and_row_and_stops_at_cells_that_do_not_exist()
    {
        // A two-by-two battle menu with three moves: the fourth cell is empty (the rule of Trail Duel's GridMove).
        var cursor = new MenuCursor(3, columns: 2) { Wrap = false };
        Assert.IsTrue(cursor.Move(UiAction.Right));
        Assert.AreEqual(1, cursor.Index);
        Assert.IsFalse(cursor.Move(UiAction.Right), "the end of the row");
        Assert.IsFalse(cursor.Move(UiAction.Down), "no cell under the second column");
        Assert.IsTrue(cursor.Move(UiAction.Left));
        Assert.IsTrue(cursor.Move(UiAction.Down));
        Assert.AreEqual(2, cursor.Index);
        Assert.IsFalse(cursor.Move(UiAction.Right), "the last row has one cell");
        Assert.IsFalse(cursor.Move(UiAction.Down));
        Assert.IsTrue(cursor.Move(UiAction.Up));
        Assert.AreEqual(0, cursor.Index);

        var wrapping = new MenuCursor(6, columns: 3);
        wrapping.Index = 2;
        Assert.IsTrue(wrapping.Move(UiAction.Right));
        Assert.AreEqual(0, wrapping.Index, "past the end of a row: its first cell");
        Assert.IsTrue(wrapping.Move(UiAction.Up));
        Assert.AreEqual(3, wrapping.Index, "past the top of a column: its last cell");
        Assert.IsTrue(wrapping.Move(UiAction.Left));
        Assert.AreEqual(5, wrapping.Index);
    }

    [TestMethod]
    public void A_grid_follows_the_presses_Trail_Duels_battle_menu_follows()
    {
        const int count = 4;
        int index = 0, update = 0, sounds = 0, cursorSounds = 0;
        var cursor = new MenuCursor(count, columns: 2) { Wrap = false };
        cursor.Moved += () => cursorSounds++;
        Play(600, 99, scene =>
        {
            update++;
            var input = scene.Input;
            bool left = input.Pressed("left"), right = input.Pressed("right"), up = input.Pressed("up"), down = input.Pressed("down");
            // Opposite presses on one update cancel out in the cursor; the demo lets left win. Not compared.
            if ((left && right) || (up && down)) { cursor.Index = index; return; }
            int col = index % 2, row = index / 2;
            if (left && col == 1) col = 0;
            else if (right && col == 0 && row * 2 + 1 < count) col = 1;
            else if (up && row == 1) row = 0;
            else if (down && row == 0 && 2 + col < count) row = 1;
            int next = row * 2 + col;
            if (next != index) { index = next; sounds++; }

            cursor.Move(input);
            Assert.AreEqual(index, cursor.Index, $"the highlighted cell on update {update}");
            Assert.AreEqual(sounds, cursorSounds);
        });
        Assert.IsTrue(sounds > 50);
    }

    [TestMethod]
    public void The_pointer_of_a_game_moves_and_chooses_through_the_same_cursor()
    {
        var cursor = new MenuCursor(3);
        int moved = 0;
        cursor.Moved += () => moved++;
        Assert.IsTrue(cursor.MoveTo(2), "the row the pointer is over");
        Assert.IsFalse(cursor.MoveTo(2), "already there: no second sound");
        Assert.IsFalse(cursor.MoveTo(7), "not a row");
        Assert.AreEqual(1, moved);
        cursor.Index = 0;
        Assert.AreEqual(1, moved, "setting the index is silent");
        Assert.AreEqual(0, cursor.Index);
    }

    [TestMethod]
    public void Its_options_can_change_and_the_cursor_stays_inside()
    {
        var cursor = new MenuCursor(5) { Index = 4 };
        cursor.Count = 3;                               // an item was used up
        Assert.AreEqual(2, cursor.Index);
        cursor.Count = 0;                               // an empty bag
        Assert.AreEqual(0, cursor.Index);
        Assert.IsFalse(cursor.Move(UiAction.Down));
        Assert.AreEqual(0, cursor.Top(4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MenuCursor(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MenuCursor(3, columns: 0));
    }

    [TestMethod]
    public void Its_state_is_the_highlighted_option_the_window_and_what_can_be_chosen()
    {
        static ulong Hash(MenuCursor c)
        {
            var hash = new StateHasher();
            hash.Add(c);
            return hash.Value;
        }
        var a = new MenuCursor(6);
        var b = new MenuCursor(6);
        Assert.AreEqual(Hash(a), Hash(b));
        a.Move(UiAction.Down);
        Assert.AreNotEqual(Hash(a), Hash(b));
        b.Move(UiAction.Down);
        Assert.AreEqual(Hash(a), Hash(b));
        b.SetEnabled(3, false);
        Assert.AreNotEqual(Hash(a), Hash(b), "what confirm does depends on it");
    }
}
