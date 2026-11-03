using System.Numerics;
using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>The per-step state hash: a stable binary encoding in named sections, the engine's seeded generator and traces.</summary>
[TestClass]
public sealed class StateHashTests
{
    private static ulong Hash(Action<StateHasher> add)
    {
        var hash = new StateHasher();
        add(hash);
        return hash.Value;
    }

    [TestMethod]
    public void TheSameValuesInTheSameOrderGiveTheSameHashAndAnyChangeGivesAnother()
    {
        static void Values(StateHasher h)
        {
            h.Section("a");
            h.Add(1); h.Add(2L); h.Add(true); h.Add(1.5f); h.Add(2.5); h.Add(new Vector2(3, 4)); h.Add("text"); h.Add((byte)7);
            h.Add([1, 2, 3]);
        }
        Assert.AreEqual(Hash(Values), Hash(Values));
        Assert.AreNotEqual(Hash(Values), Hash(h => { Values(h); h.Add(0); }));
        Assert.AreNotEqual(Hash(h => { h.Add(1); h.Add(2); }), Hash(h => { h.Add(2); h.Add(1); }), "order counts");
        // The encoding is fixed: this value is the same on every machine, run and version (FNV-1a 64 of a known stream).
        Assert.AreEqual(0xcbf29ce484222325UL, Hash(_ => { }));
        Assert.AreEqual(Hash(h => h.Add(1)), Hash(h => h.Add(1)));
    }

    [TestMethod]
    public void FloatsCountByTheirBitsSoNegativeZeroAndNaNPayloadsDiffer()
    {
        Assert.AreNotEqual(Hash(h => h.Add(0f)), Hash(h => h.Add(-0f)));
        Assert.AreNotEqual(Hash(h => h.Add(0.0)), Hash(h => h.Add(-0.0)));
        Assert.AreNotEqual(Hash(h => h.Add(BitConverter.Int32BitsToSingle(0x7fc00000))), Hash(h => h.Add(BitConverter.Int32BitsToSingle(0x7fc00001))));
        Assert.AreNotEqual(Hash(h => h.Add(1f)), Hash(h => h.Add(1.0000001f)));
    }

    [TestMethod]
    public void StringsAreLengthPrefixedSoNullEmptyAndSplitsDiffer()
    {
        Assert.AreNotEqual(Hash(h => h.Add((string?)null)), Hash(h => h.Add("")));
        Assert.AreNotEqual(Hash(h => { h.Add("ab"); h.Add("c"); }), Hash(h => { h.Add("a"); h.Add("bc"); }));
    }

    [TestMethod]
    public void SectionsHashTheirOwnValuesAndNameTheStateThatChanged()
    {
        var a = new StateHasher();
        a.Section("time"); a.Add(10);
        a.Section("player"); a.Add(new Vector2(1, 2));
        var b = new StateHasher();
        b.Section("time"); b.Add(10);
        b.Section("player"); b.Add(new Vector2(1, 3));

        Assert.AreEqual(2, a.Sections.Count);
        CollectionAssert.AreEqual(new[] { "time", "player" }, a.Sections.Select(s => s.Name).ToArray());
        Assert.AreEqual(a.Sections[0], b.Sections[0], "the unchanged section keeps its hash");
        Assert.AreNotEqual(a.Sections[1].Hash, b.Sections[1].Hash);
        Assert.AreNotEqual(a.Value, b.Value);
        // Section names take part in the whole hash: the same values under another name are another state.
        Assert.AreNotEqual(Hash(h => { h.Section("x"); h.Add(1); }), Hash(h => { h.Section("y"); h.Add(1); }));
    }

    [TestMethod]
    public void ValuesBeforeTheFirstSectionFormAnUnnamedSectionAndResetStartsOver()
    {
        var hash = new StateHasher();
        hash.Add(5);
        hash.Section("after");
        CollectionAssert.AreEqual(new[] { "", "after" }, hash.Sections.Select(s => s.Name).ToArray());
        ulong first = hash.Value;
        hash.Reset();
        Assert.AreEqual(0, hash.Sections.Count);
        hash.Add(5);
        hash.Section("after");
        Assert.AreEqual(first, hash.Value);
    }

