using System.Text;
using Examples;
using MicroEngine.Graphics;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The four rendering examples (samples/Examples: light behind walls, masks, water, post-processing) played in an SDL
/// window through the launcher, with a script that opens each from the list and captures it, on both graphics devices:
/// on <c>sdl-gpu</c> every technique shows; on the default device the two made of render targets and blend states show
/// and the two that need shaders draw their plain picture. Checked by what the captures hold.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class RenderingExamplesTests
{
    private static readonly string[] Names = ["Light behind walls", "Masks", "Water", "Post-processing"];
    private static readonly Color FlatWater = Color.Hex("3b5dc9");
    private static readonly Color[] Stripes = [Color.Hex("ef7d57"), Color.Hex("ffcd75"), Color.Hex("a7f070")];

    /// <summary>Plays the four examples on a graphics device and returns their captures (canvas pixels, 3 capture pixels each).</summary>
    private static Dictionary<string, PixelImage> Play(string graphics)
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        var folder = Directory.CreateTempSubdirectory("rendering-examples-").FullName;
        // The list keeps its focus between examples: move down to the first, then one entry at a time.
        var script = new StringBuilder();
        int update = 30, focus = 0;
        foreach (var name in Names)
        {
            int index = Array.FindIndex(ExamplesGame.All, e => e.Name.StartsWith(name, StringComparison.Ordinal));
            Assert.IsTrue(index >= focus, $"'{name}' is in the list of examples, after the one before it");
            for (; focus < index; focus++, update += 6) script.AppendLine($"{update} press down");
            script.AppendLine($"{update + 10} press confirm");
            script.AppendLine($"{update + 80} capture {File(name)}");
            script.AppendLine($"{update + 86} press pause");
            update += 110;
        }
        script.AppendLine($"{update} quit");
        System.IO.File.WriteAllText(Path.Combine(folder, "examples.script"), script.ToString());

        var errors = new StringWriter();
        var (previousError, previousOut) = (Console.Error, Console.Out);
        Console.SetError(errors);
        Console.SetOut(new StringWriter());
        int code;
        try
        {
            code = DesktopLauncher.Run(new ExamplesGame(folder), ["--platform", "sdl", "--graphics", graphics, "--hidden", "--mute",
                "--script", Path.Combine(folder, "examples.script"), "--captures", folder]);
        }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOut);
        }
        if (code != 0 && errors.ToString().Contains("cannot start here")) Native.Require(false, errors.ToString().Trim());
        Assert.AreEqual(0, code, errors.ToString());
        var captures = Names.ToDictionary(n => n, n => PixelImage.FromPng(System.IO.File.ReadAllBytes(Path.Combine(folder, File(n)))));
        Directory.Delete(folder, recursive: true);
        return captures;
    }

    /// <summary>The capture file of an example: its name in the list without spaces.</summary>
    private static string File(string name) => name.Replace(" ", "-").ToLowerInvariant() + ".png";

    private static Color At(PixelImage capture, int x, int y) => capture[x * 3 + 1, y * 3 + 1];
    private static int Brightness(Color c) => c.R + c.G + c.B;

    private static void AssertLightAndMask(Dictionary<string, PixelImage> captures)
    {
        // The torch is at (120, 92) and reaches 110 pixels: the floor beside it is lit, the floor 60 pixels away behind
        // the wall at x = 150 is not (both points are on tiles of the same colour), nor is the far corner.
        var light = captures["Light behind walls"];
        int lit = Brightness(At(light, 100, 95)), shadowed = Brightness(At(light, 180, 95)), corner = Brightness(At(light, 10, 170));
        Assert.IsTrue(lit > shadowed * 2, $"the floor in the light ({lit}) is brighter than the floor behind the wall ({shadowed})");
        Assert.IsTrue(lit > corner * 2, $"the floor in the light ({lit}) is brighter than the far corner ({corner})");
        // The circle of the mask is at (230, 120): the stripes show inside it, the scene's background everywhere else.
        var mask = captures["Masks"];
        CollectionAssert.Contains(Stripes, At(mask, 230, 120), "the picture shows through the mask's circle");
        CollectionAssert.DoesNotContain(Stripes, At(mask, 20, 150), "nothing of the picture outside the mask");
        CollectionAssert.DoesNotContain(Stripes, At(mask, 300, 40));
    }

    [TestMethod]
    [TestCategory("Gpu")]
    public void On_the_programmable_device_every_technique_shows()
    {
        var captures = Play("sdl-gpu");
        AssertLightAndMask(captures);
        // Water: the row under the surface mirrors the hills, the sky and the posts instead of being one flat colour.
        var water = captures["Water"];
        var row = Enumerable.Range(0, 320).Select(x => At(water, x, 126)).ToList();
        Assert.IsTrue(row.Distinct().Count() >= 3, $"the water mirrors what is above it ({row.Distinct().Count()} colours in a row)");
        CollectionAssert.DoesNotContain(row, FlatWater, "the shader's water replaced the flat water");
        // Post-processing: the same row of the gradient is darker at the edge than in the middle (the vignette).
        var post = captures["Post-processing"];
        Assert.IsTrue(Brightness(At(post, 160, 21)) > Brightness(At(post, 3, 21)) + 12, "the corners are darkened");
    }

    [TestMethod]
    public void On_the_default_device_the_render_target_techniques_show_and_the_shader_ones_draw_their_plain_picture()
    {
        var captures = Play("sdl-renderer");
        AssertLightAndMask(captures);
        Assert.AreEqual(FlatWater, At(captures["Water"], 20, 170), "flat water where the device has no shaders");
        var post = captures["Post-processing"];
        Assert.AreEqual(At(post, 160, 21), At(post, 3, 21), "the picture as it was drawn");
    }
}
