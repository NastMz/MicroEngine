namespace MicroEngine.Localization;

/// <summary>
/// An optional feature of a platform: the languages the player set on the system, most wanted first, as tags
/// (<c>es-MX</c>, <c>en</c>). The game's first language is the first of them it has text in. A platform that cannot
/// tell offers no such feature, and the runtime's own culture is used where the build carries culture data.
/// </summary>
public interface ISystemLanguages
{
    /// <summary>The player's languages, most wanted first; empty when the system does not say.</summary>
    IReadOnlyList<string> PreferredLanguages { get; }
}
