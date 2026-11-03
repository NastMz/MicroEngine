# Class InputLayerOrder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The orders of the engine's own layers. Higher orders see events first; gameplay is below every layer.

```csharp
public static class InputLayerOrder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputLayerOrder](MicroEngine.Input.InputLayerOrder.md)

## Fields

### <a id="MicroEngine_Input_InputLayerOrder_Game"></a> Game

Game layers that sit right above gameplay (100).

```csharp
public const int Game = 100
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputLayerOrder_TouchControls"></a> TouchControls

The on-screen touch controls (200).

```csharp
public const int TouchControls = 200
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputLayerOrder_UI"></a> UI

User interface: menus, dialogs, text fields (300).

```csharp
public const int UI = 300
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
