# Interface ILog

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Where the engine's diagnostics go. The default ([ConsoleLog](MicroEngine.Kernel.ConsoleLog.md)) writes them to the standard error stream;
a host that has its own (a phone's system log, a browser console, an editor panel) registers another sink as the
engine's [ILog](MicroEngine.Kernel.ILog.md) service.

```csharp
public interface ILog
```

## Methods

### <a id="MicroEngine_Kernel_ILog_Write_MicroEngine_Kernel_LogLevel_System_String_System_String_"></a> Write\(LogLevel, string, string\)

Writes one message.

```csharp
void Write(LogLevel level, string category, string message)
```

#### Parameters

`level` [LogLevel](MicroEngine.Kernel.LogLevel.md)

How important it is.

`category` [string](https://learn.microsoft.com/dotnet/api/system.string)

The part of the engine it comes from, a short lower-case word ("assets", "platform").

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)

The message, one line, without the category.

</div>
