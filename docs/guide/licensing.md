# Licensing

MicroEngine is open source under the [MIT License](../../LICENSE). The four demo games built with it are not:
they are published to play and to learn from, with all rights reserved ([demo games notice](../../games/LICENSE)).

## What is under which licence

| Part | Licence | You may |
| --- | --- | --- |
| The engine (`MicroEngine`, its SDL 3 and raylib platforms, the desktop launcher), its tests, the browser and phone hosts, the packaging scripts and tools | MIT | use, modify, fork, redistribute and sell it, keeping the copyright and licence notice |
| The Hello Engine sample | MIT | copy it into your own games |
| This documentation and website code | MIT | the same |
| Meadow Run, Courtyard Key, Trail Duel, Neon Rift: code, levels, characters, names, art and music made for them, and their browser builds and downloads | all rights reserved | play them, keep the copy you downloaded; read and build them to learn MicroEngine; reuse ideas, techniques and mechanics in original games. Not redistribute, sell, or republish them under another name |
| Third-party art and audio in the demos | CC0 1.0, credited on each game's credits page | what CC0 allows (anything) |
| Third-party libraries: SDL 3, ppy.SDL3-CS, NVorbis, raylib, Raylib-cs, the .NET runtime, Emscripten | zlib, MIT | see the [third-party notices](../../THIRD-PARTY-NOTICES.md) |

## Making and selling games

Games you make with MicroEngine are yours: code, art, audio, names and revenue. There are no royalties, no revenue
limits and nothing to register.

When you ship a game, its build contains the engine's assemblies and the native libraries of the platforms it uses
(SDL 3, raylib). The licences ask for one thing: keep their notices with your game. `microengine package` does it for
you: every package has `THIRD-PARTY-NOTICES.txt` and a `licenses/` folder next to the game, and your own licence as
`LICENSE.txt` ([Packaging](packaging.md)).

- MicroEngine: include the MIT License text with its copyright line, for example in a `licenses` folder or on a
  credits screen. You do not have to show "Made with MicroEngine" (it is welcome).
- SDL 3, ppy.SDL3-CS, NVorbis, raylib, Raylib-cs, the .NET runtime and, for browser builds, Emscripten: include their notices. The
  [third-party notices](../../THIRD-PARTY-NOTICES.md) list them, with their licence texts.

## Forks and modified engines

You may fork MicroEngine, change it, and distribute or sell your version, under the MIT License. Keep the original
copyright notice and the licence text in your copies, and do not present the original code as entirely your own.
The demo games are not part of that permission: a fork that includes them must leave them out of its releases.

## Contributing

Contributions are accepted under the MIT License: see the repository's contributing guide.
