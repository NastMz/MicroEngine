# Class PlayerInput

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

One local player: an action map of their own ([Map](MicroEngine.Input.PlayerInput.md#MicroEngine_Input_PlayerInput_Map)) read from the devices paired with them.

```csharp
public sealed class PlayerInput
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PlayerInput](MicroEngine.Input.PlayerInput.md)

## Properties

### <a id="MicroEngine_Input_PlayerInput_Devices"></a> Devices

The devices paired with this player, in id order. Empty for player 1 until something is paired with them.

```csharp
public IReadOnlyList<DeviceId> Devices { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[DeviceId](MicroEngine.Input.DeviceId.md)\>

### <a id="MicroEngine_Input_PlayerInput_Index"></a> Index

The player's position, 0-based: player 1 is 0.

```csharp
public int Index { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_PlayerInput_Map"></a> Map

The player's actions, sampled once per update from their devices.

```csharp
public InputMap Map { get; }
```

#### Property Value

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Input_PlayerInput_Owns"></a> Owns

Whether this player hears a device (see [OwnerOf](MicroEngine.Input.Players.md#MicroEngine_Input_Players_OwnerOf_MicroEngine_Input_DeviceId_)).

```csharp
public Func<DeviceId, bool> Owns { get; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[DeviceId](MicroEngine.Input.DeviceId.md), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

## Methods

### <a id="MicroEngine_Input_PlayerInput_Pair_MicroEngine_Input_DeviceId_"></a> Pair\(DeviceId\)

Pairs <code class="paramref">device</code> with this player, taking it from whoever had it.

```csharp
public PlayerInput Pair(DeviceId device)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [PlayerInput](MicroEngine.Input.PlayerInput.md)

### <a id="MicroEngine_Input_PlayerInput_Unpair_MicroEngine_Input_DeviceId_"></a> Unpair\(DeviceId\)

Ends the pairing of <code class="paramref">device</code> with this player; false when it was not paired with them.

```csharp
public bool Unpair(DeviceId device)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
