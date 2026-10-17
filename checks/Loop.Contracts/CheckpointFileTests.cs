using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;
using TurnExplorer.Model;
using TurnExplorer.Storage;

namespace Loop.Contracts;

[TestClass]
public sealed class CheckpointFileTests
{
    private string _root = null!;
    private static TurnCheckpoint State => new(1, Room.Hall, 616, 328, true, true, MenuOption.Return, 3);
    private string Target => Path.Combine(_root, "checkpoint.bin");

    [TestInitialize]
    public void CreateFixture() => _root = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "MicroEngine-CF1-" + Guid.NewGuid().ToString("N"))).FullName;

    [TestCleanup]
    public void RemoveOwnedFixture()
    {
        var parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Assert.IsTrue(Path.GetFullPath(_root).StartsWith(parent + "MicroEngine-CF1-", StringComparison.OrdinalIgnoreCase));
        foreach (var file in Directory.GetFiles(_root)) File.Delete(file);
        foreach (var directory in Directory.GetDirectories(_root)) Directory.Delete(directory);
        Directory.Delete(_root);
    }

    [TestMethod]
    public void CreatesExactFileAndRestoresIndependentSessionWithoutResidue()
    {
        CheckpointFile.CreateNew(Target, State);
        CollectionAssert.AreEqual(TurnCheckpointCodec.Encode(State), File.ReadAllBytes(Target));
        Assert.AreEqual(State, CheckpointFile.Read(Target));
        var a = TurnSession.Restore(State);
        var b = TurnSession.Restore(CheckpointFile.Read(Target));
        Assert.AreNotSame(a, b);
        a.Update(TimeSpan.FromTicks(1), new([], ["jump"]));
        b.Update(TimeSpan.FromTicks(1), new([], ["jump"]));
        Assert.AreEqual(a.Capture(), b.Capture());
        Assert.AreEqual(1, Directory.GetFiles(_root).Length);
        using var exclusive = new FileStream(Target, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.AreEqual(24L, exclusive.Length);
    }

    [TestMethod]
    public void ExistingValidOrCorruptFilesAreNeverReplaced()
    {
        foreach (var bytes in new[] { TurnCheckpointCodec.Encode(State), new byte[] { 9, 7, 5 } })
        {
            File.WriteAllBytes(Target, bytes);
            var sibling = Path.Combine(_root, "unrelated.bin");
            File.WriteAllBytes(sibling, [1, 2, 3]);
            Assert.ThrowsExactly<IOException>(() => CheckpointFile.CreateNew(Target, State));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Target));
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(sibling));
            Assert.AreEqual(2, Directory.GetFiles(_root).Length);
        }
    }

    [TestMethod]
    public void InvalidArgumentsDoNotWriteAndMissingPathsDoNotBecomeDefaults()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => CheckpointFile.CreateNew(null!, State));
        Assert.ThrowsExactly<ArgumentNullException>(() => CheckpointFile.Read(null!));
        foreach (var path in new[] { "", "   " })
        {
            Assert.ThrowsExactly<ArgumentException>(() => CheckpointFile.CreateNew(path, State));
            Assert.ThrowsExactly<ArgumentException>(() => CheckpointFile.Read(path));
        }
        Assert.ThrowsExactly<ArgumentNullException>(() => CheckpointFile.CreateNew(Target, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CheckpointFile.CreateNew(Target, State with { Version = 3 }));
        Assert.ThrowsExactly<FileNotFoundException>(() => CheckpointFile.Read(Target));
        var missing = Path.Combine(_root, "missing", "checkpoint.bin");
        Assert.ThrowsExactly<DirectoryNotFoundException>(() => CheckpointFile.CreateNew(missing, State));
        Assert.ThrowsExactly<DirectoryNotFoundException>(() => CheckpointFile.Read(missing));
        Assert.AreEqual(0, Directory.GetFileSystemEntries(_root).Length);
    }

    [TestMethod]
    public void FailedPublicationCleansOwnedTempAndKeepsExistingDirectory()
    {
        Directory.CreateDirectory(Target);
        Assert.ThrowsExactly<IOException>(() => CheckpointFile.CreateNew(Target, State));
        Assert.IsTrue(Directory.Exists(Target));
        Assert.AreEqual(0, Directory.GetFiles(_root).Length);
    }

    [TestMethod]
    public void MalformedAndOversizedReadsPreserveFilesAndReleaseHandles()
    {
        var badVersion = TurnCheckpointCodec.Encode(State); badVersion[0] = 3;
        var badFlags = TurnCheckpointCodec.Encode(State); badFlags[16] = 8;
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[23], new byte[25], new byte[1024 * 1024], badVersion, badFlags })
        {
            File.WriteAllBytes(Target, bytes);
            Assert.ThrowsExactly<FormatException>(() => CheckpointFile.Read(Target));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Target));
            using var exclusive = new FileStream(Target, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
    }
}
