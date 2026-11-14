#!/usr/bin/env node
// Saves without reflection, checked the way an outside developer meets them: a console project in a folder outside this
// repository, built from the packages a release would publish, that defines its own save types.
//
//   node sdk/tests/trimmed.mjs --feed dist/sdk-feed [--aot] [--keep]
//
// 1. "typed": the game declares its save types in a JsonSerializerContext and passes the metadata to SaveStore. It is
//    published trimmed and self-contained with every trim warning an error, and the published program is run: it must
//    save, load, read a slot an earlier version wrote, and set a corrupt slot aside. It also tracks achievements with
//    that store (the engine's own saved type): they must unlock, persist, and load a file an earlier version wrote.
// 2. "plain": the same game with Save(slot, data) and Load<T>(slot). Its build, with the trim and AOT analyzers on, must
//    name those two calls (IL2026, IL3050) in the game's own file, and the trimmed program must fail in them.
// 3. --aot: the typed game is also published with NativeAOT and run. NativeAOT needs the platform's native linker
//    (Visual Studio's C++ tools, clang or Xcode); without it this step fails, and that says nothing about the SDK.
// 4. --aot: "windowed", a game with a window (MicroEngine.Desktop: a sprite, text, a synthesized sound, a scene), is
//    published with NativeAOT and played by a script in a hidden window on SDL's CPU renderer: it must reach the
//    script's end and write captures with a picture in them. On Linux it needs a display (xvfb-run).
// Every program is started by its published executable, never through `dotnet`, and a NativeAOT publish must leave no
// managed assembly of the game beside it. Any FAIL line makes the exit code 1; a missing feed makes it 2.
// NuGet sees only the feed and nuget.org, with its own package cache, so nothing comes from this repository's build.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import zlib from 'node:zlib';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const args = process.argv.slice(2);
const option = name => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : undefined; };
const feed = path.resolve(option('feed') ?? path.join(repo, 'dist', 'sdk-feed'));
const version = fs.readFileSync(path.join(repo, 'Directory.Build.props'), 'utf8').match(/<MicroEngineVersion>([^<]+)</)[1];
const rid = `${{ win32: 'win', linux: 'linux', darwin: 'osx' }[process.platform]}-${process.arch === 'arm64' ? 'arm64' : 'x64'}`;
const exe = process.platform === 'win32' ? '.exe' : '';

if (!fs.existsSync(feed) || !fs.readdirSync(feed).some(f => f === `MicroEngine.${version}.nupkg`)) {
  console.error(`error: ${feed} holds no MicroEngine ${version} package; run: dotnet pack MicroEngine.slnx -c Release -o ${feed}`);
  process.exit(2);
}
const work = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-trimmed-'));
if (work.startsWith(repo)) throw new Error('the external project must live outside the repository');

let failed = 0;
const check = (name, ok, detail = '') => {
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? `: ${detail}` : ''}`);
  if (!ok) failed++;
  return ok;
};
const run = (command, list, cwd) => {
  const result = spawnSync(command, list, { cwd, encoding: 'utf8' });
  return { code: result.status, text: `${result.stdout ?? ''}${result.stderr ?? ''}` };
};
const diagnostics = text => [...new Set(text.split(/\r?\n/).filter(line => /(warning|error) (IL|CS|NETSDK|MSB)\d+/.test(line)).map(line => line.replace(/\s*\[[^\]]*\]\s*$/, '').trim()))];

const project = packageId => `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="${packageId}" Version="${version}" />
  </ItemGroup>
</Project>
`;
const nugetConfig = `<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="feed" value="${feed}" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <config><add key="globalPackagesFolder" value="${path.join(work, 'packages')}" /></config>
</configuration>
`;
// The game's own save types: a nested object, collections, an enum and a base class with derived types.
const model = `using System.Text.Json.Serialization;

public enum Difficulty { Easy, Hard }

public sealed class Progress
{
    public int Level { get; set; } = 1;
    public Hero Hero { get; set; } = new();
    public Difficulty Difficulty { get; set; }
    public Dictionary<string, int> Scores { get; set; } = [];
    public List<Item> Bag { get; set; } = [];
}

public sealed class Hero
{
    public string Name { get; set; } = "";
    public List<string> Visited { get; set; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Potion), "potion")]
[JsonDerivedType(typeof(Key), "key")]
public abstract class Item { public string Name { get; set; } = ""; }
public sealed class Potion : Item { public int Heals { get; set; } }
public sealed class Key : Item { public string Door { get; set; } = ""; }

