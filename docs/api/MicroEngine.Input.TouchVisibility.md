# Enum TouchVisibility

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

When on-screen controls are drawn.

```csharp
public enum TouchVisibility
```

## Fields

`Always = 1` 

Drawn on every screen with touch, whatever device plays.



`Auto = 0` 

On touch-first screens, and elsewhere after the first touch; hidden while a keyboard or gamepad plays, back on the next touch.



`Never = 2` 

Never drawn, and touches are ignored.
