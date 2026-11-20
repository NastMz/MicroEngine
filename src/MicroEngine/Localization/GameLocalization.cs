using System.Globalization;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Localization;

/// <summary>
/// A game's texts and language (<see cref="Game.Localization"/>): a <see cref="Localizer"/> that loads its string
/// tables from the game's content, starts in the system's language when the game has it, names the variant of an
/// asset for the language in use, and writes what is missing to the log.
/// </summary>
/// <example>
/// <code>
/// protected override void Load()
/// {
///     Localization.Load("text");                          // Content/text/en.strings.json, es.strings.json...
///     Localization.TrySetLanguage(settings.Language);     // the player's saved choice, when there is one
///     Scenes.Push(new TitleScene());
/// }
///
/// // In a scene:
/// var play = new Button(new LocalizedText("menu.play"), Play);            // follows the language by itself
/// string gems = Game.Localization.Format("hud.gems", ("count", 3));       // "3 gems", "3 gemas"
/// var title = Assets.Texture(Game.Localization.Path("ui/title.png"));     // ui/title.es.png in Spanish
/// </code>
/// </example>
public sealed class GameLocalization : Localizer
{
    private readonly Func<IContentSource> _source;
    private readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal);
    private IReadOnlyList<string>? _system;
    private Func<IReadOnlyList<string>>? _detect;

    /// <summary>The texts of a game whose content is <paramref name="source"/> (read when a table is loaded or a variant looked up).</summary>
    public GameLocalization(Func<IContentSource> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>The texts of <paramref name="game"/>: tables from its content, the system's languages from its platform, reports to its log.</summary>
    internal GameLocalization(Game game) : this(() => game.Assets.Source)
    {
        _detect = () => Detect(game);
        Report = message => game.EngineOrNull?.Log.Write(LogLevel.Warning, "localization", message);
    }

    /// <summary>
    /// The player's languages on this system, most wanted first: what the platform says
    /// (<see cref="ISystemLanguages"/>; the SDL platform does on every system), else the runtime's interface culture
    /// where the build carries culture data. Empty in a deterministic run (scripted runs, tests, captures), so those
    /// never depend on the machine, unless the game's own composition declares an <see cref="ISystemLanguages"/>.
    /// A host or a test may set it. The first of them the game has text in is the language in use until one is chosen.
    /// </summary>
    public IReadOnlyList<string> SystemLanguages
    {
        get => _system ??= _detect?.Invoke() ?? [];
        set
        {
            _system = value ?? throw new ArgumentNullException(nameof(value));
            if (HasTables) DefaultLanguage = Match(_system);
        }
    }

    private static IReadOnlyList<string> Detect(Game game)
    {
        if (game.EngineOrNull is not { } engine) return [];
        // What the game's own composition declares is always honoured (a test, a store's launcher).
        if (engine.TryGetCapability(out ISystemLanguages? declared)) return declared.PreferredLanguages;
        // A deterministic run (scripts, tests, captures) never depends on the machine it runs on.
        if (engine.Time.Mode == TimeMode.Deterministic) return [];
        if (game.TryGetCapability(out ISystemLanguages? system)) return system.PreferredLanguages;
        // Builds with invariant globalization (browsers, phones, packaged games) have no culture: the name is empty.
        string name = CultureInfo.CurrentUICulture.Name;
        return name.Length == 0 ? [] : [name];
    }

    /// <summary>
    /// Loads string tables from the game's content: every table of a folder (<c>Load("text")</c> reads
    /// <c>text/en.strings.json</c>, <c>text/es.strings.json</c>, <c>text/extra.strings.csv</c>...), or one file. A
    /// JSON table is one language, named by its file (<c>pt-BR.strings.json</c>); a CSV table has a column per
    /// language. Tables add up: loading more text for a language (another folder, a level's texts) keeps what it
    /// had, and a key given again takes the newer text. The first load makes the system's language the language in
    /// use when the game has it and has not chosen one.
    /// </summary>
    /// <exception cref="AssetLoadException">The folder or file does not exist, the folder holds no table, or a table cannot be read (the message names the file and the line).</exception>
    public GameLocalization Load(string path)
    {
        string normalized = AssetPath.Normalize(path).TrimEnd('/');
        var source = _source();
        var files = new List<string>();
        if (StringTables.IsTable(normalized)) files.Add(normalized);
        else if (source.Exists(normalized))
            throw new AssetLoadException(normalized, "strings", $"the file is not a string table: a table is named <language>{StringTables.JsonExtension} (en{StringTables.JsonExtension}) or <name>{StringTables.CsvExtension}.");
        else
        {
            IReadOnlyList<string> found;
            try { found = source.Files(normalized); }
            catch (DirectoryNotFoundException e) { throw new AssetLoadException(normalized, "strings", $"no such folder or file in the content ({source.Location}).", e); }
            files.AddRange(found.Where(StringTables.IsTable).Order(StringComparer.Ordinal));
            if (files.Count == 0)
                throw new AssetLoadException(normalized, "strings", $"the folder holds no string table: a table is named <language>{StringTables.JsonExtension} (en{StringTables.JsonExtension}) or <name>{StringTables.CsvExtension}.");
        }
        foreach (var file in files)
        {
            string text;
            try { text = System.Text.Encoding.UTF8.GetString(source.ReadAllBytes(file)); }
            catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException) { throw new AssetLoadException(file, "strings", e.Message, e); }
            try
            {
                if (StringTables.LanguageOf(file) is { } language) Add(StringTables.ParseJson(language, text));
                else foreach (var table in StringTables.ParseCsv(text)) Add(table);
            }
            catch (FormatException e) { throw new AssetLoadException(file, "strings", e.Message, e); }
        }
        DefaultLanguage = Match(SystemLanguages);
        return this;
    }

    /// <summary>
    /// The file to load for <paramref name="path"/> in the language in use: its variant when the content has one,
    /// else the path itself. A variant carries the language tag before the file's extensions:
    /// <c>ui/title.png</c> has <c>ui/title.es.png</c> for Spanish and <c>ui/title.pt-BR.png</c> for Brazilian
    /// Portuguese; a region without a variant of its own takes its language's. Pass the result to any loader
    /// (<c>Assets.Texture(Localization.Path("ui/title.png"))</c>, a sound, a font, a kind of the game's own). The
    /// answer is remembered per language, so asking while drawing allocates nothing.
    /// </summary>
    public string Path(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (_paths.TryGetValue(path, out var known)) return known;
        string normalized = AssetPath.Normalize(path);
        string result = normalized;
        var source = _source();
        for (string? tag = Language; tag is not null; tag = LanguageTag.Parent(tag))
        {
            string candidate = Variant(normalized, tag);
            if (!source.Exists(candidate)) continue;
            result = candidate;
            break;
        }
        return _paths[path] = result;
    }

    /// <summary>
    /// <paramref name="path"/> and every variant of it the content has for the game's languages, the file itself
    /// first: what to preload so changing the language loads nothing during play.
    /// </summary>
    public IReadOnlyList<string> Variants(string path)
    {
        string normalized = AssetPath.Normalize(path);
        var source = _source();
        var all = new List<string> { normalized };
        foreach (var language in Languages)
            if (Variant(normalized, language.Code) is var candidate && source.Exists(candidate)) all.Add(candidate);
        return all;
    }

    /// <summary>The name of the variant of <paramref name="path"/> for <paramref name="language"/>: the tag goes before the file's extensions (<c>glow.frag.shader</c> is <c>glow.es.frag.shader</c>).</summary>
    public static string Variant(string path, string language)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        int name = path.LastIndexOf('/') + 1;
        int dot = path.IndexOf('.', name);
        return dot < 0 ? path + "." + language : path[..dot] + "." + language + path[dot..];
    }

    /// <inheritdoc/>
    protected override void OnChanged() => _paths.Clear();
}
