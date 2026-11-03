# Class ProfileChoice<TProfile\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Hosting](MicroEngine.Hosting.md)  
Assembly: MicroEngine.Hosting.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

The platforms a launch tries, in order, or why none can be tried.

```csharp
public sealed record ProfileChoice<TProfile> : IEquatable<ProfileChoice<TProfile>> where TProfile : class, IHostProfile
```

#### Type Parameters

`TProfile` 

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ProfileChoice<TProfile\>](MicroEngine.Hosting.ProfileChoice\-1.md)

#### Implements

[IEquatable<ProfileChoice<TProfile\>\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Hosting_ProfileChoice_1__ctor_System_Collections_Generic_IReadOnlyList__0__System_String_System_Collections_Generic_IReadOnlyList_System_String__"></a> ProfileChoice\(IReadOnlyList<TProfile\>, string?, IReadOnlyList<string\>\)

The platforms a launch tries, in order, or why none can be tried.

```csharp
public ProfileChoice(IReadOnlyList<TProfile> Candidates, string? Error, IReadOnlyList<string> Notes)
```

#### Parameters

`Candidates` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

The profiles to try in order; the next one is tried when one fails to start. Empty when <code class="paramref">Error</code> is set.

`Error` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Why no profile can be tried (an unknown or unsupported name, an empty catalog), or null.

`Notes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

Messages for the player about the choice (a preference that was skipped).

## Properties

### <a id="MicroEngine_Hosting_ProfileChoice_1_Candidates"></a> Candidates

The profiles to try in order; the next one is tried when one fails to start. Empty when <code class="paramref">Error</code> is set.

```csharp
public IReadOnlyList<TProfile> Candidates { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

### <a id="MicroEngine_Hosting_ProfileChoice_1_Error"></a> Error

Why no profile can be tried (an unknown or unsupported name, an empty catalog), or null.

```csharp
public string? Error { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_ProfileChoice_1_Notes"></a> Notes

Messages for the player about the choice (a preference that was skipped).

```csharp
public IReadOnlyList<string> Notes { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Hosting_ProfileChoice_1__Clone__"></a> <Clone\>$\(\)

```csharp
public ProfileChoice<TProfile> <Clone>$()
```

#### Returns

 [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>

### <a id="MicroEngine_Hosting_ProfileChoice_1_Deconstruct_System_Collections_Generic_IReadOnlyList__0___System_String__System_Collections_Generic_IReadOnlyList_System_String___"></a> Deconstruct\(out IReadOnlyList<TProfile\>, out string?, out IReadOnlyList<string\>\)

```csharp
public void Deconstruct(out IReadOnlyList<TProfile> Candidates, out string? Error, out IReadOnlyList<string> Notes)
```

#### Parameters

`Candidates` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

`Error` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`Notes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Hosting_ProfileChoice_1_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_ProfileChoice_1_Equals_MicroEngine_Hosting_ProfileChoice__0__"></a> Equals\(ProfileChoice<TProfile\>?\)

```csharp
public bool Equals(ProfileChoice<TProfile>? other)
```

#### Parameters

`other` [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_ProfileChoice_1_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Hosting_ProfileChoice_1_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Hosting_ProfileChoice_1_op_Equality_MicroEngine_Hosting_ProfileChoice__0__MicroEngine_Hosting_ProfileChoice__0__"></a> operator ==\(ProfileChoice<TProfile\>?, ProfileChoice<TProfile\>?\)

```csharp
public static bool operator ==(ProfileChoice<TProfile>? left, ProfileChoice<TProfile>? right)
```

#### Parameters

`left` [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>?

`right` [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_ProfileChoice_1_op_Inequality_MicroEngine_Hosting_ProfileChoice__0__MicroEngine_Hosting_ProfileChoice__0__"></a> operator \!=\(ProfileChoice<TProfile\>?, ProfileChoice<TProfile\>?\)

```csharp
public static bool operator !=(ProfileChoice<TProfile>? left, ProfileChoice<TProfile>? right)
```

#### Parameters

`left` [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>?

`right` [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
