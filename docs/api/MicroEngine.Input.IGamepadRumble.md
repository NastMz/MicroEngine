# Interface IGamepadRumble

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

Gamepad vibration, a capability of input backends whose pads can rumble.

```csharp
public interface IGamepadRumble
```

## Methods

### <a id="MicroEngine_Input_IGamepadRumble_Rumble_MicroEngine_Input_DeviceId_System_Single_System_Single_System_Double_"></a> Rumble\(DeviceId, float, float, double\)

Rumbles <code class="paramref">pad</code>'s low-frequency (left, heavy) and high-frequency (right, light) motors at 0..1 for
<code class="paramref">seconds</code>; 0, 0 stops it. Returns false when the pad cannot rumble.

```csharp
bool Rumble(DeviceId pad, float low, float high, double seconds)
```

#### Parameters

`pad` [DeviceId](MicroEngine.Input.DeviceId.md)

`low` [float](https://learn.microsoft.com/dotnet/api/system.single)

`high` [float](https://learn.microsoft.com/dotnet/api/system.single)

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
