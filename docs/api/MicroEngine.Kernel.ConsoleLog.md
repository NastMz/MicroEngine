# Class ConsoleLog

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The default [ILog](MicroEngine.Kernel.ILog.md): each message at or above [MinimumLevel](MicroEngine.Kernel.ConsoleLog.md#MicroEngine_Kernel_ConsoleLog_MinimumLevel) becomes one line
<code>category: message</code> on `Error` (read when the message is written, so a redirected stream
receives it) or on the writer it was given.

```csharp
public sealed class ConsoleLog : ILog
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ConsoleLog](MicroEngine.Kernel.ConsoleLog.md)

#### Implements

[ILog](MicroEngine.Kernel.ILog.md)

## Constructors

### <a id="MicroEngine_Kernel_ConsoleLog__ctor_System_IO_TextWriter_"></a> ConsoleLog\(TextWriter?\)

The default [ILog](MicroEngine.Kernel.ILog.md): each message at or above [MinimumLevel](MicroEngine.Kernel.ConsoleLog.md#MicroEngine_Kernel_ConsoleLog_MinimumLevel) becomes one line
<code>category: message</code> on `Error` (read when the message is written, so a redirected stream
receives it) or on the writer it was given.

```csharp
public ConsoleLog(TextWriter? writer = null)
```

#### Parameters

`writer` [TextWriter](https://learn.microsoft.com/dotnet/api/system.io.textwriter)?

## Properties

### <a id="MicroEngine_Kernel_ConsoleLog_MinimumLevel"></a> MinimumLevel

Messages below this level are dropped ([Info](MicroEngine.Kernel.LogLevel.md) by default).

```csharp
public LogLevel MinimumLevel { get; init; }
```

#### Property Value

 [LogLevel](MicroEngine.Kernel.LogLevel.md)

## Methods

### <a id="MicroEngine_Kernel_ConsoleLog_Write_MicroEngine_Kernel_LogLevel_System_String_System_String_"></a> Write\(LogLevel, string, string\)

Writes one message.

```csharp
public void Write(LogLevel level, string category, string message)
```

#### Parameters

`level` [LogLevel](MicroEngine.Kernel.LogLevel.md)

How important it is.

`category` [string](https://learn.microsoft.com/dotnet/api/system.string)

The part of the engine it comes from, a short lower-case word ("assets", "platform").

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)

The message, one line, without the category.

</div>
