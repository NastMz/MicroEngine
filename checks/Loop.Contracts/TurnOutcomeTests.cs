using System.Buffers.Binary;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;
using TurnExplorer;
using TurnExplorer.Model;
using TurnExplorer.Storage;

namespace Loop.Contracts;

[TestClass]
public sealed class TurnOutcomeTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);
    private static InputSnapshot Edge(params string[] pressed) => new([], pressed);
    private static TurnSession Open()
    {
        var game = TurnSession.Restore(new(1, Room.Courtyard, 310, 164, false, false, MenuOption.Advance, 0));
        game.Update(Tick, Edge("jump", "jump")); return game;
    }
    private static TurnSession Complete()
    {
        var game = Open(); for (int i = 0; i < 4; i++) game.Update(Tick, Edge("jump", "jump")); return game;
    }

    [TestMethod]
    public void FourFreshConfirmationsCompleteOnceAndExplorationContinuesWithoutReopening()
    {
        var game = Open(); Assert.IsFalse(game.EncounterCompleted); Assert.IsFalse(game.TurnResolved);
        for (int i = 1; i <= 4; i++)
        {
            game.Update(Tick, new(["jump"], [])); Assert.AreEqual(i - 1, game.TurnPhase); Assert.IsFalse(game.TurnResolved);
            game.Update(Tick, Edge("jump", "jump", "down")); Assert.AreEqual(i % 4, game.TurnPhase); Assert.IsTrue(game.TurnResolved);
            Assert.AreEqual(i == 4, game.EncounterCompleted); Assert.AreEqual(i != 4, game.MenuOpen);
        }
        var marker = game.MarkerActivated;
        for (int i = 0; i < 3; i++) { game.Update(Tick, Edge("jump", "jump")); Assert.IsFalse(game.MenuOpen); Assert.IsFalse(game.TurnResolved); Assert.AreEqual(marker, game.MarkerActivated); }
        game.Update(TimeSpan.FromSeconds(1), new(["right"], ["jump"])); Assert.AreEqual(490f, game.X); Assert.IsTrue(game.EncounterCompleted);
        game.Update(TimeSpan.FromSeconds(1), new(["right"], [])); Assert.AreEqual(Room.Hall, game.Room);
    }

    [TestMethod]
    public void ReturnPreservesPendingProgressAndInvalidFinalPulseIsAtomic()
    {
        var game = Open(); game.Update(Tick, Edge("jump")); game.Update(Tick, Edge("down", "jump"));
        Assert.AreEqual(1, game.TurnPhase); Assert.IsFalse(game.MenuOpen); Assert.IsFalse(game.EncounterCompleted);
        game.Update(Tick, Edge("jump")); Assert.IsTrue(game.MenuOpen); Assert.AreEqual(1, game.TurnPhase); Assert.AreEqual(MenuOption.Advance, game.SelectedOption);
        for (int i = 0; i < 3; i++) game.Update(Tick, Edge("jump"));
        var before = game.Capture(); Assert.IsTrue(game.TurnResolved); Assert.IsTrue(game.EncounterCompleted);
        Assert.ThrowsExactly<ArgumentNullException>(() => game.Update(TimeSpan.Zero, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => game.Update(TimeSpan.Zero, Edge("jump")));
        Assert.AreEqual(before, game.Capture()); Assert.IsTrue(game.TurnResolved);
        var clone = TurnSession.Restore(before); Assert.IsFalse(clone.TurnResolved);
        foreach (var input in new[] { Edge("jump"), new InputSnapshot(["right"], []), Edge("down", "jump") })
        {
            game.Update(TimeSpan.FromSeconds(.1), input); clone.Update(TimeSpan.FromSeconds(.1), input); Assert.AreEqual(game.Capture(), clone.Capture());
        }
    }

    [TestMethod]
    public void V1BytesRemainExactAndCaptureOnlyNormalizesReturnedVersion()
    {
        var legacy = new TurnCheckpoint(1, Room.Hall, 616, 328, true, true, MenuOption.Return, 3);
        byte[] expected = [1,0,0,0, 1,0,0,0, 0,0,26,68, 0,0,164,67, 7,0,0,0, 3,0,0,0];
        CollectionAssert.AreEqual(expected, TurnCheckpointCodec.Encode(legacy)); Assert.AreEqual(legacy, TurnCheckpointCodec.Decode(expected));
        var session = TurnSession.Restore(legacy); Assert.IsFalse(session.EncounterCompleted);
        Assert.AreEqual(legacy with { Version = 2 }, session.Capture()); Assert.AreEqual(1, legacy.Version);
        var completed = new TurnCheckpoint(2, Room.Courtyard, 310, 164, true, false, MenuOption.Advance, 0, true);
        byte[] v2 = [2,0,0,0, 0,0,0,0, 0,0,155,67, 0,0,36,67, 9,0,0,0, 0,0,0,0];
        CollectionAssert.AreEqual(v2, TurnCheckpointCodec.Encode(completed)); Assert.AreEqual(completed, TurnCheckpointCodec.Decode(v2));
        Assert.AreEqual(completed, TurnSession.Restore(completed).Capture()); Assert.AreEqual(2, new TurnSession().Capture().Version);
    }

    [TestMethod]
    public void PerVersionMasksAndCompletedCombinationsRejectWithoutMutatingInput()
    {
        var good = Complete().Capture();
        foreach (var bad in new[] { good with { Version = 1, EncounterCompleted = true }, good with { MenuOpen = true }, good with { TurnPhase = 1 } })
        {
            Assert.ThrowsExactly<ArgumentException>(() => TurnSession.Restore(bad)); Assert.ThrowsExactly<ArgumentException>(() => TurnCheckpointCodec.Encode(bad));
        }
        foreach (var (version, flags, phase) in new[] { (1, 8, 0), (2, 16, 0), (2, 10, 0), (2, 8, 1), (3, 0, 0) })
        {
            var bytes = TurnCheckpointCodec.Encode(new(1, Room.Courtyard, 40, 164, false, false, MenuOption.Advance, 0));
            BinaryPrimitives.WriteInt32LittleEndian(bytes, version); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), flags); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(20), phase);
            var copy = bytes.ToArray(); Assert.ThrowsExactly<FormatException>(() => TurnCheckpointCodec.Decode(bytes)); CollectionAssert.AreEqual(copy, bytes);
        }
    }

    private class Canvas : ICanvas
    {
        public readonly List<(Rect, Rgba)> Fills = [];
        public void Clear(Rgba color) => Assert.Fail("Outcome must not clear the world.");
        public void Fill(Rect rect, Rgba color) => Fills.Add((rect, color));
    }
    private sealed class Labels : Canvas, ILabelCanvas
    {
        public readonly List<(string, int, int, int, Rgba)> Calls = [];
        public void Label(string text, int x, int y, int size, Rgba color) => Calls.Add((text, x, y, size, color));
    }

    [TestMethod]
    public void CompletedPresentationUsesFourGreenPipsAndOptionalPureLabel()
    {
        var game = Complete(); var before = game.Capture(); var canvas = new Canvas(); MenuRenderer.Draw(canvas, game);
        Assert.AreEqual(4, canvas.Fills.Count);
        for (int i = 0; i < 4; i++) Assert.AreEqual((new Rect(20 + 20 * i, 20, 12, 12), new Rgba(80, 200, 120)), canvas.Fills[i]);
        var labels = new Labels(); MenuRenderer.Draw(labels, game);
        CollectionAssert.AreEqual(canvas.Fills, labels.Fills);
        CollectionAssert.AreEqual(new[] { ("Completed", 20, 42, 20, new Rgba(80, 200, 120)) }, labels.Calls);
        Assert.AreEqual(before, game.Capture()); Assert.IsTrue(game.TurnResolved);
    }

    [TestMethod]
    public void CompletedStorageRoundTripPreservesOldFilesAndResetsOnlyTransientPulse()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "MicroEngine-TURN2-" + Guid.NewGuid().ToString("N"))).FullName;
        var path = Path.Combine(root, "completed.bin");
        try
        {
            var game = Complete(); Assert.IsTrue(game.EncounterCompleted); CheckpointFile.CreateNew(path, game.Capture());
            var bytes = File.ReadAllBytes(path); Assert.AreEqual(24, bytes.Length); Assert.AreEqual(2, BinaryPrimitives.ReadInt32LittleEndian(bytes));
            var restored = TurnSession.Restore(CheckpointFile.Read(path)); Assert.IsTrue(restored.EncounterCompleted); Assert.IsFalse(restored.TurnResolved); Assert.AreEqual(game.Capture(), restored.Capture());
            Assert.ThrowsExactly<IOException>(() => CheckpointFile.CreateNew(path, new TurnSession().Capture())); CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); Directory.Delete(root); }
    }
}
