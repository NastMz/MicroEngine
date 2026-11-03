# Interface IKeyLabels

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The labels the player's keyboard layout prints on keys: "Q" for [A](MicroEngine.Input.Key.md) on AZERTY. A capability of input
backends that know the layout ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)).

```csharp
public interface IKeyLabels
```

## Methods

### <a id="MicroEngine_Input_IKeyLabels_KeyLabel_MicroEngine_Input_Key_"></a> KeyLabel\(Key\)

The key's label in the current layout, or null when the backend cannot tell (the US name is used then).

```csharp
string? KeyLabel(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
