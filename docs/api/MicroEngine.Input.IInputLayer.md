# Interface IInputLayer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

One stage of input routing: user interface, on-screen controls, a debug console, a rebinding screen. Events reach
the layers from the highest order down ([InputLayers](MicroEngine.Input.InputLayers.md)); a layer that returns true takes the event, and
nothing below it (other layers, the actions of gameplay) sees it.

```csharp
public interface IInputLayer
```

## Remarks

A layer that takes a press also gets everything that press starts: the same pointer's moves until it lifts, the
same key's or button's release. So a finger that went down on an on-screen button stays the button's even when it
slides away, and a key that opened a menu does not reach gameplay when it is let go.

## Methods

### <a id="MicroEngine_Input_IInputLayer_Handle_MicroEngine_Input_InputEvent__"></a> Handle\(in InputEvent\)

Handles one event; returns true to take it from every layer and from gameplay below.

```csharp
bool Handle(in InputEvent e)
```

#### Parameters

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
