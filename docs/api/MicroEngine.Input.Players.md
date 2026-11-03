# Class Players

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The local players. With no setup there is one player who owns every device, so a single-player game never deals
with players at all (<code>Game.Input</code> is player 1's map). For local multiplayer, pair devices explicitly: a
device paired with a player is heard only by that player, and once player 1 has a device of their own they stop
hearing the devices nobody was given.

```csharp
public sealed class Players : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Players](MicroEngine.Input.Players.md)

#### Implements

IStateHashed

## Examples

```csharp
// Player 1 plays on the keyboard; anyone pressing a button on a free pad joins as the next player.
Players[0].Pair(keyboard);
Players.UnpairedPress += (device, e) => { if (Players.Count < 4) Players.Join(device); };
var p2 = Players[1].Map.Vector("left", "right", "up", "down");
```

## Remarks

Who hears a device ([OwnerOf](MicroEngine.Input.Players.md#MicroEngine_Input_Players_OwnerOf_MicroEngine_Input_DeviceId_)): the player it is paired with; else player 1, while player 1 has no
device of their own; else nobody (it is free to join: [UnpairedPress](MicroEngine.Input.Players.md#MicroEngine_Input_Players_UnpairedPress)). So keyboard and gamepad never
cancel each other across players, and a pad that was never assigned does not drive anyone by accident.

## Constructors

### <a id="MicroEngine_Input_Players__ctor_MicroEngine_Input_InputMap_"></a> Players\(InputMap\)

Creates the players with player 1 using <code class="paramref">first</code>.

```csharp
public Players(InputMap first)
```

#### Parameters

`first` [InputMap](MicroEngine.Input.InputMap.md)

## Properties

### <a id="MicroEngine_Input_Players_All"></a> All

The players in order.

```csharp
public IReadOnlyList<PlayerInput> All { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PlayerInput](MicroEngine.Input.PlayerInput.md)\>

### <a id="MicroEngine_Input_Players_Count"></a> Count

The number of players (at least 1).

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_Players_Item_System_Int32_"></a> this\[int\]

The player at <code class="paramref">index</code> (0 is player 1).

```csharp
public PlayerInput this[int index] { get; }
```

#### Property Value

 [PlayerInput](MicroEngine.Input.PlayerInput.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

There is no such player.

## Methods

### <a id="MicroEngine_Input_Players_Add_MicroEngine_Input_InputMap_"></a> Add\(InputMap\)

Adds a player with <code class="paramref">map</code> and no device yet (pair them with [Pair](MicroEngine.Input.PlayerInput.md#MicroEngine_Input_PlayerInput_Pair_MicroEngine_Input_DeviceId_)).

```csharp
public PlayerInput Add(InputMap map)
```

#### Parameters

`map` [InputMap](MicroEngine.Input.InputMap.md)

#### Returns

 [PlayerInput](MicroEngine.Input.PlayerInput.md)

### <a id="MicroEngine_Input_Players_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the players beyond player 1 (their sampled actions) and every pairing to the state hash; with one player
who has no pairing it adds nothing, so a single-player game's hash is player 1's map alone.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` StateHasher

### <a id="MicroEngine_Input_Players_IsPaired_MicroEngine_Input_DeviceId_"></a> IsPaired\(DeviceId\)

Whether <code class="paramref">device</code> is paired with any player.

```csharp
public bool IsPaired(DeviceId device)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_Players_Join_MicroEngine_Input_DeviceId_MicroEngine_Input_InputMap_"></a> Join\(DeviceId, InputMap?\)

Adds a player paired with <code class="paramref">device</code> (taken from whoever had it), with their own actions:
<code class="paramref">map</code>, or a copy of player 1's bindings.

```csharp
public PlayerInput Join(DeviceId device, InputMap? map = null)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`map` [InputMap](MicroEngine.Input.InputMap.md)?

#### Returns

 [PlayerInput](MicroEngine.Input.PlayerInput.md)

### <a id="MicroEngine_Input_Players_OwnerOf_MicroEngine_Input_DeviceId_"></a> OwnerOf\(DeviceId\)

The player who hears <code class="paramref">device</code>: the one it is paired with; else player 1 while player 1 has no
device of their own; else null.

```csharp
public PlayerInput? OwnerOf(DeviceId device)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [PlayerInput](MicroEngine.Input.PlayerInput.md)?

### <a id="MicroEngine_Input_Players_Pair_MicroEngine_Input_PlayerInput_MicroEngine_Input_DeviceId_"></a> Pair\(PlayerInput, DeviceId\)

Pairs <code class="paramref">device</code> with <code class="paramref">player</code>, taking it from whoever had it.

```csharp
public void Pair(PlayerInput player, DeviceId device)
```

#### Parameters

`player` [PlayerInput](MicroEngine.Input.PlayerInput.md)

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Input_Players_Remove_MicroEngine_Input_PlayerInput_"></a> Remove\(PlayerInput\)

Removes a player other than player 1 (the next ones move up); their devices become free.

```csharp
public bool Remove(PlayerInput player)
```

#### Parameters

`player` [PlayerInput](MicroEngine.Input.PlayerInput.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">player</code> is player 1.

### <a id="MicroEngine_Input_Players_UnpairedPress"></a> UnpairedPress

Raised when a device nobody hears presses something (a key, a gamepad button, a touch): the moment to let that
device join ([Join](MicroEngine.Input.Players.md#MicroEngine_Input_Players_Join_MicroEngine_Input_DeviceId_MicroEngine_Input_InputMap_)). Raised during the frame's input routing.

```csharp
public event Action<DeviceId, InputEvent>? UnpairedPress
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-2)<[DeviceId](MicroEngine.Input.DeviceId.md), [InputEvent](MicroEngine.Input.InputEvent.md)\>?

</div>
