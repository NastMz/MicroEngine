# Localization

A game's texts live in string tables, one per language, under `Content/`. `Game.Localization` loads them, gives the
text of a key in the language in use, and changes the language at any time: widgets, action prompts and per-language
files follow it without code of the game's own. This page covers the tables, messages with values and plurals,
choosing the language, widgets, prompts, fonts, files per language, and checking tables before a player sees them.

| To do this | Use | Section |
| --- | --- | --- |
| Write the texts of each language | `en.strings.json`, or one `.strings.csv` | [String tables](#string-tables) |
| Show a text, with values and plurals | `Localization["key"]`, `Localization.Format(key, ...)` | [Messages](#messages) |
| Choose and change the language | `Localization.Language`, `TrySetLanguage` | [The language in use](#the-language-in-use) |
| Have menus follow the language | `new Label(new LocalizedText("key"))` | [Widgets](#widgets) |
| Name keys and buttons in the language | `prompt.Space` in the table | [Prompts](#prompts) |
| Use the right font for a language | `@fonts`, `Localization.Current.Fonts` | [Fonts](#fonts) |
| Load an image or a sound per language | `Localization.Path("ui/title.png")` | [Files per language](#files-per-language) |
| Find missing translations in CI | `microengine content check` | [Checking tables](#checking-tables) |

## A game in several languages

The whole of it, from the tour's localization example: four tables, a screen whose texts follow the language, a count
with each language's plural forms, a hint that names keys, fonts per language and an image with a variant.

<!-- code: samples/Examples/Examples/LocalizationExample.cs#localization -->
```csharp
// A widget made with a key shows that key's text in the language in use; the others get theirs below.
public Label Title { get; } = new(new LocalizedText("loc.title"));
public Label Language { get; } = new();
public Label Gems { get; } = new();
public Label Hint { get; } = new() { Wrap = true, Width = 200 };
public Button Next { get; private set; } = null!;

public override void Enter()
{
    var text = Game.Localization;
    text.Load("text");                                                    // Content/text/<language>.strings.json
    text.TrySetLanguage(GameSaves.LoadSettings(Game.Saves).Language);     // the saved choice, else the system's language
    text.Changed += UseFonts;
    UseFonts();

    Next = new Button(new LocalizedText("loc.next"), NextLanguage);
    Widgets.Content = new Panel(new Stack { Width = 200 }.Add(Title).Add(Language).Add(Gems).Add(Hint).Add(Next));
}

private void NextLanguage()
{
    var text = Game.Localization;
    int at = 0;
    while (text.Languages[at].Code != text.Language) at++;
    text.Language = text.Languages[(at + 1) % text.Languages.Count].Code;

    var settings = GameSaves.LoadSettings(Game.Saves);
    settings.Language = text.Language;
    GameSaves.SaveSettings(Game.Saves, settings);
}

/// <summary>The fonts the language's table names: the first face, then those that fill in the letters it lacks.</summary>
private void UseFonts()
{
    var language = Game.Localization.Current;
    if (!_fonts.TryGetValue(language.Code, out var font))
    {
        font = ShapedFont.FromBytes(Game.Graphics, Game.Assets.Bytes(language.Fonts[0]));
        foreach (var more in language.Fonts.Skip(1)) font.AddFallback(Game.Assets.Bytes(more));
        _fonts[language.Code] = font;
    }
    Widgets.Font = font.At(12);
    Language.Localized = new("loc.language", ("name", language.Name));
}

public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    if (Input.Pressed("action")) _gems++;

    // A value that changes is given again: the plural form is chosen for the number, in the language in use.
    if (_gems != _gemsShown) Gems.Localized = new("loc.gems", ("count", _gemsShown = _gems));

    // Text written by hand: a value of the game's own ({keys}), then the control that presses {action}. Both change
    // with the language and with the device in the player's hands.
    var text = Game.Localization;
    int version = HashCode.Combine(text.Version, Game.Prompts.Version);
    if (version == _hintOf) return;
    _hintOf = version;
    Hint.Text = Game.Prompts.Format(text.Format("loc.hint", ("keys", Game.Prompts.TextAll("confirm"))));
}

/// <summary>The image for the language in use: <c>next.ar.png</c> in Arabic, <c>next.png</c> in the others.</summary>
public string ImagePath => Game.Localization.Path("localization/next.png");

public override void DrawUi(IRenderer ui)
{
    var frame = Game.Views.CanvasToUi(Vector2.Zero);
    var end = Game.Views.CanvasToUi(new Vector2(Game.Settings.VirtualWidth, Game.Settings.VirtualHeight));
    var arrow = Game.Assets.Texture(ImagePath);
    ui.Draw(arrow, new Vector2(MathF.Round((frame.X + end.X - arrow.Width) / 2), end.Y - arrow.Height - 6));
}
```

Source: [`samples/Examples/Examples/LocalizationExample.cs`](../../samples/Examples/Examples/LocalizationExample.cs),
with its tables in [`samples/Examples/Content/text`](../../samples/Examples/Content/text).

## String tables

A table is a file whose name ends in `.strings.json` or `.strings.csv`, anywhere under `Content/`. Nothing is
compiled: the files load as they are.

**One JSON file per language**, named by its language tag (`en.strings.json`, `pt-BR.strings.json`,
`zh-Hans.strings.json`):

```json
{
  "@name": "Español",
  "@fonts": ["fonts/Inter-Regular.ttf"],

  "menu": { "play": "Jugar", "quit": "Salir" },
  "hud.gems": "{count, plural, one {# gema} other {# gemas}}",
  "hint.jump": "Pulsa {jump} para saltar"
}
```

Every value is a text. Objects group keys, joined with a dot (`menu.play`); a dotted key written directly
(`"hud.gems"`) is the same thing. Comments and trailing commas are allowed. Keys that start with `@` describe the
language:

| Key | Meaning |
| --- | --- |
| `@name` | The language's own name, for a language menu ("Español", "日本語"). Without it, the tag |
| `@fonts` | The font files of the language, the main face first, then the faces that fill in what it lacks ([Fonts](#fonts)) |
| `@direction` | `rtl` or `ltr`. Without it, Arabic, Hebrew, Persian, Urdu and the other right-to-left languages are recognised by their tag |

**One CSV file for every language**, as a spreadsheet exports it: the first row names the key column and a language
tag per column, each other row is a key and its text in each language.

```text
key,en,es
@name,English,Español
menu.play,Play,Jugar
hud.gems,"{count, plural, one {# gem} other {# gems}}","{count, plural, one {# gema} other {# gemas}}"
```

Commas separate cells; a cell that holds commas, quotes or line breaks goes between quotes, with two quotes for one.
An empty cell is a text the language does not have, and a row whose first cell starts with `#` is a comment.

`Localization.Load("text")` loads every table of a folder; `Localization.Load("text/en.strings.json")` loads one file.
Tables add up: a second folder, or a level's own texts loaded later, adds to each language, and a key given again
takes the newer text. A table that cannot be read fails with an `AssetLoadException` naming the file and the line.
Tables are read through the game's content source, so they also load from a zip archive or a phone's package
([Assets](assets.md#handles-loaders-and-content-sources)).

## Messages

A text is a message in the ICU MessageFormat syntax, which translation tools and platforms read and write.

| In the text | Shows |
| --- | --- |
| `{name}` | the value given under that name |
| `{count, plural, =0 {none} one {# gem} other {# gems}}` | the branch for the number: an exact number (`=0`) first, then the language's plural form, else `other`. `#` is the number |
| `{who, select, hero {the hero} other {{who}}}` | the branch whose name is the value, else `other` |
| `'{'`, `'}'` | a brace as text. Two apostrophes are one; any other apostrophe is itself ("don't") |

```csharp
string play = Localization["menu.play"];                                   // "Jugar"
string gems = Localization.Format("hud.gems", ("count", 3));               // "3 gemas"
string line = Localization.Format("found", ("who", "Ana"), ("count", 1));  // values by name, in any order
```

Values are a name with a string or a number. A value the call does not give stays as written, so `{jump}` in
`"Pulsa {jump} para saltar"` reaches the [action prompts](#prompts). A text without values is returned as stored:
asking for it every frame allocates nothing.

**Plural forms** come from rules built into the engine, chosen by the language tag. English has two forms (`one`,
`other`), Russian four (`one`, `few`, `many`, `other`), Arabic six, Japanese one. Nothing is read from the operating
system, so plurals work the same in trimmed, NativeAOT, phone and browser builds. Every plural needs `other`; a form
the message does not give takes `other`.

```json
"hud.gems": "{count, plural, one {# самоцвет} few {# самоцвета} many {# самоцветов} other {# самоцвета}}"
```

The engine has rules for about 65 languages (see [`PluralRules`](../api/MicroEngine.Localization.PluralRules.md)). A
language it does not know counts as English does until the game gives its rule:

```csharp
PluralRules.Register("ga", n => n == 1 ? PluralCategory.One : n == 2 ? PluralCategory.Two : PluralCategory.Other,
    PluralCategory.One, PluralCategory.Two);
```

A key no table has shows the key itself, and a key the language lacks shows the fallback language's text; each is
written once to the log (`localization: no text for 'menu.options' in 'es'...`) and listed in
`Localization.MissingKeys`.

## The language in use

| Member | Gives |
| --- | --- |
| `Language` | the tag of the language in use; setting it changes every text. A tag no table matches is refused |
| `TrySetLanguage(tag)` | sets it when a table matches, and says whether one did: for a saved choice |
| `Languages` | the game's languages with their `Code`, `Name`, `RightToLeft` and `Fonts`, for a language menu |
| `Current`, `RightToLeft` | the language in use, and whether it reads right to left |
| `FallbackLanguage` | the language whose text fills in missing keys (`en`; without an English table, the first one loaded) |
| `SystemLanguages` | the player's languages on this system, most wanted first |
| `Changed`, `Version` | an event and a number that move whenever texts may have changed |

Until the game chooses, the language is the first of the system's languages the game has text in, else the fallback
language. A region takes its language's table (`es-AR` uses `es`), and a region's own table (`es-MX.strings.json`)
needs only the texts that differ. A scripted or headless run never reads the system's language, so tests and captures
are the same on every machine.

The player's choice is a setting like volume, saved with the game's other settings
([Saving data](persistence.md#settings)):

```csharp
protected override void Load()
{
    Localization.Load("text");
    Localization.TrySetLanguage(GameSaves.LoadSettings(Saves).Language);   // null or a language since removed: the default stays
}

// In the options screen:
foreach (var language in Game.Localization.Languages)
    menu.Add(new Button(language.Name, () =>
    {
        Game.Localization.Language = language.Code;
        settings.Language = language.Code;
        GameSaves.SaveSettings(Game.Saves, settings);
    }));
```

The language is part of the per-step state hash (section `engine.localization`) once a game has loaded a table.

## Widgets

A `Label`, a `Button` and a text field's placeholder take a `LocalizedText`, a key with its values, instead of a
string. They show its text in the language in use and lay themselves out again when the language changes:

```csharp
var title = new Label(new LocalizedText("menu.title"));
var play = new Button(new LocalizedText("menu.play"), Play);
var gems = new Label();
gems.Localized = new("hud.gems", ("count", 3));    // set it again when the number changes
name.LocalizedPlaceholder = new("name.hint");
```

Setting `Localized` to the same key and values costs nothing; build a new one when a value changes, not every frame.
`Text` gives the text shown as of the last layout, and setting `Text` makes the widget plain text again. In a
right-to-left language a label's lines start from its right edge. A `UiRoot` made by hand takes the texts from its
`Localization` property.

Text a game draws itself asks for it when drawing, or when `Localization.Version` moves:

```csharp
Font.Draw(r, Game.Localization["hud.paused"], position, Color.White);
```

## Prompts

`Game.Prompts` names the control that presses an action ([Input](input.md#action-prompts)). Put the action between
braces in the text and format the result:

```csharp
string hint = Game.Prompts.Format(Game.Localization["hint.jump"]);   // "Pulsa Espacio para saltar"
```

The engine's names of keys and controls are English ("Space", "Enter", "Shift", "D-pad", "Stick", "Start", "LMB",
"Wheel"). A table translates one with the key `prompt.` followed by the English name:

```json
"prompt.Space": "Espacio",
"prompt.Enter": "Intro",
"prompt.D-pad": "Cruceta",
"prompt.Mouse": "Ratón"
```

A language gives only the names it has another word for. Names of several words are translated whole
(`"prompt.Caps Lock"`) or word by word (`"Mouse 4"` uses `prompt.Mouse`). Letter keys follow the player's keyboard
layout and are never translated. `Prompts.Version` moves with the language, like the device in the player's hands.

## Fonts

The built-in pixel font has the ASCII letters only; a language with accents or another alphabet needs a
[scalable font](text.md#scalable-fonts-ttf-and-otf), and Arabic, Hebrew and Indic scripts need the
[shaping package](text.md#shaped-text-arabic-hebrew-and-indic-scripts) as well. A table names its language's fonts in
`@fonts`, and the game builds the font from them when the language changes:

```csharp
private readonly Dictionary<string, ScalableFont> _fonts = [];   // one per language, disposed when the scene ends

void UseFonts()
{
    var language = Game.Localization.Current;
    if (!_fonts.TryGetValue(language.Code, out var font))
    {
        font = ScalableFont.FromBytes(Game.Graphics, Assets.Bytes(language.Fonts[0]));
        foreach (var more in language.Fonts.Skip(1)) font.AddFallback(Assets.Bytes(more));
        font.Prepare(Game.Localization.Characters(), 14);    // every character the language shows, rasterized now
        _fonts[language.Code] = font;
    }
    Widgets.Font = font.At(14);
}
```

Call it once and from `Localization.Changed`. The example at the top of this page does the same with `ShapedFont`,
for Arabic.

`Localization.Characters()` lists every character the texts of a language can show: hand it to `Prepare` so no glyph
is rasterized during play, which matters for Chinese, Japanese and Korean, whose texts use hundreds of characters.
`FontCoverage.Read(bytes).Missing(text)` says which characters a font file lacks; the
[content check](#checking-tables) does it for every table that names its fonts. A language menu shows each language's
name in that language's own font, since the font in use may lack the others' letters.

## Files per language

An image with words in it, a voice line or a font can have a variant per language: a file beside the original with
the language tag before its extension.

```text
Content/ui/title.png            every language without a variant
Content/ui/title.es.png         Spanish
Content/ui/title.pt-BR.png      Brazilian Portuguese
```

`Localization.Path("ui/title.png")` returns the variant for the language in use when the content has one (a region
without its own takes its language's), else the path itself. Pass the result to any loader:

```csharp
r.Draw(Assets.Texture(Game.Localization.Path("ui/title.png")), position);
Audio.AddSoundFile("welcome", Game.Localization.Path("voice/welcome.wav"));
```

The answer is remembered per language, so asking while drawing allocates nothing. `Localization.Variants(path)` lists
the file and its variants for the game's languages, to [preload](assets.md#loading-screens-preload) them so a
language change loads nothing during play.

## Checking tables

`microengine content check` reads the string tables with the rest of the content and fails on what a player would
see:

```text
error: text/es.strings.json: 'menu.options': no text in 'es', though 'en' has it: the player sees another language's text. Add it to the table of 'es'.
error: text/es.strings.json: 'found': the text names {quien}, which the text of 'en' does not (it names {who}): the player would see {quien} as written. Correct the name.
warning: text/ru.strings.json: 'hud.gems': the plural of {count} gives no text for few, many, which 'ru' uses: those numbers take the 'other' text. Add those branches.
warning: text/ja.strings.json: the texts of 'ja' show 212 characters its fonts (fonts/Inter-Regular.ttf) lack, drawn as '?': ...
```

| Finding | Level |
| --- | --- |
| A table that cannot be read; a message that cannot be read | error |
| A key a language lacks (a region counts its language's texts; `prompt.` names are optional) | error |
| A value a translation names that the reference language's text does not | error |
| A font in `@fonts` that does not exist; a variant whose language tag is spelled with another letter case | error |
| A value a translation does not use; a plural without the forms its language uses, or with forms it never chooses | warning |
| Characters the language's fonts lack; a variant without its original file; a language without a plural rule | warning |

It exits with 1 on errors, so it runs in CI as it is ([the tool](../guide/cli.md#content)). The same comparison is
available to a game's own tests: `StringTableChecks.Check(tables)`.

## On every platform

Tables, messages and plural rules are managed code with no culture data, the same in trimmed and NativeAOT builds, on
phones and in browsers. The system's language comes from the platform where it says, else from the runtime's culture;
packaged builds, browsers and phones carry no culture data, and there the game starts in its fallback language until
the player chooses.

## Limits

- Numbers are written with plain digits and a point, without grouping; dates, times and currencies are not
  formatted. Format them in the game and pass them as text.
- The message syntax leaves out `selectordinal` ("1st", "2nd"), `offset:` and the date, time, duration and spell-out
  formats; a text using them is refused when it is read.
- Plural forms follow Unicode's rules for whole numbers; fractions are approximated, and French, Italian,
  Portuguese, Spanish and Catalan have no separate form for whole millions.
- Right to left: labels start from the right, and shaped fonts lay each line out. Layouts are not mirrored (a row of
  buttons keeps its order), text fields align to the left, and prompt glyphs drawn by `PromptText` use the built-in
  font.
- Chinese, Japanese and Korean lines have no spaces to wrap at: a wrapped label breaks them between characters, with
  no rule against a line starting with punctuation.
- Every language of a loaded table stays in memory. A changed table is picked up the next time the game starts.
- Gettext `.po`, XLIFF, Fluent and `.resx` files are not read: export JSON or CSV from the translation tool.

## Common mistakes

- **A key on screen instead of text.** No table has the key: the log names it, and `microengine content check`
  lists every such key.
- **`?` instead of letters.** The font lacks them: use a font that has the language's characters ([Fonts](#fonts)).
- **`{count}` shown as written.** The call did not give that value, or the translation misspells its name.
- **A widget that stays in the old language.** It holds a plain string: give it a `LocalizedText`, or set its text
  again when `Localization.Changed` is raised.

## Related

- [Text](text.md): fonts, shaping and right-to-left text
- [UI widgets](ui.md), [Input](input.md#action-prompts), [Assets](assets.md), [Saving data](persistence.md#settings)
- API: [`GameLocalization`](../api/MicroEngine.Localization.GameLocalization.md),
  [`Localizer`](../api/MicroEngine.Localization.Localizer.md), [`Message`](../api/MicroEngine.Localization.Message.md)
