# Enum PresentMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

When [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present) waits for the display.

```csharp
public enum PresentMode
```

## Fields

`Immediate = 1` 

Do not wait: may tear.



`Mailbox = 2` 

Do not wait, replace the queued frame: no tearing, lowest latency where available.



`VSync = 0` 

Wait for vertical blank: no tearing.

</div>
