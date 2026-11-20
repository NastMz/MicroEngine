using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using MicroEngine.Aseprite;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Localization;

namespace MicroEngine.Cli;

/// <summary>One finding of <c>microengine content check</c>: a file, what is wrong with it and what to do.</summary>
/// <param name="Error">Whether it fails the check (a warning does not).</param>
/// <param name="Path">The file's content path, '/'-separated.</param>
/// <param name="Message">What is wrong and what to do.</param>
public sealed record ContentIssue(bool Error, string Path, string Message)
{
    public override string ToString() => $"{(Error ? "error" : "warning")}: {Path}: {Message}";
}

/// <summary>What <c>microengine content check</c> found.</summary>
public sealed record ContentReport(int Files, IReadOnlyList<ContentIssue> Issues)
{
    public IReadOnlyList<ContentIssue> Errors => [.. Issues.Where(i => i.Error)];
    public IReadOnlyList<ContentIssue> Warnings => [.. Issues.Where(i => !i.Error)];
}

/// <summary>How <c>microengine content pack</c> packs.</summary>
/// <param name="Pivot">The anchor of loose images, as a fraction of each (0, 0: the top-left).</param>
/// <param name="Trim">Whether transparent margins are left out.</param>
/// <param name="Extrude">Whether each sprite's edge pixels repeat around it, for linear sampling and scaling.</param>
public sealed record PackOptions(Vector2 Pivot = default, bool Trim = false, bool Extrude = false);

/// <summary>What <c>microengine content pack</c> did.</summary>
public sealed record PackResult(bool UpToDate, int Sprites, int Clips, int Width, int Height, string Image, string Description);

/// <summary>
/// <c>microengine content</c>: the optional offline steps over a game's content folder (docs/guide/cli.md). <c>check</c>
/// reads every file the way the engine will and reports what would fail at run time; <c>names</c> writes the files',
/// sprites' and clips' names as C# constants; <c>pack</c> packs a folder of images and sheets into one atlas in the
/// JSON format the engine already loads. Nothing here is needed to run a game: files load as they are.
/// </summary>
public static class ContentTool
{
    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd" };
    // Pictures the engine does not decode: loading one fails at run time.
    private static readonly HashSet<string> UnreadImages = new(StringComparer.OrdinalIgnoreCase) { ".webp", ".tif", ".tiff", ".svg", ".dds", ".ktx", ".ktx2", ".avif", ".heic", ".ico" };
    // Sound formats no audio backend decodes.
    private static readonly HashSet<string> UnreadAudio = new(StringComparer.OrdinalIgnoreCase) { ".flac", ".m4a", ".aac", ".wma", ".aiff", ".aif", ".opus" };
    // Files of the tools that made the content: they are sources, not something the game loads.
    private static readonly HashSet<string> Sources = new(StringComparer.OrdinalIgnoreCase)
    {
        ".psd", ".xcf", ".kra", ".clip", ".pdn", ".afdesign", ".afphoto", ".sketch", ".fla", ".ai", ".blend", ".tps", ".ftpp", ".hlsl",
        ".tiled-project", ".tiled-session", ".bak", ".tmp", ".orig", ".swp",
    };
    private static readonly HashSet<string> Junk = new(StringComparer.OrdinalIgnoreCase) { "Thumbs.db", ".DS_Store", "desktop.ini" };

    /// <summary>The largest texture side every supported device takes; larger images are reported unless another limit is given.</summary>
    public const int DefaultMaxTexture = 4096;

    // ---- Files

    /// <summary>Every file under <paramref name="contentDir"/> as a '/'-separated relative path, in ordinal order (C# files are code, not content).</summary>
    public static IReadOnlyList<string> Files(string contentDir)
    {
        if (!Directory.Exists(contentDir)) throw new CliException($"the content folder {contentDir} does not exist. Give the folder with --content, or the game's project with --project.", 2);
        return [.. Directory.EnumerateFiles(contentDir, "*", SearchOption.AllDirectories)
            .Select(f => System.IO.Path.GetRelativePath(contentDir, f).Replace('\\', '/'))
            .Where(f => !f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)];
    }

    private static string Ext(string path) => System.IO.Path.GetExtension(path).ToLowerInvariant();

