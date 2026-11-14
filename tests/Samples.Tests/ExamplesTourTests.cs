using Examples;

namespace Samples.Tests;

/// <summary>The examples' tour script against the list of examples it walks.</summary>
[TestClass]
public sealed class ExamplesTourTests
{
    /// <summary>
    /// The tour moves down the list one entry at a time and leaves each example with one "press pause", so an example
    /// added to the list without its block makes every later capture show the example before the one its name says.
    /// </summary>
    [TestMethod]
    public void TheTourOpensEveryExampleOfTheList()
    {
        var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "scripts", "examples-tour.script"));
        int visits = lines.Count(line => line.TrimEnd().EndsWith(" press pause", StringComparison.Ordinal));
        Assert.AreEqual(ExamplesGame.All.Length, visits,
            "samples/Examples/scripts/tour.script needs one block (open, capture, press pause) per entry of ExamplesGame.All, in the list's order.");
    }
}
