using System.Runtime.CompilerServices;

// The touch screen contract and the on-screen controls moved to MicroEngine.Input.Touch.dll (carried by the MicroEngine
// package) with their namespace, so touch controls are a module of their own; code compiled against an earlier
// MicroEngine.dll still finds them here.
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchPhase))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchEvent))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.ITouchScreen))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.ITouchOverlay))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.IOverlayCanvas))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.FrameLayout))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchShape))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchStyle))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchControlView))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.ITouchSkin))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.DefaultTouchSkin))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.OverlayShapes))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchAnchor))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchVisibility))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.StickPlacement))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchButtonMode))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchControl))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchDPad))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchStick))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchButton))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.TouchLayout))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Input.VirtualControls))]
