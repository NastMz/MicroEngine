# Class CrashReport

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A text file that describes a run that stopped on an error, for a tester to send: the game and engine versions, the
operating system and runtime, the platform's facts ([IPlatformDiagnostics](MicroEngine.Core.IPlatformDiagnostics.md)), the last measured frame and
its slowest systems when [Diagnostics](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Diagnostics) measured, and the error with its stack trace. The desktop
launcher writes one when a game's run fails and names it on standard error.

```csharp
public static class CrashReport
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CrashReport](MicroEngine.Core.CrashReport.md)

## Methods

### <a id="MicroEngine_Core_CrashReport_Describe_MicroEngine_Core_Game_System_Exception_"></a> Describe\(Game, Exception\)

The report's text for <code class="paramref">error</code>, which stopped <code class="paramref">game</code>.

```csharp
public static string Describe(Game game, Exception error)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`error` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

An argument is null.

### <a id="MicroEngine_Core_CrashReport_Write_MicroEngine_Core_Game_System_Exception_System_String_"></a> Write\(Game, Exception, string\)

Writes the report into <code class="paramref">directory</code> (created if needed) as <code>crash-&lt;UTC time&gt;.txt</code> and returns its path.

```csharp
public static string Write(Game game, Exception error, string directory)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`error` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

`directory` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

An argument is null.

 [IOException](https://learn.microsoft.com/dotnet/api/system.io.ioexception)

The file cannot be written.

 [UnauthorizedAccessException](https://learn.microsoft.com/dotnet/api/system.unauthorizedaccessexception)

The folder is not writable.

</div>