    [TestMethod]
    public void SectionNamesCannotHoldTabsOrLineBreaks()
    {
        var hash = new StateHasher();
        Assert.ThrowsExactly<ArgumentException>(() => hash.Section("a\tb"));
        Assert.ThrowsExactly<ArgumentException>(() => hash.Section("a\nb"));
        Assert.ThrowsExactly<ArgumentNullException>(() => hash.Section(null!));
    }

    private sealed class Point(int x) : IStateHashed
    {
        public void HashState(StateHasher hash) => hash.Add(x);
    }

    [TestMethod]
    public void ObjectsContributeThemselvesAndAMissingOneCountsAsAbsent()
    {
        Assert.AreEqual(Hash(h => h.Add(new Point(3))), Hash(h => h.Add(new Point(3))));
        Assert.AreNotEqual(Hash(h => h.Add(new Point(3))), Hash(h => h.Add(new Point(4))));
        Assert.AreNotEqual(Hash(h => h.Add((IStateHashed?)null)), Hash(h => h.Add(new Point(0))));
    }

    [TestMethod]
    public void SeededRandomDrawsExactlyWhatRandomWithTheSameSeedDraws()
    {
        var plain = new Random(42);
        var seeded = new SeededRandom(42);
        for (int i = 0; i < 50; i++)
        {
            Assert.AreEqual(plain.Next(), seeded.Next());
            Assert.AreEqual(plain.Next(17), seeded.Next(17));
            Assert.AreEqual(plain.Next(-5, 90), seeded.Next(-5, 90));
            Assert.AreEqual(plain.NextDouble(), seeded.NextDouble());
            Assert.AreEqual(plain.NextSingle(), seeded.NextSingle());
            Assert.AreEqual(plain.NextInt64(), seeded.NextInt64());
            Assert.AreEqual(plain.NextInt64(1000), seeded.NextInt64(1000));
            Assert.AreEqual(plain.NextInt64(-10, 10), seeded.NextInt64(-10, 10));
        }
        byte[] a = new byte[9], b = new byte[9];
        plain.NextBytes(a); seeded.NextBytes(b);
        CollectionAssert.AreEqual(a, b);
        plain.NextBytes(a.AsSpan()); seeded.NextBytes(b.AsSpan());
        CollectionAssert.AreEqual(a, b);
        int[] x = [1, 2, 3, 4, 5, 6, 7, 8], y = [1, 2, 3, 4, 5, 6, 7, 8];
        plain.Shuffle(x); seeded.Shuffle(y);
        CollectionAssert.AreEqual(x, y);
        CollectionAssert.AreEqual(plain.GetItems(x, 6), seeded.GetItems(y, 6));
        Assert.AreEqual(plain.Next(), seeded.Next(), "still in step after the helpers");
        Assert.AreEqual(42, seeded.Seed);
    }

    [TestMethod]
    public void SeededRandomStateFollowsItsDraws()
    {
        var a = new SeededRandom(7);
        var b = new SeededRandom(7);
        Assert.AreEqual(Hash(h => h.Add(a)), Hash(h => h.Add(b)));
        a.Next(10); b.Next(10);
        Assert.AreEqual(1L, a.Draws);
        Assert.AreEqual(Hash(h => h.Add(a)), Hash(h => h.Add(b)));
        a.NextSingle();
        Assert.AreNotEqual(Hash(h => h.Add(a)), Hash(h => h.Add(b)), "one more draw is another state");
        b.NextSingle();
        Assert.AreEqual(Hash(h => h.Add(a)), Hash(h => h.Add(b)));
        Assert.AreNotEqual(Hash(h => h.Add(new SeededRandom(7))), Hash(h => h.Add(new SeededRandom(8))), "the seed counts");
    }

    private static StepHash Step(long update, ulong hash, params (string Name, ulong Hash)[] sections) =>
        new(update, hash, [.. sections.Select(s => new SectionHash(s.Name, s.Hash))]);

