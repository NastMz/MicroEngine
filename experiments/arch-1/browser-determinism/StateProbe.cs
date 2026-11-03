// SCRATCH PROBE (ARCH-1 browser determinism investigation). Not part of the engine.
// Copied into src/MicroEngine/Core/ of a scratch copy by apply-probe.mjs; never built from here.
// After every fixed step it prints one line with hashes of the simulation state, per component:
//   T0 u=<update> f=<frame> s=<step in frame> rnd=<hash> scn=<hash> game=<hash> aud=<hash> inp=<hash> ast=<hash> top=<scene>
// After every frame's updates: T0F f=<frame> u=<update> steps=<n> blend=<FrameBlend>
// MICROENGINE_T0_DUMP=a-b (or a single update) also prints every field as "T0D u=<update> <path>=<value>".
// MICROENGINE_T0_EVERY=n hashes only every n-th update (default 1). MICROENGINE_T0_SKIP=prefix,... leaves out more types;
// MICROENGINE_T0_SKIPFIELDS=name,... leaves out fields by name (state written while drawing).
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MicroEngine.Core;

internal static class StateProbe
{
    private static readonly long DumpFrom, DumpTo;
    private static readonly int Every;
    private static readonly bool Enabled;

    static StateProbe()
    {
        Enabled = Environment.GetEnvironmentVariable("MICROENGINE_T0") != "0";
        Every = int.TryParse(Environment.GetEnvironmentVariable("MICROENGINE_T0_EVERY"), out int e) && e > 0 ? e : 1;
        string? dump = Environment.GetEnvironmentVariable("MICROENGINE_T0_DUMP");
        DumpFrom = DumpTo = -1;
        if (!string.IsNullOrEmpty(dump))
        {
            var parts = dump.Split('-');
            DumpFrom = long.Parse(parts[0], CultureInfo.InvariantCulture);
            DumpTo = parts.Length > 1 ? long.Parse(parts[1], CultureInfo.InvariantCulture) : DumpFrom;
        }
    }

    public static void Frame(Game game, long frame, int steps)
    {
        if (!Enabled) return;
        Console.WriteLine(FormattableString.Invariant($"T0F f={frame} u={game.UpdateCount} steps={steps} blend={game.FrameBlend:R}"));
    }

    public static void Capture(Game game, string name) =>
        Console.WriteLine(FormattableString.Invariant($"T0C {name} u={game.UpdateCount} blend={game.FrameBlend:R}"));

    public static void Step(Game game, long frame, int stepInFrame)
    {
        if (!Enabled) return;
        long u = game.UpdateCount;
        bool dump = u >= DumpFrom && u <= DumpTo;
        if (!dump && u % Every != 0) return;
        var w = new Walker(dump ? u : -1);
        string rnd = w.Root("rnd", game.Random);
        // Scenes reach the engine services; those are hashed under their own names, not again under scn and game.
        string scn = w.Root("scn", game.Scenes, game, game.Audio, game.Assets, game.Input, game.Touch, game.Font);
        string gm = w.GameFields(game);
        string aud = w.Root("aud", game.Audio, game);
        string inp = w.Root("inp", game.Input, game);
        string ast = w.Root("ast", game.Assets, game);
        Console.WriteLine(FormattableString.Invariant(
            $"T0 u={u} f={frame} s={stepInFrame} t={game.Time:R} rnd={rnd} scn={scn} game={gm} aud={aud} inp={inp} ast={ast} top={game.Scenes.Current?.GetType().Name}"));
    }

    private sealed class Walker(long dumpUpdate)
    {
        private ulong _h;
        private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);
        private static readonly Dictionary<Type, FieldInfo[]> Fields = [];
        private static readonly string[] SkipPrefixes =
        [
            "MicroEngine.Graphics.SpriteAtlas", "MicroEngine.Graphics.BitmapFont", "MicroEngine.Graphics.ScalableFont",
            "MicroEngine.Graphics.LightMap", "MicroEngine.Graphics.PixelImage",
            .. (Environment.GetEnvironmentVariable("MICROENGINE_T0_SKIP") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries),
        ];
        // Fields by name (MICROENGINE_T0_SKIPFIELDS=_hud,_motion): state a scene writes while drawing, not while updating.
        private static readonly HashSet<string> SkipFields =
            [.. (Environment.GetEnvironmentVariable("MICROENGINE_T0_SKIPFIELDS") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)];

