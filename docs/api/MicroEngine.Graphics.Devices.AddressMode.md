# Enum AddressMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

What sampling does outside 0..1.

```csharp
public enum AddressMode
```

## Fields

`Clamp = 0` 

Clamp to the edge texel. Every tier.



`Mirror = 2` 

Mirror every other repetition; requires [AddressMirror](MicroEngine.Graphics.Devices.GraphicsFeatures.md).



`Repeat = 1` 

Wrap around; requires [AddressRepeat](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

</div>
