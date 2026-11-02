# Namespace MicroEngine.UI

<div data-pagefind-weight="0.3">

Pixel-art widgets: dialogue boxes, menus, typewriter text, themes, achievement banners and list.

Guide: [UI widgets](../manual/ui.md)  

### Classes

 [AchievementToasts](MicroEngine.UI.AchievementToasts.md)

Queued "achievement unlocked" banners that slide in at the top of the screen, one at a time.
Purely visual: they never read input, so play continues underneath. [Game](MicroEngine.Core.Game.md) owns one
when achievements are enabled and draws it above every scene.

 [AchievementsScene](MicroEngine.UI.AchievementsScene.md)

Overlay listing a game's achievements: unlocked ones highlighted, pending ones dimmed, progress bars
for progressive goals. Up/down moves the cursor (the list scrolls); any of [CloseActions](MicroEngine.UI.AchievementsScene.md#MicroEngine_UI_AchievementsScene_CloseActions) closes it.

 [DialogueBox](MicroEngine.UI.DialogueBox.md)

A queue of dialogue pages shown in a panel with a typewriter and a speaker name tag. A page longer than
[Lines](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Lines) wrapped lines continues on the next confirm, so no text is ever cut. With [Prompts](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Prompts)
set, <code>{action}</code> in a page names the control that presses it on the player's device, and follows a device change
while the page is showing. Games call [Update](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Update_System_Single_MicroEngine_Input_InputMap_System_String_) and [Draw](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IFont_MicroEngine_RectF_) from their own scene.

 [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)

Colours of prompt glyphs ([PromptText](MicroEngine.UI.PromptText.md)): key caps, gamepad buttons and touch controls.

 [PromptText](MicroEngine.UI.PromptText.md)

Draws text with action prompts as glyphs: <code>"Press {attack} to swing"</code> shows a key cap with Z on a keyboard, a
green A on an Xbox pad, a × button on a PlayStation pad and the touch button's label in a ring on a phone. Text that
does not name an action is drawn as with [Draw](MicroEngine.Graphics.BitmapFont.md#MicroEngine_Graphics_BitmapFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_System_Single_). Glyphs are as tall as a line of the font.

 [SelectionList](MicroEngine.UI.SelectionList.md)

A vertical list of options navigated with up/down and chosen with confirm.

 [Typewriter](MicroEngine.UI.Typewriter.md)

Reveals text character by character. Confirm skips to the end; a second confirm advances.

 [UiTheme](MicroEngine.UI.UiTheme.md)

Visual theme for the shared widgets so each game can keep its own identity.

</div>
