# Namespace MicroEngine.UI

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

A queue of dialogue pages shown in a panel with a typewriter and a speaker name tag.
Games call [Update](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Update_System_Single_MicroEngine_Input_InputMap_System_String_) and [Draw](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_MicroEngine_RectF_) from their own scene.

 [SelectionList](MicroEngine.UI.SelectionList.md)

A vertical list of options navigated with up/down and chosen with confirm.

 [Typewriter](MicroEngine.UI.Typewriter.md)

Reveals text character by character. Confirm skips to the end; a second confirm advances.

 [UiTheme](MicroEngine.UI.UiTheme.md)

Visual theme for the shared widgets so each game can keep its own identity.
