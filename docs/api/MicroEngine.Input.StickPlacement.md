# Enum StickPlacement

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

Where a virtual joystick's base is.

```csharp
public enum StickPlacement
```

## Fields

`Fixed = 0` 

Always at its anchored position; a touch must start on it.



`Floating = 1` 

Appears where the thumb lands inside [Zone](MicroEngine.Input.TouchStick.md#MicroEngine_Input_TouchStick_Zone), and follows a thumb that slides past its rim.
