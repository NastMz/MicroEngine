# Class PlatformProfile

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Backends and platforms](../manual/backends.md)  

A named way to build an [IPlatform](MicroEngine.Core.IPlatform.md): an official backend ("sdl", "raylib", "headless") or one a
developer registers. The profile decides which parts a host may replace ([Replaceable](MicroEngine.Core.PlatformProfile.md#MicroEngine_Core_PlatformProfile_Replaceable)), because some
subsystems share state with the window and cannot be swapped independently.

```csharp
public sealed class PlatformProfile : IHostProfile
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PlatformProfile](MicroEngine.Core.PlatformProfile.md)

#### Implements

[IHostProfile](MicroEngine.Hosting.IHostProfile.md)

## Constructors

### <a id="MicroEngine_Core_PlatformProfile__ctor_System_String_System_String_System_Func_MicroEngine_Core_PlatformRequest_MicroEngine_Core_IPlatform__"></a> PlatformProfile\(string, string, Func<PlatformRequest, IPlatform\>\)

Creates a profile.

```csharp
public PlatformProfile(string name, string description, Func<PlatformRequest, IPlatform> create)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The name hosts select it by, case-insensitive; must not be empty.

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

One line shown by <code>--list-platforms</code>.

`create` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[PlatformRequest](MicroEngine.Core.PlatformRequest.md), [IPlatform](MicroEngine.Core.IPlatform.md)\>

Builds the platform for a request.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">name</code> is empty.

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">create</code> is null.

## Properties

### <a id="MicroEngine_Core_PlatformProfile_CheckSupport"></a> CheckSupport

Returns why the profile cannot run in this process (system, missing library), or null when it can.

```csharp
public Func<string?> CheckSupport { get; init; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)?\>

### <a id="MicroEngine_Core_PlatformProfile_Description"></a> Description

A one-line description for listings.

```csharp
public string Description { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_PlatformProfile_GraphicsDevices"></a> GraphicsDevices

The graphics devices a request may name in [Device](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Device) (compared case-insensitively),
the platform's default first; empty (the default) when the platform has only its own device, which a request then
selects by leaving [Device](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Device) null.

```csharp
public IReadOnlyList<string> GraphicsDevices { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_PlatformProfile_Name"></a> Name

The name hosts select the profile by (compared case-insensitively).

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_PlatformProfile_Replaceable"></a> Replaceable

The parts a host may supply in its request (audio and input by default).

```csharp
public PlatformParts Replaceable { get; init; }
```

#### Property Value

 [PlatformParts](MicroEngine.Core.PlatformParts.md)

### <a id="MicroEngine_Core_PlatformProfile_UnsupportedReason"></a> UnsupportedReason

Why the profile cannot run in this process, or null when it can; evaluates [CheckSupport](MicroEngine.Core.PlatformProfile.md#MicroEngine_Core_PlatformProfile_CheckSupport) on each read.

```csharp
public string? UnsupportedReason { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

## Methods

### <a id="MicroEngine_Core_PlatformProfile_Create_MicroEngine_Core_PlatformRequest_"></a> Create\(PlatformRequest\)

Builds the platform for <code class="paramref">request</code>. The supplied parts ([Audio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_Audio),
[ExtraInput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_ExtraInput)) are composed into the backend when it is built, never by wrapping it,
so the platform returned is the backend itself with all its capabilities. Throws
[PlatformConfigurationException](MicroEngine.Core.PlatformConfigurationException.md) for a request the profile cannot honour, and when the backend did not
use the parts the request supplied.

```csharp
public IPlatform Create(PlatformRequest request)
```

#### Parameters

`request` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)

#### Returns

 [IPlatform](MicroEngine.Core.IPlatform.md)

### <a id="MicroEngine_Core_PlatformProfile_ToString"></a> ToString\(\)

The profile's [Name](MicroEngine.Core.PlatformProfile.md#MicroEngine_Core_PlatformProfile_Name).

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
