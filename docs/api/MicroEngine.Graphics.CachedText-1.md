# Struct CachedText<TKey\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A text built from a value and built again only when the value changes: for text that cannot be written as an
interpolated string where it is drawn (a localized message with a number in it, a wrapped paragraph, a text a
widget keeps). While the value stays the same, [Get](MicroEngine.Graphics.md) returns the same string and allocates nothing; a
change costs one call of the format.

```csharp
public struct CachedText<TKey> where TKey : IEquatable<TKey>
```

#### Type Parameters

`TKey` 

What the text is built from: a number, or a tuple of everything that changes it (<code>(score, Text.Version)</code>).

## Examples

```csharp
private CachedText<(int Coins, int Language)> _coins;

// in Draw: built again when the count or the language changes
string line = _coins.Get((coins, Text.Version), Text, static (text, key) => text.Format("hud.coins", ("count", key.Coins)));
Font.Draw(r, line, new Vector2(8, 8), Color.White);
```

## Methods

### <a id="MicroEngine_Graphics_CachedText_1_Clear"></a> Clear\(\)

Forgets the text, so the next [Get](MicroEngine.Graphics.md) builds it.

```csharp
public void Clear()
```

### <a id="MicroEngine_Graphics_CachedText_1_Get__0_System_Func__0_System_String__"></a> Get\(TKey, Func<TKey, string\>\)

The text of <code class="paramref">key</code>: the one kept when the key is the one it was built from, else <code class="paramref">format</code>'s.

```csharp
public string Get(TKey key, Func<TKey, string> format)
```

#### Parameters

`key` TKey

What the text is built from.

`format` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TKey, [string](https://learn.microsoft.com/dotnet/api/system.string)\>

Builds the text; pass a <code>static</code> lambda so the call allocates nothing.

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_CachedText_1_Get__1__0___0_System_Func___0__0_System_String__"></a> Get<TState\>\(TKey, TState, Func<TState, TKey, string\>\)

As [Get](MicroEngine.Graphics.md), with something the format needs that is not part of the key (the localizer, a font).

```csharp
public string Get<TState>(TKey key, TState state, Func<TState, TKey, string> format)
```

#### Parameters

`key` TKey

`state` TState

`format` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TState, TKey, [string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Type Parameters

`TState`

</div>
