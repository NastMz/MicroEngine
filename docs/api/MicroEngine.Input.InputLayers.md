# Class InputLayers

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The input layers in routing order: highest <code>order</code> first, layers of equal order in the order they were added.
Events no layer takes become the polled state actions read ([InputDevices](MicroEngine.Input.InputDevices.md)) and the frame's
[Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events).

```csharp
public sealed class InputLayers
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputLayers](MicroEngine.Input.InputLayers.md)

## Constructors

### <a id="MicroEngine_Input_InputLayers__ctor"></a> InputLayers\(\)

```csharp
public InputLayers()
```

## Properties

### <a id="MicroEngine_Input_InputLayers_Count"></a> Count

The number of layers.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputLayers_InOrder"></a> InOrder

The layers, in the order events reach them.

```csharp
public IReadOnlyList<IInputLayer> InOrder { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IInputLayer](MicroEngine.Input.IInputLayer.md)\>

## Methods

### <a id="MicroEngine_Input_InputLayers_Add_MicroEngine_Input_IInputLayer_System_Int32_"></a> Add\(IInputLayer, int\)

Adds <code class="paramref">layer</code> at <code class="paramref">order</code> (see [InputLayerOrder](MicroEngine.Input.InputLayerOrder.md)); adding it again moves it.

```csharp
public void Add(IInputLayer layer, int order = 100)
```

#### Parameters

`layer` [IInputLayer](MicroEngine.Input.IInputLayer.md)

`order` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">layer</code> is null.

### <a id="MicroEngine_Input_InputLayers_Contains_MicroEngine_Input_IInputLayer_"></a> Contains\(IInputLayer\)

Whether <code class="paramref">layer</code> was added.

```csharp
public bool Contains(IInputLayer layer)
```

#### Parameters

`layer` [IInputLayer](MicroEngine.Input.IInputLayer.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputLayers_Remove_MicroEngine_Input_IInputLayer_"></a> Remove\(IInputLayer\)

Removes <code class="paramref">layer</code>; false when it was not added.

```csharp
public bool Remove(IInputLayer layer)
```

#### Parameters

`layer` [IInputLayer](MicroEngine.Input.IInputLayer.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
