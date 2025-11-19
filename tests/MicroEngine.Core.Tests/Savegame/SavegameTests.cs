using MicroEngine.Core.Savegame;
using Xunit;

namespace MicroEngine.Core.Tests.Savegame;

public sealed class SavegameTests
{
    private readonly string _testDirectory;
    private readonly SavegameManager _savegameManager;

    public SavegameTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MicroEngine_SavegameTests", Guid.NewGuid().ToString());
        _savegameManager = new SavegameManager(_testDirectory);
    }

    private sealed class TestGameData
    {
        public string? PlayerName { get; set; }
        public int Level { get; set; }
        public float[] Position { get; set; } = Array.Empty<float>();
    }

    [Fact]
    public void SavegameManager_Save_CreatesFile()
    {
        var data = new TestGameData { PlayerName = "TestPlayer", Level = 5 };

        var result = _savegameManager.Save(data, "test_save");

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.True(_savegameManager.Exists("test_save"));

        Cleanup();
    }

    [Fact]
    public void SavegameManager_Load_ReturnsCorrectData()
    {
        var data = new TestGameData 
        { 
            PlayerName = "Hero", 
            Level = 10, 
            Position = new[] { 100.5f, 200.3f } 
        };

        _savegameManager.Save(data, "load_test");
        var result = _savegameManager.Load<TestGameData>("load_test");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Hero", result.Data.PlayerName);
        Assert.Equal(10, result.Data.Level);
        Assert.Equal(2, result.Data.Position.Length);
        Assert.Equal(100.5f, result.Data.Position[0], 2);
        Assert.Equal(200.3f, result.Data.Position[1], 2);

        Cleanup();
    }

    [Fact]
    public void SavegameManager_Load_NonExistentFile_ReturnsError()
    {
        var result = _savegameManager.Load<TestGameData>("nonexistent");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage);

        Cleanup();
    }

    [Fact]
    public void SavegameManager_Delete_RemovesFile()
    {
        var data = new TestGameData { PlayerName = "ToDelete", Level = 1 };
        _savegameManager.Save(data, "delete_test");

        Assert.True(_savegameManager.Exists("delete_test"));

        var deleted = _savegameManager.Delete("delete_test");

        Assert.True(deleted);
        Assert.False(_savegameManager.Exists("delete_test"));

        Cleanup();
    }

    [Fact]
    public void SavegameManager_ListSaves_ReturnsAllSaves()
    {
        _savegameManager.Save(new TestGameData { PlayerName = "Save1" }, "save1");
        _savegameManager.Save(new TestGameData { PlayerName = "Save2" }, "save2");
        _savegameManager.Save(new TestGameData { PlayerName = "Save3" }, "save3");

        var saves = _savegameManager.ListSaves();

        Assert.Equal(3, saves.Length);
        Assert.Contains("save1", saves);
        Assert.Contains("save2", saves);
        Assert.Contains("save3", saves);

        Cleanup();
    }

    [Fact]
    public void SavegameManager_GetMetadata_ReturnsCorrectMetadata()
    {
        var data = new TestGameData { PlayerName = "MetaTest" };
        _savegameManager.Save(data, "meta_test", "My Custom Save");

        var metadata = _savegameManager.GetMetadata("meta_test");

        Assert.NotNull(metadata);
        Assert.Equal("My Custom Save", metadata.SaveName);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.True(metadata.CreatedAt <= DateTime.UtcNow);

        Cleanup();
    }

    [Fact]
    public void SavegameManager_Overwrite_UpdatesLastModified()
    {
        var data1 = new TestGameData { PlayerName = "Original", Level = 1 };
        _savegameManager.Save(data1, "overwrite_test");

        var firstMetadata = _savegameManager.GetMetadata("overwrite_test");
        Thread.Sleep(100); // Ensure time difference

        var data2 = new TestGameData { PlayerName = "Updated", Level = 2 };
        _savegameManager.Save(data2, "overwrite_test");

        var secondMetadata = _savegameManager.GetMetadata("overwrite_test");

        Assert.NotNull(firstMetadata);
        Assert.NotNull(secondMetadata);
        
        // CreatedAt should be within 1 second (allow for precision differences)
        var timeDiff = (secondMetadata.CreatedAt - firstMetadata.CreatedAt).TotalSeconds;
        Assert.True(System.Math.Abs(timeDiff) < 1.0, $"CreatedAt timestamps differ by {timeDiff} seconds");
        
        Assert.True(secondMetadata.LastModified > firstMetadata.LastModified);

        var loaded = _savegameManager.Load<TestGameData>("overwrite_test");
        Assert.Equal("Updated", loaded.Data?.PlayerName);
        Assert.Equal(2, loaded.Data?.Level);

        Cleanup();
    }

    [Fact]
    public void SavegameManager_GetSavePath_AddsExtension()
    {
        var path = _savegameManager.GetSavePath("test");

        Assert.EndsWith(".sav", path);
        Assert.Contains("test.sav", path);
    }

    [Fact]
    public void SavegameManager_GetSavePath_DoesNotDuplicateExtension()
    {
        var path = _savegameManager.GetSavePath("test.sav");

        Assert.EndsWith(".sav", path);
        Assert.DoesNotContain(".sav.sav", path);
    }

    [Fact]
    public void SavegameManager_ComplexData_SerializesCorrectly()
    {
        var data = new Dictionary<string, List<int>>
        {
            { "scores", new List<int> { 100, 200, 300 } },
            { "levels", new List<int> { 1, 2, 3, 4, 5 } }
        };

        _savegameManager.Save(data, "complex_test");
        var result = _savegameManager.Load<Dictionary<string, List<int>>>("complex_test");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(3, result.Data["scores"].Count);
        Assert.Equal(200, result.Data["scores"][1]);

        Cleanup();
    }

    private void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
