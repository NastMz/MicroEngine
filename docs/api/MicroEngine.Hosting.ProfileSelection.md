# Class ProfileSelection

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Hosting](MicroEngine.Hosting.md)  
Assembly: MicroEngine.Hosting.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

How every host chooses a platform from its catalog (an ordered list of profiles, the most preferred first). The rules
are the launcher's, written once: a named platform ([Platform](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Platform), else the developer variable
<code>MICROENGINE_PLATFORM</code>) is the only candidate; [Headless](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Headless) names the headless platform;
otherwise the game's preferred backend comes first, then every other platform that can run here except the headless
one (a player who sees no window would not know why), or the catalog's default when that leaves none.

```csharp
public static class ProfileSelection
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ProfileSelection](MicroEngine.Hosting.ProfileSelection.md)

## Fields

### <a id="MicroEngine_Hosting_ProfileSelection_PlatformVariable"></a> PlatformVariable

The developer variable that names the platform when the launch names none.

```csharp
public const string PlatformVariable = "MICROENGINE_PLATFORM"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Hosting_ProfileSelection_Choose__1_System_Collections_Generic_IReadOnlyList___0__MicroEngine_Hosting_LaunchRequest_MicroEngine_Hosting_HostEnvironment_System_String___0_"></a> Choose<TProfile\>\(IReadOnlyList<TProfile\>, LaunchRequest, HostEnvironment, string?, TProfile?\)

The profiles a launch tries, in order (see the class remarks). <code class="paramref">headless</code> stands in for
<code>--headless</code> when the catalog has no headless profile; <code class="paramref">backend</code> is the game's preferred one.

```csharp
public static ProfileChoice<TProfile> Choose<TProfile>(IReadOnlyList<TProfile> catalog, LaunchRequest request, HostEnvironment environment, string? backend, TProfile? headless = null) where TProfile : class, IHostProfile
```

#### Parameters

`catalog` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

`request` [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)

`environment` [HostEnvironment](MicroEngine.Hosting.HostEnvironment.md)

`backend` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`headless` TProfile?

#### Returns

 [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>

#### Type Parameters

`TProfile` 

### <a id="MicroEngine_Hosting_ProfileSelection_Default__1_System_Collections_Generic_IReadOnlyList___0__"></a> Default<TProfile\>\(IReadOnlyList<TProfile\>\)

The first profile that can run here, or why there is none.

```csharp
public static (TProfile? Profile, string? Error) Default<TProfile>(IReadOnlyList<TProfile> catalog) where TProfile : class, IHostProfile
```

#### Parameters

`catalog` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

#### Returns

 \(TProfile? [Profile](https://learn.microsoft.com/dotnet/api/system.valuetuple\-\-\-0,system.string\-.profile), [string](https://learn.microsoft.com/dotnet/api/system.string)? [Error](https://learn.microsoft.com/dotnet/api/system.valuetuple\-\-\-0,system.string\-.error)\)

#### Type Parameters

`TProfile` 

### <a id="MicroEngine_Hosting_ProfileSelection_Fallbacks__1_System_Collections_Generic_IReadOnlyList___0__"></a> Fallbacks<TProfile\>\(IReadOnlyList<TProfile\>\)

The profiles tried when nobody named one: every profile that can run here except the headless one, or the
catalog's default when that leaves none.

```csharp
public static (IReadOnlyList<TProfile> Profiles, string? Error) Fallbacks<TProfile>(IReadOnlyList<TProfile> catalog) where TProfile : class, IHostProfile
```

#### Parameters

`catalog` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

#### Returns

 \([IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\> [Profiles](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.collections.generic.ireadonlylist\-\-\-0\-,system.string\-.profiles), [string](https://learn.microsoft.com/dotnet/api/system.string)? [Error](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.collections.generic.ireadonlylist\-\-\-0\-,system.string\-.error)\)

#### Type Parameters

`TProfile` 

### <a id="MicroEngine_Hosting_ProfileSelection_Find__1_System_Collections_Generic_IReadOnlyList___0__System_String_"></a> Find<TProfile\>\(IReadOnlyList<TProfile\>, string\)

The profile named <code class="paramref">name</code> (ignoring case), or null.

```csharp
public static TProfile? Find<TProfile>(IReadOnlyList<TProfile> catalog, string name) where TProfile : class, IHostProfile
```

#### Parameters

`catalog` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 TProfile?

#### Type Parameters

`TProfile` 

### <a id="MicroEngine_Hosting_ProfileSelection_Preferred__1_System_Collections_Generic_IReadOnlyList___0__System_String_"></a> Preferred<TProfile\>\(IReadOnlyList<TProfile\>, string?\)

The profiles tried when the game prefers <code class="paramref">backend</code> and nobody named one: that profile first, then
the [Fallbacks``1](MicroEngine.Hosting.ProfileSelection.md#MicroEngine_Hosting_ProfileSelection_Fallbacks__1_System_Collections_Generic_IReadOnlyList___0__). A preference the catalog lacks or that cannot run here becomes a note and is
skipped.

```csharp
public static ProfileChoice<TProfile> Preferred<TProfile>(IReadOnlyList<TProfile> catalog, string? backend) where TProfile : class, IHostProfile
```

#### Parameters

`catalog` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

`backend` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [ProfileChoice](MicroEngine.Hosting.ProfileChoice\-1.md)<TProfile\>

#### Type Parameters

`TProfile` 

### <a id="MicroEngine_Hosting_ProfileSelection_Resolve__1_System_Collections_Generic_IReadOnlyList___0__System_String_"></a> Resolve<TProfile\>\(IReadOnlyList<TProfile\>, string?\)

The named profile, or [Default``1](MicroEngine.Hosting.ProfileSelection.md#MicroEngine_Hosting_ProfileSelection_Default__1_System_Collections_Generic_IReadOnlyList___0__) for null or empty; otherwise why it cannot be used.

```csharp
public static (TProfile? Profile, string? Error) Resolve<TProfile>(IReadOnlyList<TProfile> catalog, string? name) where TProfile : class, IHostProfile
```

#### Parameters

`catalog` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TProfile\>

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 \(TProfile? [Profile](https://learn.microsoft.com/dotnet/api/system.valuetuple\-\-\-0,system.string\-.profile), [string](https://learn.microsoft.com/dotnet/api/system.string)? [Error](https://learn.microsoft.com/dotnet/api/system.valuetuple\-\-\-0,system.string\-.error)\)

#### Type Parameters

`TProfile`

</div>
