# Class MemoryCursor

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

A cursor that only remembers what it was asked: the headless platform's, so a game's cursor handling runs in tests
and scripted runs.

```csharp
public sealed class MemoryCursor : ICursorControl
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MemoryCursor](MicroEngine.Input.MemoryCursor.md)

#### Implements

[ICursorControl](MicroEngine.Input.ICursorControl.md)

## Constructors

### <a id="MicroEngine_Input_MemoryCursor__ctor"></a> MemoryCursor\(\)

```csharp
public MemoryCursor()
```

## Properties

### <a id="MicroEngine_Input_MemoryCursor_AllowsRelative"></a> AllowsRelative

Whether [SetRelative](MicroEngine.Input.MemoryCursor.md#MicroEngine_Input_MemoryCursor_SetRelative_System_Boolean_) succeeds (true by default); a test sets it to false to stand for a browser that refuses.

```csharp
public bool AllowsRelative { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_MemoryCursor_Relative"></a> Relative

Whether the cursor is locked to the window and the mouse reports motion only.

```csharp
public bool Relative { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_MemoryCursor_Visible"></a> Visible

Whether the system's cursor is drawn while it is over the window (true at start). A locked cursor is never drawn.

```csharp
public bool Visible { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Input_MemoryCursor_SetRelative_System_Boolean_"></a> SetRelative\(bool\)

Locks (<code class="paramref">relative</code>) or frees the cursor. Returns whether the platform did it: false when the
system refused (a browser outside a click or key press).

```csharp
public bool SetRelative(bool relative)
```

#### Parameters

`relative` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
