# Class AssetPreloadList

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What a preload is asked to load ([Preload](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Preload_System_String___)): the loading calls of
[AssetCache](MicroEngine.Core.AssetCache.md) with the same parameters, recorded instead of run.

```csharp
public sealed class AssetPreloadList
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

## Methods

### <a id="MicroEngine_Core_AssetPreloadList_Asset_System_String_System_String_System_Object_System_Int64_"></a> Asset\(string, string, object?, long\)

An asset of any kind by its loader's kind, as <code>Assets.Scope.Load(kind, path, options, variant)</code> asks for it: the kinds a game or a package registers.

```csharp
public AssetPreloadList Asset(string kind, string path, object? options = null, long variant = 0)
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [object](https://learn.microsoft.com/dotnet/api/system.object)?

`variant` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_Atlas_System_String_MicroEngine_Graphics_AtlasOptions_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> Atlas\(string, AtlasOptions?, TextureFilter?\)

An atlas or sheet file ([Atlas](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Atlas_System_String_MicroEngine_Graphics_AtlasOptions_System_Nullable_MicroEngine_Graphics_TextureFilter__)).

```csharp
public AssetPreloadList Atlas(string path, AtlasOptions? options = null, TextureFilter? filter = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_AtlasFromFolder_System_String_System_Numerics_Vector2_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> AtlasFromFolder\(string, Vector2, TextureFilter?\)

A folder of images packed into one atlas ([AtlasFromFolder](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_AtlasFromFolder_System_String_System_Numerics_Vector2_System_Nullable_MicroEngine_Graphics_TextureFilter__)).

```csharp
public AssetPreloadList AtlasFromFolder(string folder, Vector2 pivot = default, TextureFilter? filter = null)
```

#### Parameters

`folder` [string](https://learn.microsoft.com/dotnet/api/system.string)

`pivot` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_Files_System_String___"></a> Files\(params string\[\]\)

Files by path alone, each as the kind its extension chooses, with default options (see [Preload](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Preload_System_String___)).

```csharp
public AssetPreloadList Files(params string[] paths)
```

#### Parameters

`paths` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No loader reads a file's extension.

### <a id="MicroEngine_Core_AssetPreloadList_Font_System_String_MicroEngine_Graphics_FontOptions_"></a> Font\(string, FontOptions?\)

A TTF or OTF font ([Font](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Font_System_String_MicroEngine_Graphics_FontOptions_)).

```csharp
public AssetPreloadList Font(string path, FontOptions? options = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_Material_System_String_MicroEngine_Graphics_MaterialFallback_"></a> Material\(string, MaterialFallback\)

A material from a compiled fragment shader ([Material](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Material_System_String_MicroEngine_Graphics_MaterialFallback_)).

```csharp
public AssetPreloadList Material(string path, MaterialFallback fallback = default)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`fallback` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_Music_System_String_"></a> Music\(string\)

A music file ([Music](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Music_System_String_)).

```csharp
public AssetPreloadList Music(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_RenderTarget_System_String_System_Int32_System_Int32_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> RenderTarget\(string, int, int, TextureFilter?\)

A render target ([RenderTarget](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_RenderTarget_System_String_System_Int32_System_Int32_System_Nullable_MicroEngine_Graphics_TextureFilter__)).

```csharp
public AssetPreloadList RenderTarget(string name, int width, int height, TextureFilter? filter = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_Shader_System_String_"></a> Shader\(string\)

A compiled shader file ([Shader](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Shader_System_String_)).

```csharp
public AssetPreloadList Shader(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_Sound_System_String_"></a> Sound\(string\)

A PCM WAV file as a sound ([Sound](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Sound_System_String_)).

```csharp
public AssetPreloadList Sound(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

### <a id="MicroEngine_Core_AssetPreloadList_Texture_System_String_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> Texture\(string, TextureFilter?\)

An image as a texture ([Texture](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Texture_System_String_System_Nullable_MicroEngine_Graphics_TextureFilter__)).

```csharp
public AssetPreloadList Texture(string path, TextureFilter? filter = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

#### Returns

 [AssetPreloadList](MicroEngine.Core.AssetPreloadList.md)

</div>
