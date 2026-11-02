# Class DisplayInfo

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A display (monitor) as the system reports it.

```csharp
public sealed record DisplayInfo : IEquatable<DisplayInfo>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DisplayInfo](MicroEngine.Core.DisplayInfo.md)

#### Implements

[IEquatable<DisplayInfo\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_DisplayInfo__ctor_System_Int32_System_String_System_Int32_System_Int32_System_Double_System_Single_System_Boolean_"></a> DisplayInfo\(int, string, int, int, double, float, bool\)

A display (monitor) as the system reports it.

```csharp
public DisplayInfo(int Index, string Name, int Width, int Height, double RefreshRate, float Scale, bool Primary)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Position in [Displays](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_Displays), used by [Display](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Display) and [MoveToDisplay](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_MoveToDisplay_System_Int32_).

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The system's name for it ("LG FHD"); may be empty.

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width of its current mode in pixels.

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height of its current mode in pixels.

`RefreshRate` [double](https://learn.microsoft.com/dotnet/api/system.double)

Refresh rate of its current mode in hertz; 0 when unknown.

`Scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

The system's content scale for it (1.5 for 150%).

`Primary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it is the system's primary display.

## Properties

### <a id="MicroEngine_Core_DisplayInfo_Height"></a> Height

Height of its current mode in pixels.

```csharp
public int Height { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_DisplayInfo_Index"></a> Index

Position in [Displays](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_Displays), used by [Display](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Display) and [MoveToDisplay](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_MoveToDisplay_System_Int32_).

```csharp
public int Index { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_DisplayInfo_Name"></a> Name

The system's name for it ("LG FHD"); may be empty.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_DisplayInfo_Primary"></a> Primary

Whether it is the system's primary display.

```csharp
public bool Primary { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_DisplayInfo_RefreshRate"></a> RefreshRate

Refresh rate of its current mode in hertz; 0 when unknown.

```csharp
public double RefreshRate { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_DisplayInfo_Scale"></a> Scale

The system's content scale for it (1.5 for 150%).

```csharp
public float Scale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_DisplayInfo_Width"></a> Width

Width of its current mode in pixels.

```csharp
public int Width { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_DisplayInfo__Clone__"></a> <Clone\>$\(\)

```csharp
public DisplayInfo <Clone>$()
```

#### Returns

 [DisplayInfo](MicroEngine.Core.DisplayInfo.md)

### <a id="MicroEngine_Core_DisplayInfo_Deconstruct_System_Int32__System_String__System_Int32__System_Int32__System_Double__System_Single__System_Boolean__"></a> Deconstruct\(out int, out string, out int, out int, out double, out float, out bool\)

```csharp
public void Deconstruct(out int Index, out string Name, out int Width, out int Height, out double RefreshRate, out float Scale, out bool Primary)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`RefreshRate` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Primary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_DisplayInfo_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_DisplayInfo_Equals_MicroEngine_Core_DisplayInfo_"></a> Equals\(DisplayInfo?\)

```csharp
public bool Equals(DisplayInfo? other)
```

#### Parameters

`other` [DisplayInfo](MicroEngine.Core.DisplayInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_DisplayInfo_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_DisplayInfo_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_DisplayInfo_op_Equality_MicroEngine_Core_DisplayInfo_MicroEngine_Core_DisplayInfo_"></a> operator ==\(DisplayInfo?, DisplayInfo?\)

```csharp
public static bool operator ==(DisplayInfo? left, DisplayInfo? right)
```

#### Parameters

`left` [DisplayInfo](MicroEngine.Core.DisplayInfo.md)?

`right` [DisplayInfo](MicroEngine.Core.DisplayInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_DisplayInfo_op_Inequality_MicroEngine_Core_DisplayInfo_MicroEngine_Core_DisplayInfo_"></a> operator \!=\(DisplayInfo?, DisplayInfo?\)

```csharp
public static bool operator !=(DisplayInfo? left, DisplayInfo? right)
```

#### Parameters

`left` [DisplayInfo](MicroEngine.Core.DisplayInfo.md)?

`right` [DisplayInfo](MicroEngine.Core.DisplayInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
