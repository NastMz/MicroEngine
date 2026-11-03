# Class ServiceRegistry

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

The engine's services, one instance per type, registered by modules and read by whoever needs them. Typed and
explicit: a service is found by the exact type it was registered as (no base classes, no interfaces it also
implements), nothing is created on demand and nothing is discovered by reflection, so the registry works the same
under trimming, NativeAOT and WebAssembly.

```csharp
public sealed class ServiceRegistry
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ServiceRegistry](MicroEngine.Kernel.ServiceRegistry.md)

## Remarks

Read and written on the game's thread; not thread-safe.

## Constructors

### <a id="MicroEngine_Kernel_ServiceRegistry__ctor"></a> ServiceRegistry\(\)

```csharp
public ServiceRegistry()
```

## Properties

### <a id="MicroEngine_Kernel_ServiceRegistry_Count"></a> Count

The number of registered services.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Kernel_ServiceRegistry_Add__1___0_"></a> Add<T\>\(T\)

Registers <code class="paramref">instance</code> as the service of type <code class="typeparamref">T</code>.

```csharp
public void Add<T>(T instance) where T : class
```

#### Parameters

`instance` T

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">instance</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A service of type <code class="typeparamref">T</code> is already registered; use [Replace``1](MicroEngine.Kernel.ServiceRegistry.md#MicroEngine_Kernel_ServiceRegistry_Replace__1___0_) to change it.

### <a id="MicroEngine_Kernel_ServiceRegistry_Contains__1"></a> Contains<T\>\(\)

Whether a service is registered as <code class="typeparamref">T</code>.

```csharp
public bool Contains<T>() where T : class
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Kernel_ServiceRegistry_Get__1"></a> Get<T\>\(\)

The service registered as <code class="typeparamref">T</code>.

```csharp
public T Get<T>() where T : class
```

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No service of type <code class="typeparamref">T</code> is registered; the message names the type.

### <a id="MicroEngine_Kernel_ServiceRegistry_Remove__1"></a> Remove<T\>\(\)

Removes the service registered as <code class="typeparamref">T</code>; false when there was none.

```csharp
public bool Remove<T>() where T : class
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Kernel_ServiceRegistry_Replace__1___0_"></a> Replace<T\>\(T\)

Registers <code class="paramref">instance</code> as the service of type <code class="typeparamref">T</code>, in place of the one registered before, if any.

```csharp
public T? Replace<T>(T instance) where T : class
```

#### Parameters

`instance` T

#### Returns

 T?

The service it replaced, or null when there was none.

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">instance</code> is null.

### <a id="MicroEngine_Kernel_ServiceRegistry_TryGet__1___0__"></a> TryGet<T\>\(out T?\)

The service registered as <code class="typeparamref">T</code>, if there is one.

```csharp
public bool TryGet<T>(out T? service) where T : class
```

#### Parameters

`service` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>
