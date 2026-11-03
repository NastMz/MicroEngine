# Enum Key

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

Physical keys, named by their position on a US keyboard (a scancode, not a character): [A](MicroEngine.Input.Key.md) is the key
left of [S](MicroEngine.Input.Key.md) whatever the layout prints on it, so WASD movement stays in place on AZERTY or Dvorak. Ask
[IKeyLabels](MicroEngine.Input.IKeyLabels.md) (or <code>ActionPrompts</code>) for the label the player's layout shows. Backends map their own
codes onto these; the set covers a full-size keyboard.

```csharp
public enum Key
```

## Fields

`A = 13` 

`Apostrophe = 79` 

`B = 14` 

`Backslash = 77` 

`Backspace = 4` 

`C = 15` 

`CapsLock = 85` 

`Comma = 81` 

`D = 16` 

`D0 = 39` 

`D1 = 40` 

`D2 = 41` 

`D3 = 42` 

`D4 = 43` 

`D5 = 44` 

`D6 = 45` 

`D7 = 46` 

`D8 = 47` 

`D9 = 48` 

`Delete = 93` 

`Down = 12` 

`E = 17` 

`End = 95` 

`Enter = 2` 

`Equals = 74` 

The key left of Backspace (= on US layouts).



`Escape = 3` 

`F = 18` 

`F1 = 49` 

`F10 = 60` 

`F11 = 54` 

`F12 = 55` 

`F13 = 61` 

`F14 = 62` 

`F15 = 63` 

`F16 = 64` 

`F17 = 65` 

`F18 = 66` 

`F19 = 67` 

`F2 = 50` 

`F20 = 68` 

`F21 = 69` 

`F22 = 70` 

`F23 = 71` 

`F24 = 72` 

`F3 = 51` 

`F4 = 52` 

`F5 = 53` 

`F6 = 56` 

`F7 = 57` 

`F8 = 58` 

`F9 = 59` 

`G = 19` 

`Grave = 80` 

The key left of 1 (` on US layouts).



`H = 20` 

`Home = 94` 

`I = 21` 

`Insert = 92` 

`J = 22` 

`K = 23` 

`Keypad0 = 102` 

`Keypad1 = 103` 

`Keypad2 = 104` 

`Keypad3 = 105` 

`Keypad4 = 106` 

`Keypad5 = 107` 

`Keypad6 = 108` 

`Keypad7 = 109` 

`Keypad8 = 110` 

`Keypad9 = 111` 

`KeypadDivide = 112` 

`KeypadEnter = 116` 

`KeypadEquals = 118` 

`KeypadMinus = 114` 

`KeypadMultiply = 113` 

`KeypadPeriod = 117` 

`KeypadPlus = 115` 

`L = 24` 

`Left = 9` 

`LeftAlt = 86` 

`LeftBracket = 75` 

`LeftControl = 8` 

`LeftShift = 6` 

`LeftSuper = 89` 

The left Windows, Command or Super key.



`M = 25` 

`Menu = 91` 

The context-menu key.



`Minus = 73` 

The key right of 0 (- on US layouts).



`N = 26` 

`NonUsBackslash = 84` 

The extra key next to the left Shift of ISO keyboards (&lt; &gt; on many European layouts).



`None = 0` 

`NumLock = 101` 

`O = 27` 

`P = 28` 

`PageDown = 97` 

`PageUp = 96` 

`Pause = 100` 

`Period = 82` 

`PrintScreen = 98` 

`Q = 29` 

`R = 30` 

`Right = 10` 

`RightAlt = 87` 

`RightBracket = 76` 

`RightControl = 88` 

`RightShift = 7` 

`RightSuper = 90` 

The right Windows, Command or Super key.



`S = 31` 

`ScrollLock = 99` 

`Semicolon = 78` 

`Slash = 83` 

`Space = 1` 

`T = 32` 

`Tab = 5` 

`U = 33` 

`Up = 11` 

`V = 34` 

`W = 35` 

`X = 36` 

`Y = 37` 

`Z = 38`

</div>
