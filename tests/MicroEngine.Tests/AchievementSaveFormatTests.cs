using System.Text;
using MicroEngine.Core;

namespace MicroEngine.Tests;

/// <summary>
/// The achievements slot is a persisted format: the exact text the tracker writes, and the files earlier versions wrote,
/// are pinned here so that how the tracker turns its state into text (with or without reflection) can change without
/// changing a byte of a player's file.
/// </summary>
[TestClass]
public sealed class AchievementSaveFormatTests
{
    private static readonly Achievement[] Catalog =
    [
        new("first", "First Step", "Do the thing once."),
        new("ten", "Ten Times", "Do the thing ten times.", Goal: 10),
        new("año \"<1>\" & más/ñ", "Odd Id", "An id with characters JSON escapes."),
        new("locked", "Locked", "Never reported."),
    ];

    private string _dir = null!;

    [TestInitialize]
    public void Setup() => _dir = Path.Combine(Path.GetTempPath(), "me-ach-format-" + Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private string File_ => Path.Combine(_dir, AchievementTracker.SaveSlot + ".json");

    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    [TestMethod]
    public void The_tracker_writes_exactly_this_file()
    {
        var tracker = new AchievementTracker(Catalog, new SaveStore("game", _dir));
        tracker.AddProgress("ten", 3);
        tracker.Unlock("first");
        tracker.Unlock("año \"<1>\" & más/ñ");

        var expected = Lines(
            "{",
            "  \"Version\": 1,",
            "  \"Entries\": {",
            "    \"ten\": {",
            "      \"Progress\": 3,",
            "      \"Unlocked\": false",
            "    },",
            "    \"first\": {",
            "      \"Progress\": 1,",
            "      \"Unlocked\": true",
            "    },",
            "    \"a\\u00F1o \\u0022\\u003C1\\u003E\\u0022 \\u0026 m\\u00E1s/\\u00F1\": {",
            "      \"Progress\": 1,",
            "      \"Unlocked\": true",
            "    }",
            "  }",
            "}");
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(expected), File.ReadAllBytes(File_), File.ReadAllText(File_));
    }

    [TestMethod]
    public void An_empty_tracker_that_is_flushed_after_a_change_keeps_entries_it_does_not_know()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(File_, "{\"Version\":1,\"Entries\":{\"retired\":{\"Progress\":4,\"Unlocked\":true}}}");
        var tracker = new AchievementTracker(Catalog, new SaveStore("game", _dir));
        tracker.Unlock("first");

        var expected = Lines(
            "{",
            "  \"Version\": 1,",
            "  \"Entries\": {",
            "    \"retired\": {",
            "      \"Progress\": 4,",
            "      \"Unlocked\": true",
            "    },",
            "    \"first\": {",
            "      \"Progress\": 1,",
            "      \"Unlocked\": true",
            "    }",
            "  }",
            "}");
        Assert.AreEqual(expected, File.ReadAllText(File_));
    }

    [TestMethod]
    public void Files_written_by_earlier_versions_load_the_same()
    {
        Directory.CreateDirectory(_dir);
        string[] files =
        [
            // As the tracker writes it, with either newline.
            "{\r\n  \"Version\": 1,\r\n  \"Entries\": {\r\n    \"first\": {\r\n      \"Progress\": 1,\r\n      \"Unlocked\": true\r\n    },\r\n    \"ten\": {\r\n      \"Progress\": 7,\r\n      \"Unlocked\": false\r\n    }\r\n  }\r\n}",
            "{\n  \"Version\": 1,\n  \"Entries\": {\n    \"first\": {\n      \"Progress\": 1,\n      \"Unlocked\": true\n    },\n    \"ten\": {\n      \"Progress\": 7,\n      \"Unlocked\": false\n    }\n  }\n}",
            // Compact, reordered, without a version, with a property and an entry this version does not know, and a null entry.
            "{\"Entries\":{\"ten\":{\"Unlocked\":false,\"Progress\":7,\"When\":\"2026-01-01\"},\"first\":{\"Progress\":1,\"Unlocked\":true},\"gone\":{\"Progress\":2},\"empty\":null},\"Extra\":[1,2]}",
        ];
        foreach (var text in files)
        {
            File.WriteAllText(File_, text);
            var tracker = new AchievementTracker(Catalog, new SaveStore("game", _dir));
            Assert.IsTrue(tracker.IsUnlocked("first"), text);
            Assert.AreEqual(7, tracker.Progress("ten"), text);
            Assert.IsFalse(tracker.IsUnlocked("ten"), text);
            Assert.AreEqual(0, tracker.Progress("locked"), text);
            Assert.AreEqual(text, File.ReadAllText(File_), "loading does not rewrite the file");
            Assert.IsFalse(File.Exists(Path.Combine(_dir, "achievements.corrupt.json")), text);
        }
    }

    [TestMethod]
    public void Files_that_are_not_achievement_data_are_set_aside_as_before()
    {
        Directory.CreateDirectory(_dir);
        // A null entries object is an empty save; the others are not this format and are kept aside.
        (string Text, bool SetAside)[] files =
        [
            ("{\"Version\":1,\"Entries\":null}", false), ("{}", false), ("null", true), ("[]", true), ("{\"Entries\":{\"first\":{\"Progress\":\"1\"}}}", true),
            ("{\"Entries\":{\"first\":7}}", true), ("{\"Version\": 1, \"Entries\"", true), ("", true),
        ];
        foreach (var (text, setAside) in files)
        {
            var aside = Path.Combine(_dir, "achievements.corrupt.json");
            if (File.Exists(aside)) File.Delete(aside);
            File.WriteAllText(File_, text);
            var tracker = new AchievementTracker(Catalog, new SaveStore("game", _dir));
            Assert.AreEqual(0, tracker.UnlockedCount, text);
            Assert.AreEqual(setAside, File.Exists(aside), $"set aside: {text}");
            Assert.AreEqual(!setAside, File.Exists(File_), $"left in place: {text}");
            Assert.IsTrue(tracker.Unlock("first"), text);
            Assert.IsTrue(new AchievementTracker(Catalog, new SaveStore("game", _dir)).IsUnlocked("first"), text);
            File.Delete(File_);
        }
    }
}
