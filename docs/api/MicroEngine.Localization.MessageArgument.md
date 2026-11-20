# Class MessageArgument

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

An argument a [Message](MicroEngine.Localization.Message.md) names, for tools that compare the translations of a text.

```csharp
public sealed record MessageArgument : IEquatable<MessageArgument>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MessageArgument](MicroEngine.Localization.MessageArgument.md)

#### Implements

[IEquatable<MessageArgument\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Localization_MessageArgument__ctor_System_String_MicroEngine_Localization_MessageArgumentKind_System_Collections_Generic_IReadOnlyList_System_String__"></a> MessageArgument\(string, MessageArgumentKind, IReadOnlyList<string\>\)

An argument a [Message](MicroEngine.Localization.Message.md) names, for tools that compare the translations of a text.

```csharp
public MessageArgument(string Name, MessageArgumentKind Kind, IReadOnlyList<string> Branches)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The name between braces.

`Kind` [MessageArgumentKind](MicroEngine.Localization.MessageArgumentKind.md)

How the message uses it.

`Branches` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

The branches of a plural or a selection as written (<code>=0</code>, <code>one</code>, <code>other</code>); empty for a value.

## Properties

### <a id="MicroEngine_Localization_MessageArgument_Branches"></a> Branches

The branches of a plural or a selection as written (<code>=0</code>, <code>one</code>, <code>other</code>); empty for a value.

```csharp
public IReadOnlyList<string> Branches { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_MessageArgument_Kind"></a> Kind

How the message uses it.

```csharp
public MessageArgumentKind Kind { get; init; }
```

#### Property Value

 [MessageArgumentKind](MicroEngine.Localization.MessageArgumentKind.md)

### <a id="MicroEngine_Localization_MessageArgument_Name"></a> Name

The name between braces.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Localization_MessageArgument__Clone__"></a> <Clone\>$\(\)

```csharp
public MessageArgument <Clone>$()
```

#### Returns

 [MessageArgument](MicroEngine.Localization.MessageArgument.md)

### <a id="MicroEngine_Localization_MessageArgument_Deconstruct_System_String__MicroEngine_Localization_MessageArgumentKind__System_Collections_Generic_IReadOnlyList_System_String___"></a> Deconstruct\(out string, out MessageArgumentKind, out IReadOnlyList<string\>\)

```csharp
public void Deconstruct(out string Name, out MessageArgumentKind Kind, out IReadOnlyList<string> Branches)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Kind` [MessageArgumentKind](MicroEngine.Localization.MessageArgumentKind.md)

`Branches` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_MessageArgument_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_MessageArgument_Equals_MicroEngine_Localization_MessageArgument_"></a> Equals\(MessageArgument?\)

```csharp
public bool Equals(MessageArgument? other)
```

#### Parameters

`other` [MessageArgument](MicroEngine.Localization.MessageArgument.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_MessageArgument_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Localization_MessageArgument_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Localization_MessageArgument_op_Equality_MicroEngine_Localization_MessageArgument_MicroEngine_Localization_MessageArgument_"></a> operator ==\(MessageArgument?, MessageArgument?\)

```csharp
public static bool operator ==(MessageArgument? left, MessageArgument? right)
```

#### Parameters

`left` [MessageArgument](MicroEngine.Localization.MessageArgument.md)?

`right` [MessageArgument](MicroEngine.Localization.MessageArgument.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_MessageArgument_op_Inequality_MicroEngine_Localization_MessageArgument_MicroEngine_Localization_MessageArgument_"></a> operator \!=\(MessageArgument?, MessageArgument?\)

```csharp
public static bool operator !=(MessageArgument? left, MessageArgument? right)
```

#### Parameters

`left` [MessageArgument](MicroEngine.Localization.MessageArgument.md)?

`right` [MessageArgument](MicroEngine.Localization.MessageArgument.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