    private static string Folder(string path) => path.Contains('/') ? path[..path.LastIndexOf('/')] : "";

    private static string Join(string folder, string file) => folder.Length == 0 ? file : folder + "/" + file;

    /// <summary>What a description file (a sheet, an Aseprite file, a tool's atlas) holds, or null when the file is not one.</summary>
    private static SpriteSheet? Describe(string contentDir, string path, HashSet<string> files, out string? image)
    {
        image = null;
        string full = System.IO.Path.Combine(contentDir, path), folder = Folder(path);
        switch (Ext(path))
        {
            case ".sheet":
                return SpriteSheet.Parse(File.ReadAllText(full), name => Image(contentDir, Join(folder, name), files));
            case ".aseprite" or ".ase":
                return AsepriteAssets.Read(System.IO.Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(full));
            case ".json" or ".xml":
                string text = File.ReadAllText(full);
                if (!LooksLikeAtlas(text)) return null;
                var data = AtlasData.Parse(text);
                image = Join(folder, data.ImagePath ?? System.IO.Path.GetFileNameWithoutExtension(path) + ".png");
                return data.ToSheet(Image(contentDir, image, files));
            default:
                return null;
        }
    }

    /// <summary>Whether a JSON or XML file is an atlas description (it has "frames", or its root is a TextureAtlas) rather than a game's own data.</summary>
    private static bool LooksLikeAtlas(string text)
    {
        var start = text.AsSpan().TrimStart('﻿').TrimStart();
        if (start.StartsWith("<")) return text.Contains("<TextureAtlas", StringComparison.Ordinal);
        if (!start.StartsWith("{")) return false;
        try
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("frames", out _);
        }
        catch (JsonException) { return false; }
    }

    /// <summary>The image at a content path, found with its exact letter case, as the engine finds it on every system.</summary>
    private static PixelImage Image(string contentDir, string path, HashSet<string> files)
    {
        string normalized = Content.AssetPath.Normalize(path);
        if (!files.Contains(normalized))
        {
            string? other = files.FirstOrDefault(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));
            throw new FileNotFoundException(other is null ? $"'{normalized}' does not exist in the content folder." : $"'{normalized}' is spelled '{other}' in the content folder; names must match their letter case exactly.");
        }
        return PixelImage.Decode(File.ReadAllBytes(System.IO.Path.Combine(contentDir, normalized)));
    }

    // ---- check

    /// <summary>Reads every file of <paramref name="contentDir"/> the way the engine will, and reports what would fail or mislead.</summary>
    /// <param name="contentDir">The content folder.</param>
    /// <param name="maxTexture">The largest texture side allowed; null reports sides above <see cref="DefaultMaxTexture"/> as warnings.</param>
    public static ContentReport Check(string contentDir, int? maxTexture = null)
    {
        var files = Files(contentDir);
        var set = new HashSet<string>(files, StringComparer.Ordinal);
        var issues = new List<ContentIssue>(NameClashes(files));
        foreach (var path in files)
        {
            string full = System.IO.Path.Combine(contentDir, path), ext = Ext(path), name = System.IO.Path.GetFileName(path);
            void Error(string message) => issues.Add(new ContentIssue(true, path, message));
            void Warn(string message) => issues.Add(new ContentIssue(false, path, message));
            try
            {
                if (Junk.Contains(name)) { Warn("a file the system left behind ships with the game; delete it."); continue; }
                if (Sources.Contains(ext))
                    Warn($"a source file ({ext}) ships with the game: every file under the content folder is packaged. Keep sources outside it (for example in an Art folder next to the project){(ext == ".psd" ? "; the engine loads a PSD's flattened picture, but a PNG is smaller and loads faster" : "")}.");
                if (Images.Contains(ext))
                {
                    PixelImage image;
                    try { image = PixelImage.Decode(File.ReadAllBytes(full)); }
                    catch (Exception e) when (e is InvalidDataException or NotSupportedException) { Error($"not an image the engine reads ({e.Message}). Export it again as PNG, JPEG, BMP, TGA or GIF."); continue; }
                    int limit = maxTexture ?? DefaultMaxTexture;
                    if (image.Width > limit || image.Height > limit)
                    {
                        string message = $"the image is {image.Width}x{image.Height}, above the {limit}-pixel limit{(maxTexture is null ? " that phones and older graphics cards take" : "")}. Split it into smaller images or scale it down.";
                        if (maxTexture is null) Warn(message); else Error(message);
                    }
                }
                else if (UnreadImages.Contains(ext)) Error($"the engine does not read {ext} images; loading it fails. Convert it to PNG (or JPEG for photographs).");
                else if (ext == ".wav")
                {
                    try { AudioClip.FromWav(File.ReadAllBytes(full)); }
                    catch (Exception e) when (e is InvalidDataException or NotSupportedException or ArgumentException) { Error($"not a WAV the engine decodes ({e.Message}). Export it as PCM WAV, 8, 16 or 24 bits, mono or stereo."); }
                }
                else if (ext == ".ogg")
                {
                    var head = new byte[4];
                    using var stream = File.OpenRead(full);
                    if (stream.Read(head) < 4 || !head.AsSpan().SequenceEqual("OggS"u8)) Error("not an Ogg file, whatever its name says. Export the music as OGG Vorbis.");
                }
                else if (ext == ".mp3") Warn("MP3 music plays on the raylib platform only; on SDL (the default on every system, phones and browsers) it does not decode. Convert it to OGG Vorbis.");
                else if (UnreadAudio.Contains(ext)) Error($"no platform of the engine decodes {ext} audio. Convert effects to PCM WAV and music to OGG Vorbis.");
                else if (ext is ".ttf" or ".otf")
                {
                    var head = new byte[4];
                    using var stream = File.OpenRead(full);
                    bool font = stream.Read(head) == 4 && (head.AsSpan().SequenceEqual(new byte[] { 0, 1, 0, 0 }) || head.AsSpan().SequenceEqual("OTTO"u8) || head.AsSpan().SequenceEqual("true"u8) || head.AsSpan().SequenceEqual("ttcf"u8));
                    if (!font) Error("not a TrueType or OpenType font, whatever its name says.");
                }
                else if (path.EndsWith(CompiledShader.FileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    try { CompiledShader.Parse(File.ReadAllBytes(full)); }
                    catch (InvalidDataException e) { Error($"not a compiled shader this engine reads ({e.Message}). Compile it again with 'microengine shaders'."); }
                }
                else
                {
                    try { Describe(contentDir, path, set, out _); }
                    catch (Exception e) when (e is InvalidDataException or FileNotFoundException or NotSupportedException)
                    {
                        Error($"{e.Message} {(ext is ".aseprite" or ".ase" ? "Save it again from Aseprite." : "Fix the description or the image it names.")}");
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Error($"cannot be read ({e.Message})."); }
        }
        CheckStrings(contentDir, files, set, issues);
        return new ContentReport(files.Count, issues);
    }

    /// <summary>
    /// The string tables of the content (<c>*.strings.json</c>, <c>*.strings.csv</c>), read and compared as the game
    /// will use them: tables that cannot be read, keys a language lacks, values and plural forms that disagree
    /// (<see cref="StringTableChecks"/>), fonts a table names that are missing or lack characters its texts show, and
    /// variants of files per language that the game would never find.
    /// </summary>
    private static void CheckStrings(string contentDir, IReadOnlyList<string> files, HashSet<string> set, List<ContentIssue> issues)
    {
        var tables = new List<(string File, StringTableData Table)>();
        foreach (var path in files)
        {
            if (!StringTables.IsTable(path)) continue;
            try
            {
                string text = File.ReadAllText(System.IO.Path.Combine(contentDir, path));
                if (path.EndsWith(StringTables.JsonExtension, StringComparison.OrdinalIgnoreCase) && StringTables.LanguageOf(path) is null)
                    issues.Add(new ContentIssue(true, path, $"a JSON string table is named by its language tag (en{StringTables.JsonExtension}, pt-BR{StringTables.JsonExtension}), and this name is not one: the game refuses to load it. Rename it, or put texts shared by several languages in a {StringTables.CsvExtension} file."));
                else if (StringTables.LanguageOf(path) is { } language) tables.Add((path, StringTables.ParseJson(language, text)));
                else foreach (var table in StringTables.ParseCsv(text)) tables.Add((path, table));
            }
            catch (FormatException e) { issues.Add(new ContentIssue(true, path, $"not a string table the engine reads: {e.Message}")); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { issues.Add(new ContentIssue(true, path, $"cannot be read ({e.Message}).")); }
        }
        if (tables.Count == 0) return;

        // A finding belongs to the file that holds the key in that language, else to the language's largest table.
        string FileOf(string language, string? key) =>
            tables.FirstOrDefault(t => LanguageTag.Same(t.Table.Language, language) && key is not null && t.Table.Entries.ContainsKey(key)).File
            ?? tables.Where(t => LanguageTag.Same(t.Table.Language, language)).MaxBy(t => t.Table.Entries.Count).File;

        foreach (var issue in StringTableChecks.Check(tables.Select(t => t.Table)))
            issues.Add(new ContentIssue(issue.Error, FileOf(issue.Language, issue.Key), issue.Key is null ? issue.Message : $"'{issue.Key}': {issue.Message}"));

        // Fonts: each one a table names must exist, and together they must have the characters the language shows.
        var coverage = new Dictionary<string, FontCoverage?>(StringComparer.Ordinal);
        foreach (var language in tables.Select(t => t.Table.Language).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var own = tables.Where(t => LanguageTag.Same(t.Table.Language, language)).ToList();
            var named = own.LastOrDefault(t => t.Table.Fonts.Count > 0);
            if (named.File is null) continue;
            var chain = new List<FontCoverage>();
            bool complete = true;
            foreach (var font in named.Table.Fonts)
            {
                string normalized = Content.AssetPath.Normalize(font);
                if (!coverage.TryGetValue(normalized, out var read))
                {
                    if (!set.Contains(normalized))
                    {
                        string? other = files.FirstOrDefault(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));
                        issues.Add(new ContentIssue(true, named.File, other is null
                            ? $"@fonts names '{normalized}', which does not exist in the content folder."
                            : $"@fonts names '{normalized}', which is spelled '{other}' in the content folder; names must match their letter case exactly."));
                    }
                    else
                    {
                        try { read = FontCoverage.Read(File.ReadAllBytes(System.IO.Path.Combine(contentDir, normalized))); }
                        catch (InvalidDataException e) { issues.Add(new ContentIssue(true, named.File, $"@fonts names '{normalized}': {e.Message}")); }
                    }
                    coverage[normalized] = read;
                }
                if (read is null) complete = false; else chain.Add(read);
            }
            if (!complete) continue;
            var shown = new StringBuilder();
            foreach (var (_, table) in own)
            {
                shown.Append(table.Name);
                foreach (var text in table.Entries.Values) shown.Append(text);
            }
            string missing = FontCoverage.Missing(shown.ToString(), [.. chain]);
            if (missing.Length == 0) continue;
            int count = missing.EnumerateRunes().Count();
            string sample = count <= 24 ? missing : string.Concat(missing.EnumerateRunes().Take(24).Select(r => r.ToString())) + "...";
            issues.Add(new ContentIssue(false, named.File,
                $"the texts of '{language}' show {count} character{(count == 1 ? "" : "s")} its fonts ({string.Join(", ", named.Table.Fonts)}) lack, drawn as '?': {sample}. Add a font that has them to @fonts."));
        }

        // Variants of a file per language (name.<language>.ext): found only beside their file, spelled as the language's tag.
        var languages = tables.Select(t => t.Table.Language).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var path in files)
        {
            if (StringTables.IsTable(path)) continue;
            int name = path.LastIndexOf('/') + 1;
            int first = path.IndexOf('.', name);
            int second = first < 0 ? -1 : path.IndexOf('.', first + 1);
            if (second < 0) continue;
            string tag = path[(first + 1)..second];
            if (languages.FirstOrDefault(l => string.Equals(l, tag, StringComparison.OrdinalIgnoreCase)) is not { } language) continue;
            string original = path[..first] + path[second..];
            if (tag != language)
                issues.Add(new ContentIssue(true, path, $"a variant for '{language}' whose name spells the language '{tag}': the game looks for '{path[..first]}.{language}{path[second..]}' and does not find it. Rename the file."));
            else if (!set.Contains(original))
                issues.Add(new ContentIssue(false, path, $"looks like the variant of '{original}' for '{language}', but that file does not exist: the game loads a variant only in place of its file. Add '{original}' (the file for the other languages), or rename this one."));
        }
    }

    /// <summary>
    /// Names that only some systems can tell apart: two files (or two folders) whose paths differ only in letter case.
    /// Windows and macOS keep one of them, Linux, Android and browsers keep both, so a game would load different files
    /// on different systems.
    /// </summary>
    public static IReadOnlyList<ContentIssue> NameClashes(IEnumerable<string> files)
    {
        var issues = new List<ContentIssue>();
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var folders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in files.Order(StringComparer.Ordinal))
        {
            string folder = Folder(path), built = "";
            foreach (var part in folder.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                built = Join(built, part);
                if (folders.TryGetValue(built, out var known) && known != built && reported.Add(known + "|" + built))
                    issues.Add(new ContentIssue(true, path, $"the folders '{known}' and '{built}' differ only in letter case: Linux, Android and browsers keep both, Windows and macOS merge them. Give them one spelling."));
                folders.TryAdd(built, built);
            }
            if (seen.TryGetValue(path, out var first) && first != path)
            {
                if (Folder(first) == folder)
                    issues.Add(new ContentIssue(true, path, $"differs from '{first}' only in letter case: Linux, Android and browsers keep both files, Windows and macOS keep one. Rename one of them."));
            }
            else seen[path] = path;
        }
        return issues;
    }

    // ---- names

    /// <summary>
    /// The C# file that names every content file as a constant holding its path, in classes that follow the folders.
    /// A sheet, an Aseprite file or an atlas becomes a class with its <c>Path</c> and the names of its sprites, strips
    /// (the numbered frames <c>atlas.Frames</c> takes) and clips, so a name that no longer exists fails to compile.
    /// </summary>
    public static string Names(string contentDir, string @namespace, string className)
    {
        var files = Files(contentDir);
        var set = new HashSet<string>(files, StringComparer.Ordinal);
        var root = new NameNode(className);
        foreach (var path in files)
        {
            if (Junk.Contains(System.IO.Path.GetFileName(path))) continue;
            var node = root;
            foreach (var part in Folder(path).Split('/', StringSplitOptions.RemoveEmptyEntries)) node = node.Child(part);
            SpriteSheet? sheet = null;
            try { sheet = Describe(contentDir, path, set, out _); }
            catch (Exception e) when (e is InvalidDataException or FileNotFoundException or NotSupportedException or IOException) { }
            node.Files.Add((path, sheet));
        }
        var code = new StringBuilder();
        code.Append("// <auto-generated>\n");
        code.Append("// Written by `microengine content names` from the content folder. Do not edit: run the command again after adding,\n");
        code.Append("// renaming or removing content (`--check` fails when this file is out of date).\n");
        code.Append("// </auto-generated>\n");
        code.Append("#nullable enable\n\n");
        code.Append("namespace ").Append(@namespace).Append(";\n\n");
        code.Append("/// <summary>The names of the game's content: every file's path, and the sprites, strips and clips of its sheets and atlases.</summary>\n");
        root.Write(code, 0, "internal static class", null);
        return code.ToString();
    }

    private sealed class NameNode(string name)
    {
        public string Name { get; } = name;
        public SortedDictionary<string, NameNode> Folders { get; } = new(StringComparer.Ordinal);
        public List<(string Path, SpriteSheet? Sheet)> Files { get; } = [];

        public NameNode Child(string folder) => Folders.TryGetValue(folder, out var node) ? node : Folders[folder] = new NameNode(folder);

        public void Write(StringBuilder code, int depth, string declaration, string? identifier)
        {
            string indent = new(' ', depth * 4), self = identifier ?? Identifier(Name);
            code.Append(indent).Append(declaration).Append(' ').Append(self).Append('\n').Append(indent).Append("{\n");
            var taken = new HashSet<string>(StringComparer.Ordinal) { self };
            // Folders first, so a file named like a folder is the one that takes a suffix.
            var folders = Folders.Values.Select(f => (Node: f, Id: Unique(Identifier(f.Name), taken))).ToList();
            // Two files of one name in a folder are told apart by their extensions; a description keeps the bare name.
            var stems = Files.GroupBy(f => Identifier(System.IO.Path.GetFileNameWithoutExtension(f.Path)), StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            bool first = true;
            foreach (var (path, sheet) in Files)
            {
                string stem = Identifier(System.IO.Path.GetFileNameWithoutExtension(path));
                // A class cannot hold a member of its own name: such a file takes its extension too.
                string wanted = (stems[stem] > 1 && sheet is null) || stem == self ? stem + Identifier(System.IO.Path.GetExtension(path).TrimStart('.')) : stem;
                string id = Unique(wanted, taken);
                if (sheet is null)
                {
                    code.Append(indent).Append("    public const string ").Append(id).Append(" = ").Append(Literal(path)).Append(";\n");
                    continue;
                }
                if (!first) code.Append('\n');
                WriteSheet(code, depth + 1, id, path, sheet);
                first = false;
            }
            foreach (var (node, id) in folders)
            {
                if (Files.Count > 0 || node != folders[0].Node) code.Append('\n');
                node.Write(code, depth + 1, "public static class", id);
            }
            code.Append(indent).Append("}\n");
        }

        private static void WriteSheet(StringBuilder code, int depth, string id, string path, SpriteSheet sheet)
        {
            string indent = new(' ', depth * 4);
            code.Append(indent).Append("/// <summary>").Append(path).Append(": ").Append(sheet.Frames.Count).Append(" sprites, ").Append(sheet.Clips.Count).Append(" clips.</summary>\n");
            code.Append(indent).Append("public static class ").Append(id).Append('\n').Append(indent).Append("{\n");
            code.Append(indent).Append("    public const string Path = ").Append(Literal(path)).Append(";\n");
            var names = sheet.Frames.Select(f => f.Name).ToList();
            var all = new HashSet<string>(names, StringComparer.Ordinal);
            // "name0", "name1"... (or from 1) is the strip "name", which atlas.Frames(name) takes.
            var strips = new List<string>();
            foreach (var name in names)
            {
                int digits = name.Length;
                while (digits > 0 && char.IsAsciiDigit(name[digits - 1])) digits--;
                if (digits == name.Length || digits == 0) continue;
                string prefix = name[..digits];
                if (name[digits..] is "0" or "1" && !strips.Contains(prefix) && (name[digits..] == "0" || !all.Contains(prefix + "0"))) strips.Add(prefix);
            }
            Group(code, depth + 1, "Sprites", "The sprites, by the name atlas[name] takes.", names, id);
            if (strips.Count > 0) Group(code, depth + 1, "Strips", "Runs of numbered sprites, by the name atlas.Frames(name) takes.", strips, id);
            if (sheet.Clips.Count > 0) Group(code, depth + 1, "Clips", "The animations, by the name atlas.Clip(name) and an Animator take.", [.. sheet.Clips.Select(c => c.Name)], id);
            code.Append(indent).Append("}\n");
        }

        private static void Group(StringBuilder code, int depth, string name, string summary, IReadOnlyList<string> values, string owner)
        {
            // A class cannot hold a member of its own name ("sprites.sheet" would hold a class Sprites).
            if (name == owner) name = name[..^1] + "Names";
            string indent = new(' ', depth * 4);
            code.Append('\n').Append(indent).Append("/// <summary>").Append(summary).Append("</summary>\n");
            code.Append(indent).Append("public static class ").Append(name).Append('\n').Append(indent).Append("{\n");
            var taken = new HashSet<string>(StringComparer.Ordinal) { name, owner };
            foreach (var value in values)
                code.Append(indent).Append("    public const string ").Append(Unique(Identifier(value), taken)).Append(" = ").Append(Literal(value)).Append(";\n");
            code.Append(indent).Append("}\n");
        }

        private static string Unique(string wanted, HashSet<string> taken)
        {
            string id = wanted;
            for (int n = 2; !taken.Add(id); n++) id = wanted + "_" + n.ToString(CultureInfo.InvariantCulture);
            return id;
        }
    }

    /// <summary>A C# identifier for a file, folder or sprite name: words joined in PascalCase, an underscore before a leading digit.</summary>
    public static string Identifier(string name)
    {
        var id = new StringBuilder(name.Length);
        bool upper = true;
        foreach (char c in name)
        {
            if (!char.IsAsciiLetterOrDigit(c)) { upper = true; continue; }
            id.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }
        if (id.Length == 0) return "_";
        if (char.IsAsciiDigit(id[0])) id.Insert(0, '_');
        return id.ToString();
    }

    private static string Literal(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    // ---- pack

    /// <summary>
    /// Packs the images, sheets, Aseprite files and atlases directly inside <paramref name="sourceDir"/> into
    /// <paramref name="outputBase"/>.png and <paramref name="outputBase"/>.json: an atlas the engine loads with
    /// <c>Assets.Atlas</c>. Loose images are named by file name, as <c>Assets.AtlasFromFolder</c> names them; an image a
    /// sheet or atlas of the folder cuts up is not also a sprite of its own.
    /// </summary>
    /// <param name="sourceDir">The folder to pack.</param>
    /// <param name="outputBase">The output path without an extension.</param>
    /// <param name="options">Pivot, trimming and extrusion.</param>
    /// <param name="check">Write nothing: only say whether the files on disk are what packing gives now.</param>
    public static PackResult Pack(string sourceDir, string outputBase, PackOptions options, bool check)
    {
        if (!Directory.Exists(sourceDir)) throw new CliException($"the folder to pack, {sourceDir}, does not exist.", 2);
        var files = Directory.EnumerateFiles(sourceDir).Select(f => System.IO.Path.GetFileName(f)!).Order(StringComparer.Ordinal).ToList();
        var set = new HashSet<string>(files, StringComparer.Ordinal);
        var builder = new SpriteAtlasBuilder();
        var used = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var file in files)
            {
                SpriteSheet? sheet;
                string? image = null;
                if (Ext(file) == ".sheet")
                    sheet = SpriteSheet.Parse(File.ReadAllText(System.IO.Path.Combine(sourceDir, file)), name => { used.Add(Content.AssetPath.Normalize(name)); return Image(sourceDir, name, set); }, new AtlasOptions(options.Pivot));
                else sheet = Describe(sourceDir, file, set, out image);
                if (sheet is null) continue;
                if (image is not null) used.Add(image);
                builder.AddSheet(sheet);
            }
            foreach (var file in files)
            {
                if (!Images.Contains(Ext(file)) || used.Contains(file)) continue;
                PixelImage image;
                try { image = PixelImage.Decode(File.ReadAllBytes(System.IO.Path.Combine(sourceDir, file))); }
                catch (Exception e) when (e is InvalidDataException or NotSupportedException) { throw new InvalidDataException($"{file}: {e.Message}", e); }
                builder.Add(System.IO.Path.GetFileNameWithoutExtension(file), image, options.Pivot * new Vector2(image.Width, image.Height));
            }
        }
        catch (Exception e) when (e is InvalidDataException or FileNotFoundException or NotSupportedException or ArgumentException)
        {
            throw new CliException($"{sourceDir} cannot be packed: {e.Message}");
        }
        if (builder.Count == 0) throw new CliException($"{sourceDir} holds no image, sheet or atlas to pack.");
        var packed = builder.Pack(options.Extrude, options.Trim);
        string png = outputBase + ".png", json = outputBase + ".json";
        string description = packed.ToJson(System.IO.Path.GetFileName(png));
        bool upToDate = File.Exists(png) && File.Exists(json) && File.ReadAllText(json) == description && SamePixels(png, packed.Image);
        if (!check && !upToDate)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(png))!);
            File.WriteAllBytes(png, PngEncoder.Encode(packed.Image.ToRgba(), packed.Image.Width, packed.Image.Height));
            File.WriteAllText(json, description);
        }
        return new PackResult(upToDate, packed.Regions.Count, packed.Clips.Count, packed.Image.Width, packed.Image.Height, png, json);
    }

    /// <summary>Whether the picture on disk holds the pixels of <paramref name="image"/> (compared decoded: PNG compression may differ between .NET versions).</summary>
    private static bool SamePixels(string png, PixelImage image)
    {
        try
        {
            var existing = PixelImage.Decode(File.ReadAllBytes(png));
            return existing.Width == image.Width && existing.Height == image.Height && existing.Pixels.SequenceEqual(image.Pixels);
        }
        catch (Exception e) when (e is InvalidDataException or NotSupportedException or IOException) { return false; }
    }

    // ---- the command

    /// <summary><c>microengine content &lt;check|names|pack&gt;</c>.</summary>
    public static int Run(Options o)
    {
        string action = o.Positional.FirstOrDefault() ?? throw new CliException("choose what to do: microengine content <check|names|pack> (see microengine content --help).", 2);
        string? projectOption = o.Get("project"), contentOption = o.Get("content");
        bool check = o.Has("check");
        // The content folder: --content, or Content/ next to the project (found from --project or this folder). A
        // command given every path it needs looks for no project.
        bool needsProject = contentOption is null && !(action == "pack" && o.Has("out"));
        string? projectDir = !needsProject && projectOption is null ? null : System.IO.Path.GetDirectoryName(GameProject.Locate(projectOption));
        string contentDir = contentOption is not null ? System.IO.Path.GetFullPath(contentOption) : projectDir is not null ? System.IO.Path.Combine(projectDir, "Content") : "";
        switch (action)
        {
            case "check":
            {
                string? max = o.Get("max-texture");
                o.RejectUnknown("content");
                int? limit = max is null ? null : int.TryParse(max, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : throw new CliException("--max-texture takes a positive whole number of pixels.", 2);
                var report = Check(contentDir, limit);
                foreach (var issue in report.Issues) Console.WriteLine(issue);
                Console.WriteLine($"content: {report.Files} files, {report.Errors.Count} errors, {report.Warnings.Count} warnings ({contentDir})");
                return report.Errors.Count > 0 ? 1 : 0;
            }
            case "names":
            {
                string? outOption = o.Get("out"), ns = o.Get("namespace"), name = o.Get("class");
                o.RejectUnknown("content");
                string output = System.IO.Path.GetFullPath(outOption ?? System.IO.Path.Combine(projectDir ?? throw new CliException("say where the file goes with --out (or give the game's project with --project).", 2), "GameContent.g.cs"));
                ns ??= projectDir is not null ? Identifier(System.IO.Path.GetFileNameWithoutExtension(GameProject.Locate(projectOption))) : throw new CliException("give the namespace of the generated class with --namespace.", 2);
                string code = Names(contentDir, ns, name ?? "GameContent");
                bool same = File.Exists(output) && File.ReadAllText(output).ReplaceLineEndings("\n") == code;
                if (check)
                {
                    Console.WriteLine(same ? $"content names: up to date ({output})" : $"out of date: {output} (run microengine content names)");
                    return same ? 0 : 1;
                }
                if (!same)
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
                    File.WriteAllText(output, code);
                }
                Console.WriteLine($"{(same ? "unchanged" : "wrote")} {output}");
                return 0;
            }
            case "pack":
            {
                string source = o.Positional.Skip(1).FirstOrDefault() ?? throw new CliException("say which folder to pack: microengine content pack <folder> --out <path without extension>.", 2);
                string? outOption = o.Get("out"), pivotOption = o.Get("pivot");
                bool trim = o.Has("trim"), extrude = o.Has("extrude");
                o.RejectUnknown("content");
                source = System.IO.Path.GetFullPath(source);
                string output = System.IO.Path.GetFullPath(outOption ?? System.IO.Path.Combine(contentDir, "atlases", System.IO.Path.GetFileName(source.TrimEnd('/', '\\'))));
                if (System.IO.Path.GetExtension(output).ToLowerInvariant() is ".json" or ".png") output = System.IO.Path.ChangeExtension(output, null);
                var pivot = Vector2.Zero;
                if (pivotOption is not null)
                {
                    var parts = pivotOption.Split(',');
                    if (parts.Length != 2 || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        throw new CliException("--pivot takes two fractions separated by a comma, for example 0.5,1 (the middle of the bottom edge).", 2);
                    pivot = new Vector2(x, y);
                }
                var result = Pack(source, output, new PackOptions(pivot, trim, extrude), check);
                if (check)
                {
                    Console.WriteLine(result.UpToDate ? $"content pack: up to date ({result.Description})" : $"out of date: {result.Description} (run microengine content pack again)");
                    return result.UpToDate ? 0 : 1;
                }
                Console.WriteLine($"{(result.UpToDate ? "unchanged" : "wrote")} {result.Description} and {System.IO.Path.GetFileName(result.Image)}: {result.Sprites} sprites, {result.Clips} clips, {result.Width}x{result.Height}");
                return 0;
            }
            default:
                throw new CliException($"unknown content command '{action}': use check, names or pack.", 2);
        }
    }
}
