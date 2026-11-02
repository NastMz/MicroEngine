# Struct Percentiles

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Percentiles of one frame measurement, in milliseconds.

```csharp
public readonly struct Percentiles : IEquatable<Percentiles>
```

#### Implements

[IEquatable<Percentiles\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_Percentiles__ctor_System_Double_System_Double_System_Double_System_Double_System_Double_System_Double_"></a> Percentiles\(double, double, double, double, double, double\)

Percentiles of one frame measurement, in milliseconds.

```csharp
public Percentiles(double Mean, double P50, double P95, double P99, double P999, double Max)
```

#### Parameters

`Mean` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P50` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P95` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P99` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P999` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Max` [double](https://learn.microsoft.com/dotnet/api/system.double)

## Properties

### <a id="MicroEngine_Core_Percentiles_Max"></a> Max

```csharp
public double Max { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_Percentiles_Mean"></a> Mean

```csharp
public double Mean { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_Percentiles_P50"></a> P50

```csharp
public double P50 { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_Percentiles_P95"></a> P95

```csharp
public double P95 { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_Percentiles_P99"></a> P99

```csharp
public double P99 { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_Percentiles_P999"></a> P999

```csharp
public double P999 { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

## Methods

### <a id="MicroEngine_Core_Percentiles_Deconstruct_System_Double__System_Double__System_Double__System_Double__System_Double__System_Double__"></a> Deconstruct\(out double, out double, out double, out double, out double, out double\)

```csharp
public void Deconstruct(out double Mean, out double P50, out double P95, out double P99, out double P999, out double Max)
```

#### Parameters

`Mean` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P50` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P95` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P99` [double](https://learn.microsoft.com/dotnet/api/system.double)

`P999` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Max` [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_Percentiles_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Percentiles_Equals_MicroEngine_Core_Percentiles_"></a> Equals\(Percentiles\)

```csharp
public bool Equals(Percentiles other)
```

#### Parameters

`other` [Percentiles](MicroEngine.Core.Percentiles.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Percentiles_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_Percentiles_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_Percentiles_op_Equality_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_"></a> operator ==\(Percentiles, Percentiles\)

```csharp
public static bool operator ==(Percentiles left, Percentiles right)
```

#### Parameters

`left` [Percentiles](MicroEngine.Core.Percentiles.md)

`right` [Percentiles](MicroEngine.Core.Percentiles.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Percentiles_op_Inequality_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_"></a> operator \!=\(Percentiles, Percentiles\)

```csharp
public static bool operator !=(Percentiles left, Percentiles right)
```

#### Parameters

`left` [Percentiles](MicroEngine.Core.Percentiles.md)

`right` [Percentiles](MicroEngine.Core.Percentiles.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
