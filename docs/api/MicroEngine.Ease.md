# Class Ease

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Standard easing curves mapping 0..1 to 0..1 (some overshoot).

```csharp
public static class Ease
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Ease](MicroEngine.Ease.md)

## Methods

### <a id="MicroEngine_Ease_InCubic_System_Single_"></a> InCubic\(float\)

Starts slow, ends fast (cubic).

```csharp
public static float InCubic(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_InOutQuad_System_Single_"></a> InOutQuad\(float\)

Slow at both ends (quadratic).

```csharp
public static float InOutQuad(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_InOutSine_System_Single_"></a> InOutSine\(float\)

Slow at both ends (sine).

```csharp
public static float InOutSine(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_InQuad_System_Single_"></a> InQuad\(float\)

Starts slow, ends fast (quadratic).

```csharp
public static float InQuad(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_Linear_System_Single_"></a> Linear\(float\)

No easing.

```csharp
public static float Linear(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_OutBack_System_Single_"></a> OutBack\(float\)

Overshoots past 1 and settles back.

```csharp
public static float OutBack(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_OutBounce_System_Single_"></a> OutBounce\(float\)

Bounces on 1 like a dropped ball.

```csharp
public static float OutBounce(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_OutCubic_System_Single_"></a> OutCubic\(float\)

Starts fast, ends slow (cubic).

```csharp
public static float OutCubic(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_OutElastic_System_Single_"></a> OutElastic\(float\)

Springs past 1 and oscillates to rest.

```csharp
public static float OutElastic(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ease_OutQuad_System_Single_"></a> OutQuad\(float\)

Starts fast, ends slow (quadratic); the default of <code>Tweener.Add</code>.

```csharp
public static float OutQuad(float t)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
