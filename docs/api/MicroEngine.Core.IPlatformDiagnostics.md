# Interface IPlatformDiagnostics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Facts a platform can report about itself for logs and profiles: the graphics API in use, VSync, the graphics adapter
when the backend can name it. Keys are short lower-case words; values are free text.

```csharp
public interface IPlatformDiagnostics
```

## Properties

### <a id="MicroEngine_Core_IPlatformDiagnostics_Diagnostics"></a> Diagnostics

The facts, read when asked (a value may change, such as the display's refresh rate).

```csharp
IReadOnlyDictionary<string, string> Diagnostics { get; }
```

#### Property Value

 [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

</div>