[JsonSerializable(typeof(Progress))]
public partial class GameSaves : JsonSerializerContext;
`;
const program = (save, load) => `using MicroEngine.Core;

var dir = Path.Combine(Path.GetTempPath(), "microengine-trimmed-run-" + Guid.NewGuid().ToString("N"));
try
{
    var store = new SaveStore("com.example.trimmed", dir);
    var progress = new Progress
    {
        Level = 3,
        Hero = new Hero { Name = "Ada", Visited = ["forest", "cave"] },
        Difficulty = Difficulty.Hard,
        Scores = new() { ["forest"] = 1200 },
        Bag = [new Potion { Name = "red", Heals = 5 }, new Key { Name = "brass", Door = "cellar" }],
    };
    Console.WriteLine("missing: " + (${load('"progress"')} is null));
    Console.WriteLine("save: " + ${save('"progress"', 'progress')});
    var loaded = ${load('"progress"')};
    Console.WriteLine($"load: {loaded?.Level} {loaded?.Hero.Name} {loaded?.Hero.Visited.Count} {loaded?.Difficulty} {loaded?.Scores["forest"]} {(loaded?.Bag[0] as Potion)?.Heals} {(loaded?.Bag[1] as Key)?.Door}");
    // A slot as an earlier version wrote it: indented, without the properties added since.
    File.WriteAllText(Path.Combine(dir, "old.json"), "{\\n  \\"Level\\": 7,\\n  \\"Hero\\": {\\n    \\"Name\\": \\"Pip\\"\\n  }\\n}");
    var old = ${load('"old"')};
    Console.WriteLine($"old: {old?.Level} {old?.Hero.Name} {old?.Bag.Count}");
    File.WriteAllText(Path.Combine(dir, "broken.json"), "{ \\"Level\\": 3, \\"Hero\\"");
    Console.WriteLine("corrupt: " + (${load('"broken"')} is null) + " " + File.Exists(Path.Combine(dir, "broken.corrupt.json")) + " " + store.Exists("broken"));
    // Achievements keep their state in the same store: the engine's own saved type, which the game declares nowhere.
    Achievement[] catalog = [new("first", "First", "Once."), new("ten", "Ten", "Ten times.", Goal: 10)];
    var tracker = new AchievementTracker(catalog, store);
    tracker.AddProgress("ten", 3);
    Console.WriteLine("achievements: " + tracker.Unlock("first") + " " + tracker.Flush() + " " + File.ReadAllText(Path.Combine(dir, "achievements.json")).ReplaceLineEndings("").Replace(" ", ""));
    var again = new AchievementTracker(catalog, store);
    Console.WriteLine($"achievements again: {again.IsUnlocked("first")} {again.Progress("ten")} {again.Unlock("first")}");
    File.WriteAllText(Path.Combine(dir, "achievements.json"), "{\\"Entries\\":{\\"ten\\":{\\"Unlocked\\":false,\\"Progress\\":7,\\"When\\":1},\\"gone\\":{\\"Progress\\":2}}}");
    var earlier = new AchievementTracker(catalog, store);
    Console.WriteLine($"achievements earlier: {earlier.IsUnlocked("first")} {earlier.Progress("ten")} {earlier.AddProgress("ten", 3)} {File.ReadAllText(Path.Combine(dir, "achievements.json")).Contains("gone")}");
    return 0;
}
catch (Exception e)
{
    Console.WriteLine($"FAILED: {e.GetType().Name}: {e.Message}");
    return 1;
}
finally
{
    if (Directory.Exists(dir)) Directory.Delete(dir, true);
}
`;
const expected = ['missing: True', 'save: True', 'load: 3 Ada 2 Hard 1200 5 cellar', 'old: 7 Pip 0', 'corrupt: True True False',
  'achievements: True True {"Version":1,"Entries":{"ten":{"Progress":3,"Unlocked":false},"first":{"Progress":1,"Unlocked":true}}}',
  'achievements again: True 3 False', 'achievements earlier: False 7 True True'];

// The windowed game: what the smallest game does (an image made in code, an atlas, a sound, a scene that moves a
// sprite by input, text), so the NativeAOT program opens a window, loads, updates, draws and captures frames.
const windowed = `using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Desktop;
using MicroEngine.Scenes;

return DesktopLauncher.Run(new WindowedGame(), args);

internal sealed class WindowedGame : Game
{
    public override GameSettings Settings { get; } = new("Windowed") { Background = Color.Hex("1d2b53") };
    public SpriteAtlas Atlas { get; private set; } = null!;

