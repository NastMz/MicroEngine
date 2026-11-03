# Class KeyUsages

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The physical position of each [Key](MicroEngine.Input.Key.md) as a USB HID keyboard usage (page 7), the code SDL calls a scancode:
backends that report HID usages map them with one table.

```csharp
public static class KeyUsages
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[KeyUsages](MicroEngine.Input.KeyUsages.md)

## Properties

### <a id="MicroEngine_Input_KeyUsages_All"></a> All

Every key the engine names, in enum order (no [None](MicroEngine.Input.Key.md)).

```csharp
public static IReadOnlyList<Key> All { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Key](MicroEngine.Input.Key.md)\>

## Methods

### <a id="MicroEngine_Input_KeyUsages_FromHidUsage_System_Int32_"></a> FromHidUsage\(int\)

The key at HID usage <code class="paramref">usage</code>, or [None](MicroEngine.Input.Key.md) for a usage the engine does not name.

```csharp
public static Key FromHidUsage(int usage)
```

#### Parameters

`usage` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [Key](MicroEngine.Input.Key.md)

### <a id="MicroEngine_Input_KeyUsages_Usage_MicroEngine_Input_Key_"></a> Usage\(Key\)

The HID usage of <code class="paramref">key</code>, or 0 for [None](MicroEngine.Input.Key.md).

```csharp
public static int Usage(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
