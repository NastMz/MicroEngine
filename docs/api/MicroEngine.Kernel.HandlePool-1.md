# Class HandlePool<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

Items reached through generational [Handle`1](MicroEngine.Kernel.md)s: slots are reused, and a handle to a removed item is
detected (stale) instead of reaching the item that took its slot.

```csharp
public sealed class HandlePool<T>
```

#### Type Parameters

`T` 

The items.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HandlePool<T\>](MicroEngine.Kernel.HandlePool\-1.md)

## Remarks

Not thread-safe.

## Constructors

### <a id="MicroEngine_Kernel_HandlePool_1__ctor"></a> HandlePool\(\)

```csharp
public HandlePool()
```

## Properties

### <a id="MicroEngine_Kernel_HandlePool_1_Count"></a> Count

The number of live items.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Kernel_HandlePool_1_Add__0_"></a> Add\(T\)

Stores <code class="paramref">item</code> and returns its handle; a free slot is reused with a new generation.

```csharp
public Handle<T> Add(T item)
```

#### Parameters

`item` T

#### Returns

 [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

### <a id="MicroEngine_Kernel_HandlePool_1_Contains_MicroEngine_Kernel_Handle__0__"></a> Contains\(Handle<T\>\)

Whether <code class="paramref">handle</code> refers to a live item.

```csharp
public bool Contains(Handle<T> handle)
```

#### Parameters

`handle` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_HandlePool_1_Get_MicroEngine_Kernel_Handle__0__"></a> Get\(Handle<T\>\)

The item of <code class="paramref">handle</code>.

```csharp
public T Get(Handle<T> handle)
```

#### Parameters

`handle` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

#### Returns

 T

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The handle is [None](MicroEngine.Kernel.md), was never issued by this pool, or its item was removed (stale); the message says which.

### <a id="MicroEngine_Kernel_HandlePool_1_Remove_MicroEngine_Kernel_Handle__0__"></a> Remove\(Handle<T\>\)

Removes the item of <code class="paramref">handle</code>; every handle to it becomes stale. False when it was not live.

```csharp
public bool Remove(Handle<T> handle)
```

#### Parameters

`handle` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_HandlePool_1_TryGet_MicroEngine_Kernel_Handle__0___0__"></a> TryGet\(Handle<T\>, out T\)

The item of <code class="paramref">handle</code>, if it is still live.

```csharp
public bool TryGet(Handle<T> handle, out T item)
```

#### Parameters

`handle` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

`item` T

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
