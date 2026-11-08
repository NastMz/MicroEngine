# MicroEngine.Text.Shaping

Shaped text for [MicroEngine](https://github.com/NastMz/MicroEngine): `ShapedFont` draws scripts whose letters change
shape with their neighbours or run right to left (Arabic, Hebrew, Indic scripts) with HarfBuzz, on every platform the
engine runs on, browsers included. `ShapedFont.At(size)` is an `IFont`, so widgets, layout and wrapping use it as
they use a scalable font.

```csharp
var font = ShapedFont.FromBytes(Graphics, Assets.Bytes("fonts/Inter-Regular.ttf"))
    .AddFallback(Assets.Bytes("fonts/NotoSansArabic-Regular.ttf"));
font.At(32).Draw(renderer, "مرحبا", new Vector2(24, 24), Color.White);
```

HarfBuzz is a native library, which is why shaping is a package of its own: games that only need Latin text do not
carry it. Native HarfBuzz comes from the `HarfBuzzSharp` packages.

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed; HarfBuzz is
under the Old MIT licence and HarfBuzzSharp under MIT.
