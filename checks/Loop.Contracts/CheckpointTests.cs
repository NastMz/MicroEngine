using System.Buffers.Binary;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;
using TurnExplorer.Model;

namespace Loop.Contracts;

[TestClass]
public sealed class CheckpointTests
{
    private static InputSnapshot Input(params string[] pressed) => new([], pressed);

    [TestMethod]
    public void DurableStatesRoundTripAndResumeIndependently()
    {
        foreach (var room in Enum.GetValues<Room>())
        foreach (var menu in new[] { false, true })
        foreach (var option in Enum.GetValues<MenuOption>())
        foreach (var phase in Enumerable.Range(0, 4))
        {
            var checkpoint = new TurnCheckpoint(1, room, 310, 164, true, menu, option, phase);
            var original = TurnSession.Restore(checkpoint);
            var restored = TurnSession.Restore(TurnCheckpointCodec.Decode(TurnCheckpointCodec.Encode(original.Capture())));
            Assert.AreEqual(checkpoint with { Version = 2 }, restored.Capture());
            Assert.AreNotSame(original, restored);
            Assert.IsFalse(restored.TurnResolved);
            foreach (var input in new[] { Input("down", "jump", "jump"), Input("jump"), new InputSnapshot(["right"], []) })
            {
                original.Update(TimeSpan.FromSeconds(.1), input);
                restored.Update(TimeSpan.FromSeconds(.1), input);
                Assert.AreEqual(original.Capture(), restored.Capture());
                Assert.AreEqual(original.TurnResolved, restored.TurnResolved);
            }
        }
        var fresh = new TurnSession();
        Assert.AreEqual(new TurnCheckpoint(2, Room.Courtyard, 40, 164, false, false, MenuOption.Advance, 0), fresh.Capture());
        fresh.Update(TimeSpan.FromSeconds(1.5), new InputSnapshot(["right"], ["jump"]));
        fresh.Update(TimeSpan.FromTicks(1), Input("jump"));
        Assert.IsTrue(fresh.TurnResolved);
        var clone = TurnSession.Restore(fresh.Capture());
        Assert.IsFalse(clone.TurnResolved);
        Assert.AreEqual(fresh.Capture(), clone.Capture());
    }

    [TestMethod]
    public void EncodingIsExactLittleEndianAndOwnsItsBytes()
    {
        var state = new TurnCheckpoint(1, Room.Hall, 616, 328, true, true, MenuOption.Return, 3);
        byte[] expected = [1,0,0,0, 1,0,0,0, 0,0,26,68, 0,0,164,67, 7,0,0,0, 3,0,0,0];
        var encoded = TurnCheckpointCodec.Encode(state);
        CollectionAssert.AreEqual(expected, encoded);
        Assert.AreEqual(state, TurnCheckpointCodec.Decode(encoded));
        var other = TurnCheckpointCodec.Encode(state);
        encoded[0] = 99;
        CollectionAssert.AreEqual(expected, other);
        Assert.AreEqual(state, TurnCheckpointCodec.Decode(other));
        var zero = state with { X = 0, Y = 0, MarkerActivated = false, MenuOpen = false, SelectedOption = MenuOption.Advance };
        Assert.AreEqual(zero, TurnCheckpointCodec.Decode(TurnCheckpointCodec.Encode(zero)));
    }

    [TestMethod]
    public void InvalidStatesAreRejectedWithoutMutatingExistingSession()
    {
        var session = new TurnSession();
        var state = session.Capture();
        TurnCheckpoint[] invalid = [state with { Version = 0 }, state with { Version = 3 }, state with { Room = (Room)2 },
            state with { X = -1 }, state with { X = 617 }, state with { Y = -1 }, state with { Y = 329 },
            state with { X = float.NaN }, state with { Y = float.PositiveInfinity }, state with { X = float.NegativeInfinity },
            state with { SelectedOption = (MenuOption)(-1) }, state with { TurnPhase = -1 }, state with { TurnPhase = 4 }];
        foreach (var bad in invalid)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TurnSession.Restore(bad));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TurnCheckpointCodec.Encode(bad));
            Assert.AreEqual(state, session.Capture());
        }
        Assert.ThrowsExactly<ArgumentNullException>(() => TurnSession.Restore(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => TurnCheckpointCodec.Encode(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => TurnCheckpointCodec.Decode(null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Explorer.Create((Room)7, 0, 0, false));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Explorer.Create(Room.Hall, float.NaN, 0, false));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Explorer.Create(Room.Hall, 0, 329, false));
        var explorer = Explorer.Create(Room.Hall, 616, 328, true);
        Assert.AreEqual(Room.Hall, explorer.Room); Assert.AreEqual(616f, explorer.X); Assert.AreEqual(328f, explorer.Y);
        Assert.IsTrue(explorer.MarkerActivated); Assert.IsFalse(explorer.InteractionOccurred);
    }

    [TestMethod]
    public void CorruptPayloadsRejectWithoutChangingInputOrSource()
    {
        var session = new TurnSession();
        var state = session.Capture();
        foreach (var length in new[] { 0, 1, 23, 25, 48 })
            Assert.ThrowsExactly<FormatException>(() => TurnCheckpointCodec.Decode(new byte[length]));
        foreach (var (offset, word) in new (int, int)[] { (0, 3), (4, -1), (8, BitConverter.SingleToInt32Bits(float.NaN)),
            (12, BitConverter.SingleToInt32Bits(329)), (16, 16), (16, -1), (20, 4) })
        {
            var bytes = TurnCheckpointCodec.Encode(state);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), word);
            var before = (byte[])bytes.Clone();
            Assert.ThrowsExactly<FormatException>(() => TurnCheckpointCodec.Decode(bytes));
            CollectionAssert.AreEqual(before, bytes);
            Assert.AreEqual(state, session.Capture());
        }
    }
}
