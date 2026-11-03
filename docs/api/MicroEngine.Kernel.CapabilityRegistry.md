# Class CapabilityRegistry

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

Optional features something in the engine offers (a display that can change its mode, a touch screen, platform
diagnostics), declared by whoever provides them and queried by type. A feature that is absent is simply not
declared: callers ask with [TryGet``1](MicroEngine.Kernel.CapabilityRegistry.md#MicroEngine_Kernel_CapabilityRegistry_TryGet__1___0__) instead of casting a platform or device to find out.

```csharp
public sealed class CapabilityRegistry
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CapabilityRegistry](MicroEngine.Kernel.CapabilityRegistry.md)

## Remarks

Read and written on the game's thread; not thread-safe.

## Constructors

### <a id="MicroEngine_Kernel_CapabilityRegistry__ctor"></a> CapabilityRegistry\(\)

```csharp
public CapabilityRegistry()
```

## Properties

### <a id="MicroEngine_Kernel_CapabilityRegistry_Count"></a> Count

The number of declared capabilities.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Kernel_CapabilityRegistry_Add__1___0_"></a> Add<T\>\(T\)

Declares <code class="paramref">capability</code> as the capability of type <code class="typeparamref">T</code>.

```csharp
public void Add<T>(T capability) where T : class
```

#### Parameters

`capability` T

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">capability</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A capability of type <code class="typeparamref">T</code> is already declared; use [Replace``1](MicroEngine.Kernel.CapabilityRegistry.md#MicroEngine_Kernel_CapabilityRegistry_Replace__1___0_).

### <a id="MicroEngine_Kernel_CapabilityRegistry_Has__1"></a> Has<T\>\(\)

Whether a capability is declared as <code class="typeparamref">T</code>.

```csharp
public bool Has<T>() where T : class
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Kernel_CapabilityRegistry_Remove__1"></a> Remove<T\>\(\)

Withdraws the capability declared as <code class="typeparamref">T</code>; false when there was none.

```csharp
public bool Remove<T>() where T : class
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Kernel_CapabilityRegistry_Replace__1___0_"></a> Replace<T\>\(T\)

Declares <code class="paramref">capability</code> as the capability of type <code class="typeparamref">T</code>, in place of the one declared before, if any.

```csharp
public void Replace<T>(T capability) where T : class
```

#### Parameters

`capability` T

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">capability</code> is null.

### <a id="MicroEngine_Kernel_CapabilityRegistry_TryGet__1___0__"></a> TryGet<T\>\(out T?\)

The capability declared as <code class="typeparamref">T</code>, if there is one.

```csharp
public bool TryGet<T>(out T? capability) where T : class
```

#### Parameters

`capability` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>
