# Enum AlphaMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

How the colour channels of texel data relate to alpha.

```csharp
public enum AlphaMode
```

## Fields

`Premultiplied = 0` 

Colour already multiplied by alpha: the engine-wide default, what the built-in blend states assume.



`Straight = 1` 

Colour not multiplied by alpha. The device does not convert: content tools premultiply on import.

</div>
