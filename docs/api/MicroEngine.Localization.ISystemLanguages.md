# Interface ISystemLanguages

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

An optional feature of a platform: the languages the player set on the system, most wanted first, as tags
(<code>es-MX</code>, <code>en</code>). The game's first language is the first of them it has text in. A platform that cannot
tell offers no such feature, and the runtime's own culture is used where the build carries culture data.

```csharp
public interface ISystemLanguages
```

## Properties

### <a id="MicroEngine_Localization_ISystemLanguages_PreferredLanguages"></a> PreferredLanguages

The player's languages, most wanted first; empty when the system does not say.

```csharp
IReadOnlyList<string> PreferredLanguages { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

</div>