        public string Root(string name, object? root, params object[] skip)
        {
            _h = 14695981039346656037UL;
            _seen.Clear();
            foreach (var s in skip) _seen.Add(s);
            Walk(root, name, 0);
            return _h.ToString("x16", CultureInfo.InvariantCulture);
        }

        public string GameFields(Game game)
        {
            _h = 14695981039346656037UL;
            _seen.Clear();
            _seen.Add(game);
            _seen.Add(game.Scenes); _seen.Add(game.Audio); _seen.Add(game.Input); _seen.Add(game.Assets); _seen.Add(game.Random);
            _seen.Add(game.Touch); _seen.Add(game.Font);
            for (var t = game.GetType(); t is not null && t != typeof(Game); t = t.BaseType)
                foreach (var f in FieldsOf(t, declaredOnly: true)) Walk(f.GetValue(game), "game." + f.Name, 1);
            return _h.ToString("x16", CultureInfo.InvariantCulture);
        }

        private void Mix(ulong v)
        {
            for (int i = 0; i < 8; i++) { _h ^= (byte)(v >> (i * 8)); _h *= 1099511628211UL; }
        }

        private void MixBytes(ReadOnlySpan<byte> bytes)
        {
            foreach (byte b in bytes) { _h ^= b; _h *= 1099511628211UL; }
        }

        private void Leaf(string path, string text, ulong bits)
        {
            Mix(bits);
            if (dumpUpdate >= 0) Console.WriteLine($"T0D u={dumpUpdate} {path}={text}");
        }

