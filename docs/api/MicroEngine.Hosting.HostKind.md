# Enum HostKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Hosting](MicroEngine.Hosting.md)  
Assembly: MicroEngine.Hosting.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

The kind of program hosting a game; the composition may differ by kind (a browser has no files to save to).

```csharp
public enum HostKind
```

## Fields

`Android = 2` 

An Android app.



`Browser = 1` 

A browser page that drives frames from <code>requestAnimationFrame</code> and cannot block.



`Desktop = 0` 

A desktop process (Windows, Linux, macOS) started from its command line.



`IOS = 3` 

An iOS app.



`Test = 4` 

A test or tool that runs games in its own process.

</div>
