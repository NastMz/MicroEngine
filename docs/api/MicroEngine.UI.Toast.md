# Class Toast

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

One banner of [Toasts](MicroEngine.UI.Toasts.md): a heading, a title, an optional description and an optional icon.

```csharp
public sealed record Toast : IEquatable<Toast>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Toast](MicroEngine.UI.Toast.md)

#### Implements

[IEquatable<Toast\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_UI_Toast__ctor_System_String_System_String_System_String_"></a> Toast\(string, string, string\)

One banner of [Toasts](MicroEngine.UI.Toasts.md): a heading, a title, an optional description and an optional icon.

```csharp
public Toast(string Heading, string Title, string Description = "")
```

#### Parameters

`Heading` [string](https://learn.microsoft.com/dotnet/api/system.string)

The first line, in the accent colour ("ACHIEVEMENT UNLOCKED", "SAVED").

`Title` [string](https://learn.microsoft.com/dotnet/api/system.string)

The second line.

`Description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Up to two wrapped lines in the muted colour; empty draws none.

## Properties

### <a id="MicroEngine_UI_Toast_Description"></a> Description

Up to two wrapped lines in the muted colour; empty draws none.

```csharp
public string Description { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_Toast_Heading"></a> Heading

The first line, in the accent colour ("ACHIEVEMENT UNLOCKED", "SAVED").

```csharp
public string Heading { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_Toast_Icon"></a> Icon

A sprite drawn left of the text, or null.

```csharp
public Sprite? Icon { get; init; }
```

#### Property Value

 Sprite?

### <a id="MicroEngine_UI_Toast_Title"></a> Title

The second line.

```csharp
public string Title { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_UI_Toast__Clone__"></a> <Clone\>$\(\)

```csharp
public Toast <Clone>$()
```

#### Returns

 [Toast](MicroEngine.UI.Toast.md)

### <a id="MicroEngine_UI_Toast_Deconstruct_System_String__System_String__System_String__"></a> Deconstruct\(out string, out string, out string\)

```csharp
public void Deconstruct(out string Heading, out string Title, out string Description)
```

#### Parameters

`Heading` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Title` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Description` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_Toast_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Toast_Equals_MicroEngine_UI_Toast_"></a> Equals\(Toast?\)

```csharp
public bool Equals(Toast? other)
```

#### Parameters

`other` [Toast](MicroEngine.UI.Toast.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Toast_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_Toast_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_UI_Toast_op_Equality_MicroEngine_UI_Toast_MicroEngine_UI_Toast_"></a> operator ==\(Toast?, Toast?\)

```csharp
public static bool operator ==(Toast? left, Toast? right)
```

#### Parameters

`left` [Toast](MicroEngine.UI.Toast.md)?

`right` [Toast](MicroEngine.UI.Toast.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Toast_op_Inequality_MicroEngine_UI_Toast_MicroEngine_UI_Toast_"></a> operator \!=\(Toast?, Toast?\)

```csharp
public static bool operator !=(Toast? left, Toast? right)
```

#### Parameters

`left` [Toast](MicroEngine.UI.Toast.md)?

`right` [Toast](MicroEngine.UI.Toast.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
