using System.Runtime.InteropServices;
using MicroEngine.Localization;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// The player's languages as the system has them, through SDL (<c>SDL_GetPreferredLocales</c>): the display languages
/// of Windows, the preferred languages of macOS and iOS, the locale of Linux and Android, and
/// <c>navigator.languages</c> in a browser. It needs no culture data from .NET, so a packaged game, a phone and a
/// page start in the player's language. Read once, when first asked.
/// </summary>
internal sealed unsafe class SdlLanguages : ISystemLanguages
{
    private IReadOnlyList<string>? _languages;

    /// <inheritdoc/>
    public IReadOnlyList<string> PreferredLanguages => _languages ??= Read();

    private static IReadOnlyList<string> Read()
    {
        int count = 0;
        SDL_Locale** locales = SDL_GetPreferredLocales(&count);
        if (locales is null) return [];
        try
        {
            var languages = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                if (locales[i] is null) continue;
                string? tag = Tag(Marshal.PtrToStringUTF8((nint)locales[i]->language), Marshal.PtrToStringUTF8((nint)locales[i]->country));
                if (tag is not null && !languages.Contains(tag, StringComparer.OrdinalIgnoreCase)) languages.Add(tag);
            }
            return languages;
        }
        finally { SDL_free(locales); }
    }

    /// <summary>
    /// The language tag of what SDL reports: a language (<c>es</c>) and, when the system gives one, a country
    /// (<c>MX</c>), joined as <c>es-MX</c>. Null for what is not a language (the "C" locale, an empty entry).
    /// </summary>
    internal static string? Tag(string? language, string? country)
    {
        if (string.IsNullOrWhiteSpace(language)) return null;
        string tag = LanguageTag.Normalize(string.IsNullOrWhiteSpace(country) ? language : $"{language}-{country}");
        return LanguageTag.IsValid(tag) ? tag : null;
    }
}