        private static bool Skipped(Type t)
        {
            if (typeof(Delegate).IsAssignableFrom(t) || t.IsPointer || t == typeof(IntPtr) || t == typeof(UIntPtr)) return true;
            if (typeof(MemberInfo).IsAssignableFrom(t) || typeof(System.Threading.Tasks.Task).IsAssignableFrom(t) || typeof(Stream).IsAssignableFrom(t)) return true;
            // Platform-side objects, GPU resources and the save location are environment, not simulation state: a
            // headless texture keeps its pixels where raylib's does not, and every run gets its own save folder.
            if (typeof(IPlatform).IsAssignableFrom(t) || typeof(MicroEngine.Graphics.Texture).IsAssignableFrom(t)
                || typeof(MicroEngine.Graphics.IGraphicsDevice).IsAssignableFrom(t) || typeof(MicroEngine.Graphics.IRenderer).IsAssignableFrom(t)
                || typeof(MicroEngine.Audio.IAudioDevice).IsAssignableFrom(t) || typeof(ISaveStorage).IsAssignableFrom(t)
                || typeof(MicroEngine.Input.IInputSource).IsAssignableFrom(t) || typeof(MicroEngine.Input.ITouchScreen).IsAssignableFrom(t))
                return true;
            // Built-at-load art (atlases, fonts, light maps, images, a game's Art namespace) never changes during play and
            // only adds bulk; MICROENGINE_T0_SKIP adds type-name prefixes to leave out.
            string name = t.FullName ?? t.Name;
            foreach (var prefix in SkipPrefixes) if (name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            string ns = t.Namespace ?? "";
            if (ns.EndsWith(".Art", StringComparison.Ordinal)) return true;
            return ns.StartsWith("MicroEngine.Raylib", StringComparison.Ordinal) || ns.StartsWith("MicroEngine.Sdl", StringComparison.Ordinal)
                || ns.StartsWith("Raylib_cs", StringComparison.Ordinal) || ns.StartsWith("SDL", StringComparison.Ordinal)
                || ns.StartsWith("System.Threading", StringComparison.Ordinal) || ns.StartsWith("System.Runtime", StringComparison.Ordinal)
                || ns.StartsWith("System.Diagnostics", StringComparison.Ordinal) || ns.StartsWith("System.Text.Json", StringComparison.Ordinal);
        }

        private static FieldInfo[] FieldsOf(Type t, bool declaredOnly = false)
        {
            if (!declaredOnly && Fields.TryGetValue(t, out var cached)) return cached;
            var list = new List<FieldInfo>();
            for (var c = t; c is not null && c != typeof(object); c = declaredOnly ? null : c.BaseType)
                list.AddRange(c.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(f => !f.FieldType.IsPointer));
            var result = list.OrderBy(f => f.DeclaringType!.Name, StringComparer.Ordinal).ThenBy(f => f.Name, StringComparer.Ordinal).ToArray();
            if (!declaredOnly) Fields[t] = result;
            return result;
        }

        private void Walk(object? o, string path, int depth)
        {
            if (o is null) { Leaf(path, "null", 0x6e756c6c); return; }
            var t = o.GetType();
            switch (o)
            {
                case float f: Leaf(path, f.ToString("R", CultureInfo.InvariantCulture), BitConverter.SingleToUInt32Bits(f)); return;
                case double d: Leaf(path, d.ToString("R", CultureInfo.InvariantCulture), BitConverter.DoubleToUInt64Bits(d)); return;
                case bool b: Leaf(path, b ? "true" : "false", b ? 1UL : 0UL); return;
                case char c: Leaf(path, ((int)c).ToString(CultureInfo.InvariantCulture), c); return;
                case string s:
                    MixBytes(MemoryMarshal.AsBytes(s.AsSpan()));
                    if (dumpUpdate >= 0) Console.WriteLine($"T0D u={dumpUpdate} {path}=\"{s}\"");
                    return;
                case Enum e: Leaf(path, e.ToString(), (ulong)e.GetHashCode()); return;
            }
            if (t.IsPrimitive)
            {
                ulong bits = unchecked(o switch
                {
                    int i => (ulong)i, long l => (ulong)l, uint ui => ui, ulong ul => ul, short sh => (ulong)sh,
                    ushort us => us, byte by => by, sbyte sb => (ulong)sb, _ => 0UL,
                });
                Leaf(path, Convert.ToString(o, CultureInfo.InvariantCulture)!, bits);
                return;
            }
            if (t == typeof(decimal) || t == typeof(DateTime) || t == typeof(TimeSpan) || t == typeof(Guid)) { Leaf(path, o.ToString()!, (ulong)o.GetHashCode()); return; }
            if (Skipped(t)) return;
            if (depth > 12) { Leaf(path, "<deep>", 1); return; }
            if (!t.IsValueType && !_seen.Add(o)) { Leaf(path, "<seen>", 2); return; }

            if (o is Array a)
            {
                var et = t.GetElementType()!;
                Leaf(path + ".Length", a.Length.ToString(CultureInfo.InvariantCulture), (ulong)a.Length);
                if (et.IsPrimitive && a.Rank == 1)
                {
                    // Primitive arrays: bytes at once (fast); dump only a short prefix.
                    var bytes = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(a), Buffer.ByteLength(a));
                    MixBytes(bytes);
                    if (dumpUpdate >= 0)
                        for (int i = 0; i < Math.Min(a.Length, 64); i++) Console.WriteLine($"T0D u={dumpUpdate} {path}[{i}]={Format(a.GetValue(i))}");
                    return;
                }
                int k = 0;
                foreach (var item in a) { if (k >= 20000) break; Walk(item, $"{path}[{k}]", depth + 1); k++; }
                return;
            }
            if (o is IDictionary dict)
            {
                int k = 0;
                foreach (DictionaryEntry entry in dict) { Walk(entry.Key, $"{path}{{{k}}}.key", depth + 1); Walk(entry.Value, $"{path}{{{k}}}.value", depth + 1); k++; }
                return;
            }
            if ((t.Namespace ?? "").StartsWith("System.Collections", StringComparison.Ordinal) && o is IEnumerable items)
            {
                // Collections only (never iterators: enumerating a coroutine would run it).
                int k = 0;
                foreach (var item in items) { if (k >= 20000) break; Walk(item, $"{path}[{k}]", depth + 1); k++; }
                Leaf(path + ".Count", k.ToString(CultureInfo.InvariantCulture), (ulong)k);
                return;
            }
            foreach (var f in FieldsOf(t))
            {
                if (Skipped(f.FieldType) && !f.FieldType.IsInterface && f.FieldType != typeof(object)) continue;
                if (SkipFields.Contains(f.Name)) continue;
                object? v;
                try { v = f.GetValue(o); } catch { continue; }
                Walk(v, path + "." + f.Name, depth + 1);
            }
        }

        private static string Format(object? v) => v switch
        {
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "null",
        };
    }
}