    protected override void Load()
    {
        var star = PixelImage.FromMap(Art.Palette("o=#000000", "y=#ffec27"), "..o..", ".oyo.", "oyyyo", ".oyo.", "..o..");
        Atlas = new SpriteAtlasBuilder().Add("star", star, new Vector2(2.5f, 2.5f)).Build(Graphics);
        Audio.AddSound("ding", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 880, Sustain = 0.05f, Decay = 0.2f }));
        Scenes.Push(new WindowedScene(this));
    }

    protected override void Unload() => Atlas.Dispose();
}

internal sealed class WindowedScene(WindowedGame game) : Scene
{
    private Vector2 _position = new(160, 100);

    public override void Update(float dt)
    {
        _position += new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down")) * 90 * dt;
        if (Input.Pressed("confirm")) Audio.Play("ding");
    }

    public override void Draw(IRenderer r)
    {
        Font.DrawCentered(r, "Hello, NativeAOT!", new Vector2(160, 40), Color.Hex("fff1e8"), 2, Color.Hex("000000"));
        r.Draw(game.Atlas["star"], _position);
    }
}
`;
const windowedScript = ['10 capture before.png', '20 hold right 30', '60 press confirm', '64 capture after.png', '70 quit', ''].join('\n');

function folder(name, packageId, files) {
  const dir = path.join(work, name);
  fs.mkdirSync(dir);
  fs.writeFileSync(path.join(dir, `${name}.csproj`), project(packageId));
  fs.writeFileSync(path.join(dir, 'nuget.config'), nugetConfig);
  for (const [file, text] of Object.entries(files)) fs.writeFileSync(path.join(dir, file), text);
  return dir;
}
const game = (name, save, load) => folder(name, 'MicroEngine', { 'Model.cs': model, 'Program.cs': program(save, load) });
const published = (dir, name, out, extra) => {
  const result = run('dotnet', ['publish', `${name}.csproj`, '-c', 'Release', '-r', rid, '-o', path.join(dir, out), '--disable-build-servers', ...extra], dir);
  return { ...result, found: diagnostics(result.text), program: path.join(dir, out, name + exe) };
};
const sameLines = (text, lines) => JSON.stringify(text.trim().split(/\r?\n/)) === JSON.stringify(lines);
// A NativeAOT publish: the executable is there and the game's managed assembly is not, so what runs is native code.
const native = (result, dir, name) => fs.existsSync(result.program) && !fs.existsSync(path.join(dir, 'out-aot', `${name}.dll`));
const linkErrors = result => result.code === 0 ? '' : result.text.trim().split(/\r?\n/).filter(l => /error/.test(l)).slice(-2).join(' | ').slice(0, 400);
// How many different rows a PNG has (0 when it is not one): a frame with text and a sprite on it has many, an empty
// or single-colour frame has one.
function pictureRows(file) {
  if (!fs.existsSync(file)) return 0;
  const png = fs.readFileSync(file);
  if (png.length < 33 || png.readUInt32BE(0) !== 0x89504e47) return 0;
  const height = png.readUInt32BE(20), data = [];
  for (let at = 8; at + 8 <= png.length; at += 12 + png.readUInt32BE(at)) {
    if (png.toString('latin1', at + 4, at + 8) === 'IDAT') data.push(png.subarray(at + 8, at + 8 + png.readUInt32BE(at)));
  }
  let raw;
  try { raw = zlib.inflateSync(Buffer.concat(data)); } catch { return 0; }
  const stride = raw.length / height, rows = new Set();
  if (!Number.isInteger(stride)) return 0;
  for (let y = 0; y < height; y++) rows.add(raw.toString('latin1', y * stride, (y + 1) * stride));
  return rows.size;
}

console.log(`trimmed saves: MicroEngine ${version} from ${feed}, ${rid}, .NET SDK ${run('dotnet', ['--version'], work).text.trim()}, in ${work}`);

// 1. The typed game: trimmed, every warning an error, and run.
const typed = game('typed', (slot, data) => `store.Save(${slot}, ${data}, GameSaves.Default.Progress)`, slot => `store.Load(${slot}, GameSaves.Default.Progress)`);
const trimmed = published(typed, 'typed', 'out-trimmed', ['-p:PublishTrimmed=true', '--self-contained', '-warnaserror']);
if (check('typed game: trimmed publish without warnings', trimmed.code === 0 && trimmed.found.length === 0, trimmed.found.join(' | ') || (trimmed.code === 0 ? '' : trimmed.text.slice(-600)))) {
  const ran = run(trimmed.program, [], typed);
  check('typed game: the trimmed program saves and loads its own types and its achievements', ran.code === 0 && sameLines(ran.text, expected), ran.text.trim().replace(/\r?\n/g, ' | '));
}

// 2. The plain game: the analyzers name the two reflection calls in the game's file, and the trimmed program fails there.
const plain = game('plain', (slot, data) => `store.Save(${slot}, ${data})`, slot => `store.Load<Progress>(${slot})`);
const analyzed = run('dotnet', ['build', 'plain.csproj', '-c', 'Release', '-p:PublishAot=true', '--disable-build-servers'], plain);
const inProgram = diagnostics(analyzed.text).filter(line => /Program\.cs\(\d+,\d+\): warning IL(2026|3050)/.test(line));
check('plain game: the build names the reflection calls in Program.cs (IL2026 and IL3050)',
  analyzed.code === 0 && inProgram.some(l => l.includes('IL2026')) && inProgram.some(l => l.includes('IL3050')) && inProgram.every(l => /SaveStore\.(Save|Load)/.test(l)),
  `${inProgram.length} diagnostics`);
const plainTrimmed = published(plain, 'plain', 'out-trimmed', ['-p:PublishTrimmed=true', '--self-contained']);
if (check('plain game: trimmed publish succeeds with warnings only from the game', plainTrimmed.code === 0 && !plainTrimmed.found.some(l => /IL2104|IL3053/.test(l)), plainTrimmed.found.filter(l => /IL2104|IL3053/.test(l)).join(' | '))) {
  const ran = run(plainTrimmed.program, [], plain);
  check('plain game: the trimmed program reports that reflection is disabled', ran.code === 1 && ran.text.includes('FAILED: InvalidOperationException'), ran.text.trim().replace(/\r?\n/g, ' | ').slice(0, 200));
}

// 3. NativeAOT, where this machine can link native code.
if (args.includes('--aot')) {
  const aot = published(typed, 'typed', 'out-aot', ['-p:PublishAot=true', '-warnaserror']);
  const analysis = aot.found.filter(l => /IL\d+/.test(l));
  check('typed game: NativeAOT compilation reports no trim or AOT warnings', analysis.length === 0, analysis.join(' | '));
  if (check('typed game: NativeAOT publish links a native executable', aot.code === 0 && native(aot, typed, 'typed'), linkErrors(aot))) {
    const ran = run(aot.program, [], typed);
    check('typed game: the NativeAOT program saves and loads its own types and its achievements', ran.code === 0 && sameLines(ran.text, expected), ran.text.trim().replace(/\r?\n/g, ' | '));
  }

  // 4. A game with a window, as a NativeAOT executable, played by a script.
  const window = folder('windowed', 'MicroEngine.Desktop', { 'Program.cs': windowed, 'play.script': windowedScript });
  const windowAot = published(window, 'windowed', 'out-aot', ['-p:PublishAot=true', '-warnaserror']);
  const windowAnalysis = windowAot.found.filter(l => /IL\d+/.test(l));
  check('windowed game: NativeAOT compilation reports no trim or AOT warnings', windowAnalysis.length === 0, windowAnalysis.join(' | '));
  if (check('windowed game: NativeAOT publish links a native executable', windowAot.code === 0 && native(windowAot, window, 'windowed'), linkErrors(windowAot))) {
    const captures = path.join(window, 'captures');
    const ran = run(windowAot.program, ['--platform', 'sdl-software', '--hidden', '--mute', '--script', path.join(window, 'play.script'), '--captures', captures], window);
    const before = path.join(captures, 'before.png'), after = path.join(captures, 'after.png');
    const rows = pictureRows(after);
    const moved = rows > 0 && pictureRows(before) > 0 && !fs.readFileSync(before).equals(fs.readFileSync(after));
    check('windowed game: the NativeAOT program plays its script to the end', ran.code === 0 && /done: 70 updates/.test(ran.text), ran.text.trim().split(/\r?\n/).slice(-2).join(' | ').slice(0, 300));
    check('windowed game: its captures hold a drawn frame, and scripted input moved the sprite', rows >= 8 && moved, `${rows} different rows${moved ? '' : ', captures equal or missing'}`);
  }
}

if (args.includes('--keep')) console.log(`kept: ${work}`);
else {
  // The folder is scratch: failing to remove it (a build process still holding a file on Windows) is reported, and
  // never changes the result of the checks.
  try { fs.rmSync(work, { recursive: true, force: true, maxRetries: 5, retryDelay: 500 }); }
  catch (error) { console.log(`left behind (${error.code}): ${work}`); }
}
console.log(failed ? `${failed} check(s) failed` : 'all checks passed');
process.exit(failed ? 1 : 0);
