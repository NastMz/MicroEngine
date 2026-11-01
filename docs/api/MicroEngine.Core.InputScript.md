# Class InputScript

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Testing your game](../examples/testing.md)  

A deterministic input script, keyed by update number. Text format, one command per line:

<pre><code class="lang-csharp"># comment
30 press confirm          # down for 2 updates
60 hold right 90          # down for 90 updates
60 down jump / 75 up jump # explicit edges
200 capture title.png
240 touch 1 dpad 1 0      # finger 1 down on the d-pad, one radius to the right (or moves there)
250 tap 2 jump            # finger 2 taps the "jump" control (lifted 2 updates later)
260 touch 3 0.5,0.5       # finger 3 at the middle of the screen (fractions of its size)
300 lift 1
900 quit</code></pre>

"@+N" instead of an absolute number means N updates after the previous command. Touch commands go through the
game's [VirtualControls](MicroEngine.Input.VirtualControls.md) with the same hit testing as real fingers.

```csharp
public sealed class InputScript
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputScript](MicroEngine.Core.InputScript.md)

## Constructors

### <a id="MicroEngine_Core_InputScript__ctor"></a> InputScript\(\)

```csharp
public InputScript()
```

## Properties

### <a id="MicroEngine_Core_InputScript_QuitUpdate"></a> QuitUpdate

The update after which the script stops the run, or null.

```csharp
public long? QuitUpdate { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)?

## Methods

### <a id="MicroEngine_Core_InputScript_Apply_System_Int64_MicroEngine_Input_InputMap_"></a> Apply\(long, InputMap\)

Applies every edge scheduled for the update about to run.

```csharp
public void Apply(long update, InputMap input)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`input` [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Core_InputScript_ApplyTouches_System_Int64_MicroEngine_Input_VirtualControls_"></a> ApplyTouches\(long, VirtualControls\)

Hands the touches scheduled for the update about to run to the game's touch controls.

```csharp
public void ApplyTouches(long update, VirtualControls controls)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`controls` [VirtualControls](MicroEngine.Input.VirtualControls.md)

### <a id="MicroEngine_Core_InputScript_Capture_System_Int64_System_String_"></a> Capture\(long, string\)

Saves a screenshot named <code class="paramref">fileName</code> after update <code class="paramref">at</code>.

```csharp
public InputScript Capture(long at, string fileName)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`fileName` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_CaptureAt_System_Int64_"></a> CaptureAt\(long\)

The screenshot file name scheduled after <code class="paramref">update</code>, or null.

```csharp
public string? CaptureAt(long update)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_InputScript_Hold_System_Int64_System_String_System_Int64_"></a> Hold\(long, string, long\)

Holds <code class="paramref">action</code> down from update <code class="paramref">at</code> for <code class="paramref">duration</code> updates (at least 1).

```csharp
public InputScript Hold(long at, string action, long duration)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`duration` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Lift_System_Int64_System_Int32_"></a> Lift\(long, int\)

Lifts finger <code class="paramref">finger</code> at update <code class="paramref">at</code>.

```csharp
public InputScript Lift(long at, int finger)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`finger` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Parse_System_String_"></a> Parse\(string\)

Parses the text format described above.

```csharp
public static InputScript Parse(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

A line is invalid; the message names its line number.

### <a id="MicroEngine_Core_InputScript_Press_System_Int64_System_String_"></a> Press\(long, string\)

Presses <code class="paramref">action</code> at update <code class="paramref">at</code> for 2 updates.

```csharp
public InputScript Press(long at, string action)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_QuitAfter_System_Int64_"></a> QuitAfter\(long\)

Stops the run after update <code class="paramref">at</code>.

```csharp
public InputScript QuitAfter(long at)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_QuitAt_System_Int64_"></a> QuitAt\(long\)

Whether the run must stop after <code class="paramref">update</code>.

```csharp
public bool QuitAt(long update)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_InputScript_Touch_System_Int64_System_Int32_System_String_System_Numerics_Vector2_"></a> Touch\(long, int, string, Vector2\)

Puts finger <code class="paramref">finger</code> on a touch control (offset in its radii) at update <code class="paramref">at</code>, or moves it there.

```csharp
public InputScript Touch(long at, int finger, string control, Vector2 offset = default)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`finger` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`control` [string](https://learn.microsoft.com/dotnet/api/system.string)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)
