# Class InputSystem

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The engine's input: the devices and their events, the routing through input layers, the players and their action
maps, and the text channel. Each frame the events of the platform's input backend (and those sent with
[Send](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Send_MicroEngine_Input_InputEvent__)) are routed through [Layers](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Layers); what no layer takes becomes the devices' polled state
and [Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events). Each update samples every player's map from the devices they hear.

```csharp
public sealed class InputSystem : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputSystem](MicroEngine.Input.InputSystem.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Remarks

A game rarely needs this directly: <code>Game.Input</code> is player 1's map and <code>Game.Players</code> the players. Read
[Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events) for what actions cannot express (a tap's position, a gesture, the order of presses).

## Constructors

### <a id="MicroEngine_Input_InputSystem__ctor_MicroEngine_Input_InputMap_"></a> InputSystem\(InputMap\)

Creates the input with player 1 reading <code class="paramref">first</code>.

```csharp
public InputSystem(InputMap first)
```

#### Parameters

`first` [InputMap](MicroEngine.Input.InputMap.md)

## Properties

### <a id="MicroEngine_Input_InputSystem_Devices"></a> Devices

The devices and their state.

```csharp
public InputDevices Devices { get; }
```

#### Property Value

 [InputDevices](MicroEngine.Input.InputDevices.md)

### <a id="MicroEngine_Input_InputSystem_Events"></a> Events

The events no layer took, since the previous update, in order: read them in an update for what actions cannot
say. Each event is seen by exactly one update (frames that run no update keep them for the next).

```csharp
public IReadOnlyList<InputEvent> Events { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[InputEvent](MicroEngine.Input.InputEvent.md)\>

### <a id="MicroEngine_Input_InputSystem_KeyLabels"></a> KeyLabels

The labels of the player's keyboard layout, when the backend knows them.

```csharp
public IKeyLabels? KeyLabels { get; }
```

#### Property Value

 [IKeyLabels](MicroEngine.Input.IKeyLabels.md)?

### <a id="MicroEngine_Input_InputSystem_Layers"></a> Layers

The input layers events pass through before gameplay.

```csharp
public InputLayers Layers { get; }
```

#### Property Value

 [InputLayers](MicroEngine.Input.InputLayers.md)

### <a id="MicroEngine_Input_InputSystem_Players"></a> Players

The local players; player 1 owns every device until devices are paired.

```csharp
public Players Players { get; }
```

#### Property Value

 [Players](MicroEngine.Input.Players.md)

### <a id="MicroEngine_Input_InputSystem_PolledSources"></a> PolledSources

The polled sources read as devices (a host's extra input, a test's hand-set input).

```csharp
public IReadOnlyList<(DeviceId Id, IInputSource Source)> PolledSources { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([DeviceId](MicroEngine.Input.DeviceId.md) [Id](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.deviceid,microengine.input.iinputsource\-.id), [IInputSource](MicroEngine.Input.IInputSource.md) [Source](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.deviceid,microengine.input.iinputsource\-.source)\)\>

### <a id="MicroEngine_Input_InputSystem_Routed"></a> Routed

While [Trace](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Trace) is on, the events routed this frame (device changes excepted) in order, each with the
layer that took it, or none when it reached the devices and gameplay: the answer to "why did my action not fire?"
when a menu, the on-screen controls or another layer took the press.

```csharp
public IReadOnlyList<RoutedEvent> Routed { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[RoutedEvent](MicroEngine.Input.RoutedEvent.md)\>

### <a id="MicroEngine_Input_InputSystem_Text"></a> Text

The text channel.

```csharp
public TextInput Text { get; }
```

#### Property Value

 [TextInput](MicroEngine.Input.TextInput.md)

### <a id="MicroEngine_Input_InputSystem_Trace"></a> Trace

Whether [Routed](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Routed) records where each event of the frame went. Off by default; the game's diagnostics
turn it on while they measure.

```csharp
public bool Trace { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Input_InputSystem_AddPolled_MicroEngine_Input_IInputSource_"></a> AddPolled\(IInputSource\)

Reads <code class="paramref">source</code> as one more device, every update: its keys, gamepad and pointer, merged like one
keyboard and one pad. Returns the id players pair it by.

```csharp
public DeviceId AddPolled(IInputSource source)
```

#### Parameters

`source` [IInputSource](MicroEngine.Input.IInputSource.md)

#### Returns

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Input_InputSystem_BeginFrame"></a> BeginFrame\(\)

Starts a frame: routes the backend's events, then the sent ones. Called by the loop once per frame.

```csharp
public void BeginFrame()
```

### <a id="MicroEngine_Input_InputSystem_Connect_MicroEngine_Input_IInputBackend_MicroEngine_Input_IKeyLabels_MicroEngine_Input_IGamepadRumble_MicroEngine_Input_ITextInputControl_"></a> Connect\(IInputBackend?, IKeyLabels?, IGamepadRumble?, ITextInputControl?\)

Connects the platform's input: its event backend and the optional features it offers.

```csharp
public void Connect(IInputBackend? backend, IKeyLabels? labels = null, IGamepadRumble? rumble = null, ITextInputControl? text = null)
```

#### Parameters

`backend` [IInputBackend](MicroEngine.Input.IInputBackend.md)?

`labels` [IKeyLabels](MicroEngine.Input.IKeyLabels.md)?

`rumble` [IGamepadRumble](MicroEngine.Input.IGamepadRumble.md)?

`text` [ITextInputControl](MicroEngine.Input.ITextInputControl.md)?

### <a id="MicroEngine_Input_InputSystem_EndUpdate"></a> EndUpdate\(\)

Ends an update: edge detection of every map, presses already released stop reading as held, the wheel and the
update's events are cleared. Called by the loop after each fixed update.

```csharp
public void EndUpdate()
```

### <a id="MicroEngine_Input_InputSystem_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the players beyond player 1 and the pairings ([HashState](MicroEngine.Input.Players.md#MicroEngine_Input_Players_HashState_MicroEngine_Kernel_StateHasher_)); player 1's map is hashed by its owner.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Input_InputSystem_ListenForPress_System_Action_MicroEngine_Input_InputEvent__"></a> ListenForPress\(Action<InputEvent\>\)

Takes the next key or gamepad-button press of any device away from everything else and hands it to
<code class="paramref">pressed</code>, once: the "press a key for jump" step of a rebinding screen
([BindFrom](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_BindFrom_System_String_MicroEngine_Input_InputEvent__System_Boolean_)). Dispose the result to stop listening before a press came.

```csharp
public IDisposable ListenForPress(Action<InputEvent> pressed)
```

#### Parameters

`pressed` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[InputEvent](MicroEngine.Input.InputEvent.md)\>

#### Returns

 [IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

### <a id="MicroEngine_Input_InputSystem_ReleaseAll"></a> ReleaseAll\(\)

Releases every key, button and pointer and forgets which layer took what (focus lost, suspended).

```csharp
public void ReleaseAll()
```

### <a id="MicroEngine_Input_InputSystem_Route_MicroEngine_Input_InputEvent__"></a> Route\(in InputEvent\)

Routes one event now: to the layer that took the press it belongs to, else through the layers from the top, else
into the devices' state and [Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events).

```csharp
public void Route(in InputEvent e)
```

#### Parameters

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputSystem_Rumble_MicroEngine_Input_PlayerInput_System_Single_System_Single_System_Double_"></a> Rumble\(PlayerInput, float, float, double\)

Rumbles every gamepad <code class="paramref">player</code> hears: low-frequency (heavy) and high-frequency (light) motors at
0..1 for <code class="paramref">seconds</code>. Returns false when none could rumble.

```csharp
public bool Rumble(PlayerInput player, float low, float high, double seconds)
```

#### Parameters

`player` [PlayerInput](MicroEngine.Input.PlayerInput.md)

`low` [float](https://learn.microsoft.com/dotnet/api/system.single)

`high` [float](https://learn.microsoft.com/dotnet/api/system.single)

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputSystem_Rumble_MicroEngine_Input_DeviceId_System_Single_System_Single_System_Double_"></a> Rumble\(DeviceId, float, float, double\)

Rumbles one gamepad (see [Rumble](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Rumble_MicroEngine_Input_PlayerInput_System_Single_System_Single_System_Double_)).

```csharp
public bool Rumble(DeviceId pad, float low, float high, double seconds)
```

#### Parameters

`pad` [DeviceId](MicroEngine.Input.DeviceId.md)

`low` [float](https://learn.microsoft.com/dotnet/api/system.single)

`high` [float](https://learn.microsoft.com/dotnet/api/system.single)

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputSystem_Sample"></a> Sample\(\)

Samples every player's actions from the devices they hear and makes the pending events this update's
[Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events). Called by the loop before each fixed update.

```csharp
public void Sample()
```

### <a id="MicroEngine_Input_InputSystem_Send_MicroEngine_Input_InputEvent__"></a> Send\(in InputEvent\)

Queues an event as if a device had produced it; it is routed with the next frame's events, after the backend's.
Scripted runs and tests drive input this way, through the same layers and state as real devices.

```csharp
public void Send(in InputEvent e)
```

#### Parameters

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputSystem_ViewOf_MicroEngine_Input_PlayerInput_"></a> ViewOf\(PlayerInput\)

What <code class="paramref">player</code> hears, read as one polled source: the devices they own and the polled sources
they own, a key or button down when any has it down.

```csharp
public IInputSource ViewOf(PlayerInput player)
```

#### Parameters

`player` [PlayerInput](MicroEngine.Input.PlayerInput.md)

#### Returns

 [IInputSource](MicroEngine.Input.IInputSource.md)

</div>
