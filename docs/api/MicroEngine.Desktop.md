# Namespace MicroEngine.Desktop

<div data-pagefind-weight="0.3">

The desktop launcher with the official desktop platforms (SDL 3, raylib, SDL software, headless).

Guide: [Backends and platforms](../manual/backends.md)  

### Classes

 [DesktopLauncher](MicroEngine.Desktop.DesktopLauncher.md)

Standard entry point for desktop games: [GameLauncher](MicroEngine.Core.GameLauncher.md) with the official desktop platforms, in order of
preference: <code>sdl</code> (the default on Windows, Linux and macOS), <code>raylib</code>, <code>sdl-software</code> (SDL's CPU
renderer, for machines without a usable GPU such as virtual machines and CI runners) and <code>headless</code>. A game's
<code>Program.cs</code> is one line; choosing a platform is the player's or the test's business (<code>--platform</code>,
<code>MICROENGINE_PLATFORM</code>), never the game's.

</div>
