# Enum UiSpace

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

The view a [UiRoot](MicroEngine.UI.UiRoot.md) lives in: where its widgets are laid out and drawn, and in which units.

```csharp
public enum UiSpace
```

## Fields

`Canvas = 1` 

The game's canvas: canvas pixels, on whole pixels, as part of the picture the scene draws. The interface of a
pixel-art game, whose menus and boxes have the canvas's pixel size. The scene draws the root from its own
<code>Draw</code> ([Draw](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Draw_MicroEngine_Graphics_IRenderer_)), where it belongs in its picture.



`UiView = 0` 

The UI view (the default): UI units at the window's resolution, drawn by the engine over the world, so text and
shapes stay sharp whatever the canvas's size.

</div>
