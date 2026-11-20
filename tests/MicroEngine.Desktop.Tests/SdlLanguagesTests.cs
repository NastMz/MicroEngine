using MicroEngine.Core;
using MicroEngine.Localization;
using MicroEngine.NativeChecks;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>The player's languages as the SDL platform reports them (SDL_GetPreferredLocales), as language tags.</summary>
[TestClass]
public sealed class SdlLanguagesTests
{
    [TestMethod]
    [DataRow("es", "MX", "es-MX")]
    [DataRow("en", null, "en")]
    [DataRow("en", "", "en")]
    [DataRow("zh", "CN", "zh-CN")]
    [DataRow("pt_BR", null, "pt-BR")]
    [DataRow("C", null, null)]
    [DataRow("", "US", null)]
    [DataRow(null, null, null)]
    public void SdlsLanguageAndCountryBecomeATag(string? language, string? country, string? expected) =>
        Assert.AreEqual(expected, SdlLanguages.Tag(language, country));

    [TestMethod]
    [TestCategory("Native")]
    public void TheSystemSaysWhichLanguagesThePlayerPrefers()
    {
        var languages = new SdlLanguages().PreferredLanguages;
        Assert.IsTrue(languages.Count > 0, "this machine has a language set");
        foreach (var tag in languages)
            Assert.IsTrue(LanguageTag.IsValid(tag) && !tag.Contains('_'), $"'{tag}' is a language tag");
        Assert.AreEqual(languages.Count, languages.Distinct(StringComparer.OrdinalIgnoreCase).Count(), "each language once");
    }

    [TestMethod]
    [TestCategory("Native")]
    public void TheSdlPlatformOffersThemToTheGame()
    {
        SdlPlatform? platform = null;
        try { platform = new SdlPlatform(new PlatformRequest(new GameSettings("languages test", 160, 90, 2)) { WaitForDisplay = false, Mute = true, Hidden = true }); }
        catch (InvalidOperationException e) { Native.Require(false, $"SDL cannot open a window here: {e.Message}"); }
        using (platform)
        {
            Assert.IsTrue(platform!.TryGetCapability<ISystemLanguages>(out var system));
            CollectionAssert.AreEqual(new SdlLanguages().PreferredLanguages.ToArray(), system!.PreferredLanguages.ToArray());
        }
    }
}
