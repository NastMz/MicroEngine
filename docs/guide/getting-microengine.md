# Getting MicroEngine

MicroEngine is not distributed to external developers yet: there is no package, template or download to install
today. This page states what is planned for distribution, what you can already do as a visitor, and how to read
the rest of the documentation in the meantime.

## Distribution status

The engine is developed in a private repository and used only by its own games and sample. No NuGet package,
project template, installer or release has been published, and no installation procedure exists for external
developers. Instructions that build or run the engine from its source are internal and are not part of this
documentation.

## Planned

The following is planned. None of it is available yet, and no dates are set; progress is tracked on the
[roadmap](/roadmap/).

- NuGet packages for the engine and its desktop backend.
- C# project templates to start a new game.
- Installation without cloning the engine's repository.
- Versioned releases.

With those in place, starting a game is planned to work like any other .NET project: create a project from a
template, reference the engine packages, write the game class and run it. The commands and package names will be
documented when they exist.

## Licence

MicroEngine is proprietary. When the SDK ships it is planned to be free for personal, educational and commercial
games, with no royalties, under the MicroEngine SDK License; your games stay yours. The licence is not published
yet. See [Licensing](licensing.md) for the planned model.

## What you can do now

- Play the four demos in a desktop browser on the [demos page](/demos/). See [Platforms](platforms.md) for the
  browsers they were verified in.
- Read the [engine manual](../manual/lifecycle.md), the [examples](../examples/hello-engine.md) and the
  [reference](../reference/contracts.md) to see how games are written and what the engine provides.
- Check [Status and limitations](status.md) to judge whether it would fit a project of yours.

## About the code in these pages

The rest of the documentation shows real engine code and APIs: the classes, signatures and excerpts come from
the engine and its four games as they are today. Where a page mentions a source path, it is a label for where the
code lives, not a file you can open. Project setup will change once packages exist, so the parts of a page that
describe how a game project references the engine reflect the current, internal setup.
