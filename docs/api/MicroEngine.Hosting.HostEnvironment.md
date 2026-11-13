# Class HostEnvironment

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Hosting](MicroEngine.Hosting.md)  
Assembly: MicroEngine.Hosting.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

What a host knows about the process it runs in, handed to the composition instead of being read from process-wide
state: its kind, its developer variables (environment variables on desktop; the page's or the launch's variables
elsewhere), where it reports and where its files are. Tests build one with their own variables and writers.

```csharp
public sealed class HostEnvironment
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HostEnvironment](MicroEngine.Hosting.HostEnvironment.md)

## Constructors

### <a id="MicroEngine_Hosting_HostEnvironment__ctor_MicroEngine_Hosting_HostKind_"></a> HostEnvironment\(HostKind\)

An environment of <code class="paramref">kind</code> over this process: its environment variables, the console and its base directory.

```csharp
public HostEnvironment(HostKind kind)
```

#### Parameters

`kind` [HostKind](MicroEngine.Hosting.HostKind.md)

## Properties

### <a id="MicroEngine_Hosting_HostEnvironment_BaseDirectory"></a> BaseDirectory

The folder the host's program runs from (`BaseDirectory` by default).

```csharp
public string BaseDirectory { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Hosting_HostEnvironment_ClockSeed"></a> ClockSeed

A seed for runs that ask for none, from the clock by default ([ResolveSeed](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ResolveSeed_System_Int32_)).

```csharp
public Func<int> ClockSeed { get; init; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

### <a id="MicroEngine_Hosting_HostEnvironment_CrashDirectory"></a> CrashDirectory

Where the desktop launcher writes the crash report of a run that fails (<code>Core.CrashReport</code>), or null for
the game's save folder (<code>SaveStore.DefaultDirectory</code>).

```csharp
public string? CrashDirectory { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_HostEnvironment_Error"></a> Error

Where the host prints errors (the console's standard error by default, as it was when this was created).

```csharp
public TextWriter Error { get; init; }
```

#### Property Value

 [TextWriter](https://learn.microsoft.com/dotnet/api/system.io.textwriter)

### <a id="MicroEngine_Hosting_HostEnvironment_Kind"></a> Kind

The kind of host.

```csharp
public HostKind Kind { get; }
```

#### Property Value

 [HostKind](MicroEngine.Hosting.HostKind.md)

### <a id="MicroEngine_Hosting_HostEnvironment_Out"></a> Out

Where the host prints results (the console's standard output by default, as it was when this was created).

```csharp
public TextWriter Out { get; init; }
```

#### Property Value

 [TextWriter](https://learn.microsoft.com/dotnet/api/system.io.textwriter)

### <a id="MicroEngine_Hosting_HostEnvironment_Variables"></a> Variables

Reads a developer variable by name, or null; the process's environment variables by default.

```csharp
public Func<string, string?> Variables { get; init; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)?\>

## Methods

### <a id="MicroEngine_Hosting_HostEnvironment_Variable_System_String_"></a> Variable\(string\)

The developer variable <code class="paramref">name</code>, or null when it is unset or blank.

```csharp
public string? Variable(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
