# Namespace MicroEngine.UI

<div data-pagefind-weight="0.3">

Widgets: labels, buttons, panels, stacks, text fields, dialogue boxes, menus, typewriter text, toasts, themes, focus and the UI input layer.

Guide: [UI widgets](../manual/ui.md)  

### Classes

 [AchievementToasts](MicroEngine.UI.AchievementToasts.md)

Queued "achievement unlocked" banners that slide in at the top of the screen, one at a time: the UI's
[Toasts](MicroEngine.UI.Toasts.md) showing achievements. Purely visual: they never read input, so play continues underneath.
[Game](MicroEngine.Core.Game.md) owns one when achievements are enabled and draws it above every scene.

 [AchievementsScene](MicroEngine.UI.AchievementsScene.md)

Overlay listing a game's achievements: unlocked ones highlighted, pending ones dimmed, progress bars
for progressive goals. Up/down moves the cursor (the list scrolls); any of [CloseActions](MicroEngine.UI.AchievementsScene.md#MicroEngine_UI_AchievementsScene_CloseActions) closes it.

 [Button](MicroEngine.UI.Button.md)

A button: confirm while it has the focus, or a click (a press and release of the pointer on it), raises
[Clicked](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Clicked). It takes the focus.

 [DialogueBox](MicroEngine.UI.DialogueBox.md)

A queue of dialogue pages shown in a panel with a typewriter and a speaker name tag. A page longer than
[Lines](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Lines) wrapped lines continues on the next confirm, so no text is ever cut. With [Prompts](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Prompts)
set, <code>{action}</code> in a page names the control that presses it on the player's device, and follows a device change
while the page is showing. Games call [Update](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Update_System_Single_MicroEngine_Input_InputMap_System_String_) and [Draw](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IFont_MicroEngine_RectF_) from their own scene.

 [Grid](MicroEngine.UI.Grid.md)

Children in rows of [Columns](MicroEngine.UI.Grid.md#MicroEngine_UI_Grid_Columns) equal cells, left to right then top to bottom: an inventory, a level select,
a character roster, a keyboard on screen. Every cell is as large as the largest child; hidden children take no cell.
Focus moves between cells by direction, as it does anywhere in the tree.

 [Label](MicroEngine.UI.Label.md)

Text that does nothing: a heading, a value, a hint, a line of dialogue. Lines are separated by <code>\n</code>; with
[Wrap](MicroEngine.UI.Label.md#MicroEngine_UI_Label_Wrap) and a fixed [Width](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Width) the text also breaks between words to fit. Set
[VisibleCharacters](MicroEngine.UI.Label.md#MicroEngine_UI_Label_VisibleCharacters) to reveal it a little at a time: the lines stay where the whole text puts them.

 [Layer](MicroEngine.UI.Layer.md)

The whole UI view, with each child placed on its own: against an edge or a corner (a banner in a corner, a HUD
block), or at a point (a speech bubble over a character, a damage number, a marker that follows something in the
world). Children keep the size they measure; later children draw over earlier ones. Where no child is, the pointer
passes through to gameplay. Use it as a root's [Content](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Content).

 [Panel](MicroEngine.UI.Panel.md)

A box behind one child, with padding around it: the frame of a menu, a dialog or a HUD block.

 [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)

Colours of prompt glyphs ([PromptText](MicroEngine.UI.PromptText.md)): key caps, gamepad buttons and touch controls.

 [PromptText](MicroEngine.UI.PromptText.md)

Draws text with action prompts as glyphs: <code>"Press {attack} to swing"</code> shows a key cap with Z on a keyboard, a
green A on an Xbox pad, a × button on a PlayStation pad and the touch button's label in a ring on a phone. Text that
does not name an action is drawn as with [Draw](MicroEngine.Graphics.BitmapFont.md#MicroEngine_Graphics_BitmapFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_System_Single_). Glyphs are as tall as a line of the font.

 [ScrollView](MicroEngine.UI.ScrollView.md)

A window onto content longer than the space it has: a long menu, a list of saves, an achievement list, the credits.
Give it a fixed [Height](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Height) (or [Width](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Width) when [Orientation](MicroEngine.UI.ScrollView.md#MicroEngine_UI_ScrollView_Orientation) is
horizontal); its content keeps the size it measures and is cut at the view's edges. It scrolls to show a widget
inside it when that widget takes the focus, with the wheel over it, and with a drag, also one that starts on a button
inside it (a finger on a phone). A thin bar on its edge shows where the window is.

 [SelectionList](MicroEngine.UI.SelectionList.md)

A vertical list of options navigated with up/down and chosen with confirm.

 [Stack](MicroEngine.UI.Stack.md)

Children placed one after another, top to bottom or left to right, with a space between them: menus, rows of buttons, forms.

 [TextField](MicroEngine.UI.TextField.md)

One line of editable text: a name, a seed, a chat message. While it has the focus it turns the input system's text
channel on (the on-screen keyboard on a phone, the input method's candidates next to the caret), inserts what is
typed at the caret, shows what an input method is still composing, and takes the keyboard from gameplay: Backspace,
Delete, the arrows, Home and End edit, Enter submits, Escape leaves the field, Tab and the up and down keys move the
focus. A gamepad edits nothing but moves the caret with left and right and submits with confirm.

 [Toast](MicroEngine.UI.Toast.md)

One banner of [Toasts](MicroEngine.UI.Toasts.md): a heading, a title, an optional description and an optional icon.

 [Toasts](MicroEngine.UI.Toasts.md)

Queued banners that slide in at the top of the screen (or the bottom), one at a time: unlocked achievements, saves,
notifications. Purely visual: they never read input, so play continues underneath. Call [Update](MicroEngine.UI.Toasts.md#MicroEngine_UI_Toasts_Update_System_Single_) every
update and [Draw](MicroEngine.UI.Toasts.md#MicroEngine_UI_Toasts_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IFont_) after the scenes.

 [Typewriter](MicroEngine.UI.Typewriter.md)

Reveals text character by character. Confirm skips to the end; a second confirm advances.

 [UiActions](MicroEngine.UI.UiActions.md)

The actions engine widgets answer to. A member left null (the default) is the input map's own name for it
([Names](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Names)), read at every update: a game that renames <code>confirm</code> with
[ActionNames](MicroEngine.Input.ActionNames.md) keeps working widgets, and a gameplay action that happens to be called <code>confirm</code>
never presses a button. Set a member to give the widgets an action of the game's own.

 [UiRoot](MicroEngine.UI.UiRoot.md)

Shows one widget tree in the UI view and drives it: lays [Content](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Content) out in UI units at
[Anchor](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Anchor), keeps the focus, moves it with the engine's direction actions, gives the focused widget confirm
and cancel, and, as the highest input layer ([UI](MicroEngine.Input.InputLayerOrder.md#MicroEngine_Input_InputLayerOrder_UI)), takes pointer presses that land on
the content and, while a text field has the focus, the keyboard. What it takes never reaches gameplay: a press on a
button is not a press in the world, and the actions it acts on are consumed ([Consume](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Consume_System_String_)), so the
scene's update, which runs after the root's, does not see them. A scene has one in <code>Scene.Widgets</code>, which the engine
updates, draws and routes input to; a root can also be driven by hand ([Update](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Update_System_Single_), [Draw](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Draw_MicroEngine_Graphics_IRenderer_),
[Handle](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Handle_MicroEngine_Input_InputEvent__)).

 [UiTheme](MicroEngine.UI.UiTheme.md)

Visual theme for the shared widgets so each game can keep its own identity.

 [Widget](MicroEngine.UI.Widget.md)

One element of a user interface, laid out in UI units in the UI view (window pixels divided by the presentation's UI
scale) by its [UiRoot](MicroEngine.UI.UiRoot.md): it says how large it wants to be ([Measure](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Measure_MicroEngine_UI_UiContext_)), is given its
[Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds) ([Arrange](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Arrange_MicroEngine_RectF_MicroEngine_UI_UiContext_)) and draws itself there ([Draw](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_)). Focusable widgets take the
focus, act on the engine's actions while they have it and on the pointer. Derive from it for a widget of a game's own.

### Structs

 [FocusChange](MicroEngine.UI.FocusChange.md)

A move of the focus, as [FocusChanged](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_FocusChanged) reports it.

 [UiClip](MicroEngine.UI.UiClip.md)

A clip set by [ClipTo](MicroEngine.UI.UiContext.md#MicroEngine_UI_UiContext_ClipTo_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_UI_UiContext__); disposing it restores the clip that was set before.

 [UiContext](MicroEngine.UI.UiContext.md)

What widgets measure and draw with: the root's font and theme, the layout unit and the root's clock.

### Enums

 [Align](MicroEngine.UI.Align.md)

Where something is placed in the space it is given.

 [Anchor](MicroEngine.UI.Anchor.md)

A point of the UI view a [UiRoot](MicroEngine.UI.UiRoot.md) places its content at.

 [Orientation](MicroEngine.UI.Orientation.md)

The direction a [Stack](MicroEngine.UI.Stack.md) places its children in.

 [UiAction](MicroEngine.UI.UiAction.md)

What a widget is asked to do by the engine's actions ([UiActions](MicroEngine.UI.UiActions.md)): move, confirm or cancel.

</div>
