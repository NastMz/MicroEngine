# Struct SlotInfo

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

What a storage knows about one of its slots without reading the save in it.

```csharp
public readonly struct SlotInfo : IEquatable<SlotInfo>
```

#### Implements

[IEquatable<SlotInfo\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Platform_SlotInfo__ctor_System_String_System_Int64_System_Nullable_System_DateTimeOffset__"></a> SlotInfo\(string, long, DateTimeOffset?\)

What a storage knows about one of its slots without reading the save in it.

```csharp
public SlotInfo(string Name, long Size, DateTimeOffset? LastWrite)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The slot's name, as [Read](MicroEngine.Platform.IStorage.md#MicroEngine_Platform_IStorage_Read_System_String_) takes it.

`Size` [long](https://learn.microsoft.com/dotnet/api/system.int64)

How much the slot holds: the file's size in bytes for file storage, the length of the stored text for a storage
that keeps text (the browser's). Good for showing and comparing, not for exact accounting.

`LastWrite` [DateTimeOffset](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)?

When the slot was last written, or null when the storage does not know (a slot written by an earlier version of the
browser storage, a medium that keeps no times).

## Properties

### <a id="MicroEngine_Platform_SlotInfo_LastWrite"></a> LastWrite

When the slot was last written, or null when the storage does not know (a slot written by an earlier version of the
browser storage, a medium that keeps no times).

```csharp
public DateTimeOffset? LastWrite { get; init; }
```

#### Property Value

 [DateTimeOffset](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)?

### <a id="MicroEngine_Platform_SlotInfo_Name"></a> Name

The slot's name, as [Read](MicroEngine.Platform.IStorage.md#MicroEngine_Platform_IStorage_Read_System_String_) takes it.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Platform_SlotInfo_Size"></a> Size

How much the slot holds: the file's size in bytes for file storage, the length of the stored text for a storage
that keeps text (the browser's). Good for showing and comparing, not for exact accounting.

```csharp
public long Size { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Platform_SlotInfo_Deconstruct_System_String__System_Int64__System_Nullable_System_DateTimeOffset___"></a> Deconstruct\(out string, out long, out DateTimeOffset?\)

```csharp
public void Deconstruct(out string Name, out long Size, out DateTimeOffset? LastWrite)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Size` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`LastWrite` [DateTimeOffset](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)?

### <a id="MicroEngine_Platform_SlotInfo_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_SlotInfo_Equals_MicroEngine_Platform_SlotInfo_"></a> Equals\(SlotInfo\)

```csharp
public bool Equals(SlotInfo other)
```

#### Parameters

`other` [SlotInfo](MicroEngine.Platform.SlotInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_SlotInfo_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Platform_SlotInfo_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Platform_SlotInfo_op_Equality_MicroEngine_Platform_SlotInfo_MicroEngine_Platform_SlotInfo_"></a> operator ==\(SlotInfo, SlotInfo\)

```csharp
public static bool operator ==(SlotInfo left, SlotInfo right)
```

#### Parameters

`left` [SlotInfo](MicroEngine.Platform.SlotInfo.md)

`right` [SlotInfo](MicroEngine.Platform.SlotInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_SlotInfo_op_Inequality_MicroEngine_Platform_SlotInfo_MicroEngine_Platform_SlotInfo_"></a> operator \!=\(SlotInfo, SlotInfo\)

```csharp
public static bool operator !=(SlotInfo left, SlotInfo right)
```

#### Parameters

`left` [SlotInfo](MicroEngine.Platform.SlotInfo.md)

`right` [SlotInfo](MicroEngine.Platform.SlotInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
