# Enum LoadOp

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

What a pass does with an attachment's previous contents.

```csharp
public enum LoadOp
```

## Fields

`Clear = 1` 

Fill with the clear value; ignores viewport and scissor.



`DontCare = 2` 

Contents undefined at pass start.



`Load = 0` 

Keep the previous contents.

</div>
