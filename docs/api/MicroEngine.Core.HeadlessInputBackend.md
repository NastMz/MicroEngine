# Class HeadlessInputBackend

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

An input backend tests drive by hand: device events sent with [Send](MicroEngine.Core.HeadlessInputBackend.md#MicroEngine_Core_HeadlessInputBackend_Send_MicroEngine_Input_InputEvent_) are delivered at the next drain, in
order; key labels can be set as a keyboard layout would print them; rumbles and text-input switches are recorded.

```csharp
public sealed class HeadlessInputBackend : IInputBackend, IKeyLabels, IGamepadRumble, ITextInputControl
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessInputBackend](MicroEngine.Core.HeadlessInputBackend.md)

#### Implements

[IInputBackend](MicroEngine.Input.IInputBackend.md), 
[IKeyLabels](MicroEngine.Input.IKeyLabels.md), 
[IGamepadRumble](MicroEngine.Input.IGamepadRumble.md), 
[ITextInputControl](MicroEngine.Input.ITextInputControl.md)

## Constructors

### <a id="MicroEngine_Core_HeadlessInputBackend__ctor"></a> HeadlessInputBackend\(\)

```csharp
public HeadlessInputBackend()
```

## Properties

### <a id="MicroEngine_Core_HeadlessInputBackend_Labels"></a> Labels

The labels [KeyLabel](MicroEngine.Core.HeadlessInputBackend.md#MicroEngine_Core_HeadlessInputBackend_KeyLabel_MicroEngine_Input_Key_) reports, as a layout would print them; keys not in it report null.

```csharp
public Dictionary<Key, string> Labels { get; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[Key](MicroEngine.Input.Key.md), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_HeadlessInputBackend_Rumbles"></a> Rumbles

Every [Rumble](MicroEngine.Core.HeadlessInputBackend.md#MicroEngine_Core_HeadlessInputBackend_Rumble_MicroEngine_Input_DeviceId_System_Single_System_Single_System_Double_) call, in order.

```csharp
public List<(DeviceId Pad, float Low, float High, double Seconds)> Rumbles { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<\([DeviceId](MicroEngine.Input.DeviceId.md) [Pad](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.deviceid,system.single,system.single,system.double\-.pad), [float](https://learn.microsoft.com/dotnet/api/system.single) [Low](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.deviceid,system.single,system.single,system.double\-.low), [float](https://learn.microsoft.com/dotnet/api/system.single) [High](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.deviceid,system.single,system.single,system.double\-.high), [double](https://learn.microsoft.com/dotnet/api/system.double) [Seconds](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.deviceid,system.single,system.single,system.double\-.seconds)\)\>

### <a id="MicroEngine_Core_HeadlessInputBackend_TextInput"></a> TextInput

Whether text input is on, and the caret it was turned on with.

```csharp
public (bool Active, RectF Caret) TextInput { get; }
```

#### Property Value

 \([bool](https://learn.microsoft.com/dotnet/api/system.boolean) [Active](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.boolean,microengine.rectf\-.active), [RectF](MicroEngine.RectF.md) [Caret](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.boolean,microengine.rectf\-.caret)\)

## Methods

### <a id="MicroEngine_Core_HeadlessInputBackend_Drain_System_Collections_Generic_List_MicroEngine_Input_InputEvent__"></a> Drain\(List<InputEvent\>\)

Moves every event since the previous call into <code class="paramref">events</code>, oldest first.

```csharp
public void Drain(List<InputEvent> events)
```

#### Parameters

`events` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[InputEvent](MicroEngine.Input.InputEvent.md)\>

### <a id="MicroEngine_Core_HeadlessInputBackend_KeyLabel_MicroEngine_Input_Key_"></a> KeyLabel\(Key\)

The key's label in the current layout, or null when the backend cannot tell (the US name is used then).

```csharp
public string? KeyLabel(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_HeadlessInputBackend_Rumble_MicroEngine_Input_DeviceId_System_Single_System_Single_System_Double_"></a> Rumble\(DeviceId, float, float, double\)

Records the call; every pad can rumble here.

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

### <a id="MicroEngine_Core_HeadlessInputBackend_Send_MicroEngine_Input_InputEvent_"></a> Send\(InputEvent\)

Queues an event for the next drain.

```csharp
public HeadlessInputBackend Send(InputEvent e)
```

#### Parameters

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [HeadlessInputBackend](MicroEngine.Core.HeadlessInputBackend.md)

### <a id="MicroEngine_Core_HeadlessInputBackend_Start_MicroEngine_RectF_"></a> Start\(RectF\)

Turns text input on; <code class="paramref">caret</code> is where the text being edited is, in virtual canvas pixels, so an
input method can place its candidate list next to it.

```csharp
public void Start(RectF caret)
```

#### Parameters

`caret` [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Core_HeadlessInputBackend_Stop"></a> Stop\(\)

Turns text input off.

```csharp
public void Stop()
```

</div>