    [TestMethod]
    public void ATraceRecordsStepsInOrderAndIdenticalTracesHaveNoDivergence()
    {
        var a = new StateTrace();
        var b = new StateTrace();
        foreach (var t in new[] { a, b })
        {
            t.Record(Step(1, 10));
            t.Record(Step(2, 20));
        }
        Assert.AreEqual(2, a.Steps.Count);
        Assert.AreEqual(2L, a.Steps[1].Update);
        Assert.IsNull(StateTrace.FirstDivergence(a, b));
        Assert.IsFalse(a.Sections, "per-section hashes are recorded only on request");
    }

    [TestMethod]
    public void FirstDivergenceNamesTheFirstDifferingStepAndSection()
    {
        var a = new StateTrace { Sections = true };
        var b = new StateTrace { Sections = true };
        a.Record(Step(1, 1, ("time", 1), ("scene[0]:Play", 5)));
        b.Record(Step(1, 1, ("time", 1), ("scene[0]:Play", 5)));
        a.Record(Step(2, 2, ("time", 2), ("scene[0]:Play", 6), ("game", 9)));
        b.Record(Step(2, 3, ("time", 2), ("scene[0]:Play", 7), ("game", 8)));
        a.Record(Step(3, 4));
        b.Record(Step(3, 5));

        var divergence = StateTrace.FirstDivergence(a, b);
        Assert.IsNotNull(divergence);
        Assert.AreEqual(2L, divergence.Update);
        Assert.AreEqual("scene[0]:Play", divergence.Section);
        Assert.AreEqual(2UL, divergence.First!.Hash);
        Assert.AreEqual(3UL, divergence.Second!.Hash);
        StringAssert.Contains(divergence.ToString(), "update 2");
        StringAssert.Contains(divergence.ToString(), "scene[0]:Play");
    }

    [TestMethod]
    public void FirstDivergenceReportsAMissingSectionAStepWithoutSectionsAndAShorterTrace()
    {
        var missing = StateTrace.FirstDivergence(
            [Step(1, 1, ("time", 1), ("scene[0]:A", 2))],
            [Step(1, 2, ("time", 1))]);
        Assert.AreEqual("scene[0]:A", missing!.Section);

        var unnamed = StateTrace.FirstDivergence([Step(1, 1)], [Step(1, 2)]);
        Assert.AreEqual(1L, unnamed!.Update);
        Assert.IsNull(unnamed.Section, "without section hashes only the step is known");

        var shorter = StateTrace.FirstDivergence([Step(1, 1), Step(2, 2)], [Step(1, 1)]);
        Assert.AreEqual(2L, shorter!.Update);
        Assert.IsNotNull(shorter.First);
        Assert.IsNull(shorter.Second);

        var renumbered = StateTrace.FirstDivergence([Step(1, 1)], [Step(2, 1)]);
        Assert.AreEqual(1L, renumbered!.Update, "steps are compared by position; a different update number diverges");
    }

    [TestMethod]
    public void ATraceRoundTripsThroughItsTextForm()
    {
        var trace = new StateTrace { Sections = true };
        trace.Record(Step(1, 0xdeadbeefUL, ("time", 3), ("scene[0]:Title Screen", 0xffffffffffffffffUL)));
        trace.Record(Step(2, 7));
        string text = trace.ToText();
        StringAssert.StartsWith(text, "1\t00000000deadbeef\ttime=0000000000000003\tscene[0]:Title Screen=ffffffffffffffff");

        var parsed = StateTrace.Parse(text);
        Assert.IsTrue(parsed.Sections);
        Assert.AreEqual(2, parsed.Steps.Count);
        Assert.IsNull(StateTrace.FirstDivergence(trace, parsed));
        Assert.AreEqual("scene[0]:Title Screen", parsed.Steps[0].Sections[1].Name);
        Assert.ThrowsExactly<FormatException>(() => StateTrace.Parse("x\t1"));
    }
}
