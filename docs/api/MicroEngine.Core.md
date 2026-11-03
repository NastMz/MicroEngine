# Namespace MicroEngine.Core

<div data-pagefind-weight="0.3">

The game and its loop: `Game`, settings, runners and sessions, launchers and platform composition, scripted input, saves, content files, achievements, application identity, the headless platform.

Guide: [Game lifecycle and time](../manual/lifecycle.md)  

### Classes

 [Achievement](MicroEngine.Core.Achievement.md)

One entry of a game's achievement catalog. [Id](MicroEngine.Core.Achievement.md#MicroEngine_Core_Achievement_Id) is the stable key stored in saves, so
names and descriptions can change freely. [Goal](MicroEngine.Core.Achievement.md#MicroEngine_Core_Achievement_Goal) of 1 is an immediate achievement;
larger goals are progressive ("win 10 battles").

 [AchievementRecord](MicroEngine.Core.AchievementRecord.md)

The saved state of one achievement.

 [AchievementSaveData](MicroEngine.Core.AchievementSaveData.md)

What is written to the save slot. Entries are keyed by achievement id.

 [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

Owns a game's achievements: the catalog, progress, unlocking and persistence. Games decide when
something happened and report it with [Unlock](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_Unlock_System_String_), [AddProgress](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_AddProgress_System_String_System_Int32_) or
[SetProgress](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_SetProgress_System_String_System_Int32_); the tracker guarantees that an achievement unlocks (and raises
[Unlocked](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_Unlocked)) exactly once, across sessions.

 [AppIdentity](MicroEngine.Core.AppIdentity.md)

The application identity a game declares once, in its project file (docs/guide/app-identity.md):
<code>ApplicationTitle</code>, <code>ApplicationId</code>, <code>Version</code>, <code>MicroEngineIcon</code> and
<code>MicroEngineOrientation</code>. The MicroEngine build targets write it into the game assembly; packaging turns it into
each system's metadata, and platforms read it at run time (application name, window icon, phone orientation).

 [AssetBatch](MicroEngine.Core.AssetBatch.md)

Assets started by [Preload](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Preload_System_String___): progress for a loading screen, errors, and a way to wait.

 [AssetCache](MicroEngine.Core.AssetCache.md)

Loads, shares and releases a game's content files (<code>Game.Assets</code>): images as textures, packing tools' atlases,
folders of images packed into one atlas, and TTF/OTF fonts. The same request returns the same object, so a texture
used by ten scenes is decoded and uploaded once, whichever scopes ask for it; an asset is disposed when the last scope
holding it releases it.

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

A content file that could not be loaded: missing, unreadable, in an unsupported format, or named with the wrong letter case.

 [CompositeInput](MicroEngine.Core.CompositeInput.md)

Several input sources read as one: a key or button is down when any source has it down; the stick or axis with the largest deflection wins.

 [ContentFolder](MicroEngine.Core.ContentFolder.md)

Locates a game's asset files. By default the root is the <code>Content</code> folder next to the
executable (games copy it there at build time). Missing files fail with the full path so a
broken install or a typo is obvious.

 [DisplayInfo](MicroEngine.Core.DisplayInfo.md)

A display (monitor) as the system reports it.

 [FileSaveStorage](MicroEngine.Core.FileSaveStorage.md)

One JSON file per slot in a directory (desktop and tests).

 [FrameProfiler](MicroEngine.Core.FrameProfiler.md)

Measures a run frame by frame and the phases of its start, for <code>--profile</code> and benchmarks. It only measures
what the engine itself sees: when [EndFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_EndFrame) returns, not when the display shows the frame
(presentation times need the platform's own tools). Samples go into a buffer allocated up front, so profiling
adds no allocations to the frames it measures; a run without a profiler does not measure anything.

 [FrameSummary](MicroEngine.Core.FrameSummary.md)

What a profiled run looked like. [MotionError](MicroEngine.Core.FrameSummary.md#MicroEngine_Core_FrameSummary_MotionError) is the measure of uneven motion: for every frame, how far
the game time it advanced differs from the wall time that passed, so a frame that repeats the previous picture or
jumps two steps counts even when every frame took the same time.

 [Game](MicroEngine.Core.Game.md)

Base class for a game. Subclasses declare [Settings](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Settings), build content in
[Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) and push the first scene. The runner owns the loop.

 [GameLauncher](MicroEngine.Core.GameLauncher.md)

The command line every desktop game accepts, on any backend. Supported arguments:

<ul><li><code>--script FILE</code> play scripted input (see [InputScript](MicroEngine.Core.InputScript.md)); implies deterministic time unless
<code>--time</code> says otherwise.</li><li><code>--captures DIR</code> directory for script captures (default ./captures).</li><li><code>--platform NAME</code> run on that platform of the catalog (default: <code>MICROENGINE_PLATFORM</code>, else the
first one that can run here); <code>--list-platforms</code> prints them and exits.</li><li><code>--headless</code> run without a window (no captures are written); the same as <code>--platform headless</code>.</li><li><code>--hidden</code> render in a hidden window (captures still work); it implies nothing about time or sound.</li><li><code>--time realtime|deterministic</code> how the loop advances time ([TimeMode](MicroEngine.Kernel.TimeMode.md)): with the
wall clock, or one fixed step per frame; by default the game's choice, else the platform's preference.</li><li><code>--frames N</code> stop after N updates; <code>--seed N</code>; <code>--scale N</code>; <code>--mute</code>.</li><li><code>--report</code> print the platform that runs and whether it has audio before the game starts (package checks).</li><li><code>--audio auto|device|silent</code> where sound may go ([AudioOutput](MicroEngine.Core.AudioOutput.md)); by default
deterministic-time runs (scripts, <code>--time deterministic</code>) open no physical device, so checks are never heard.</li><li><code>--audio-capture FILE</code> play nothing aloud and write what would have been heard as a WAV
([CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)).</li><li><code>--gpu auto|high-performance|power-saving</code> the GPU to ask for on a machine with two
([GpuPreference](MicroEngine.Core.GpuPreference.md)).</li><li><code>--renderer NAME[,NAME...]</code> graphics APIs to try first ([Renderers](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Renderers)).</li><li><code>--vsync on|off</code> wait for the display's refresh or not ([VSync](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_VSync)).</li><li><code>--fps-limit N</code> draw at most N frames per second; 0 removes the game's limit
([FrameRateLimit](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_FrameRateLimit)).</li><li><code>--window windowed|fullscreen|exclusive</code> the window mode ([WindowMode](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_WindowMode)).</li><li><code>--display N</code> open on display N, counted from 0 ([Display](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Display)).</li><li><code>--pacing display|measured</code> count frame time in refresh periods of the display (the default) or as
measured ([PaceToDisplay](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_PaceToDisplay)).</li><li><code>--profile FILE</code> measure every frame and the start phases ([FrameProfiler](MicroEngine.Core.FrameProfiler.md)), print a summary and
write the results as JSON, or CSV when FILE ends in .csv.</li></ul>

Returns a process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.

 [GameLoop](MicroEngine.Core.GameLoop.md)

The loop, one frame per [Tick](MicroEngine.Core.GameLoop.md#MicroEngine_Core_GameLoop_Tick). It owns time: the platform only supplies a monotonic clock
([Now](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Now)), and the engine's [FrameClock](MicroEngine.Kernel.FrameClock.md) turns each frame's time into fixed
steps following the game's [TimeSettings](MicroEngine.Kernel.TimeSettings.md) (at most <code>MaxStepsPerFrame</code> a frame, frames of at
most <code>MaxFrameDelta</code>). Each frame runs the engine's frame schedule: the per-frame phases, the four fixed phases
once per step, then the update, drawing and presentation phases. In deterministic time every frame is exactly one
step, which makes scripted runs reproducible. Hosts that cannot block, such as a browser driving frames from
<code>requestAnimationFrame</code>, call [Tick](MicroEngine.Core.GameLoop.md#MicroEngine_Core_GameLoop_Tick) themselves; errors are captured, never thrown.

 [GameRunner](MicroEngine.Core.GameRunner.md)

Runs a game to completion on platforms that own their thread (desktop, headless):
[GameLoop](MicroEngine.Core.GameLoop.md) ticked in a blocking loop.

 [GameScreen](MicroEngine.Core.GameScreen.md)

The game's screen while it runs ([Screen](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Screen)): the displays, which one the game is on, and the options
that can change without restarting. Changes apply where the platform allows; read a property back to see what applies
(a browser or phone keeps its own window, a headless run has none).

 [GameSession](MicroEngine.Core.GameSession.md)

Runs one game at a time for hosts that drive frames themselves and outlive a run, such as a browser
page: each [Start](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Start_MicroEngine_Core_RunOptions_) builds a fresh game and platform, [Tick](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Tick) runs one frame, and
the end of a run (quit, error, [Stop](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Stop)) shuts the game down and releases the platform, so the
host can show its own screen and start again without reloading.

 [GameSettings](MicroEngine.Core.GameSettings.md)

Window/backbuffer settings a game declares. The virtual size is the canvas everything is drawn on: 320×180 pixel art
by default, or a high-resolution canvas such as 1280×720 or 1920×1080 with [Smooth](MicroEngine.Graphics.CanvasScaling.md).

 [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

The graphics options a game starts with ([Graphics](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Graphics)). Every option has a sensible automatic
default; the launcher's options (<code>--platform</code>, <code>--renderer</code>, <code>--vsync</code>, <code>--fps-limit</code>, <code>--gpu</code>,
<code>--window</code>, <code>--display</code>) override them for one run, and [Screen](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Screen) changes the ones a platform
can change while the game runs. A game that offers a graphics menu saves [Current](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_Current) and returns
it from [Settings](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Settings) at its next start. VSync, the frame limit and [UpdateRate](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_UpdateRate)
are independent: VSync waits for the display, the limit caps frames per second, the update rate is the simulation's.

 [HeadlessGraphicsDevice](MicroEngine.Core.HeadlessGraphicsDevice.md)

A graphics device for tests: textures keep their size and filter but hold no pixels (unless [KeepsPixels](MicroEngine.Core.HeadlessGraphicsDevice.md#MicroEngine_Core_HeadlessGraphicsDevice_KeepsPixels));
creations, uploads and live textures are counted.

 [HeadlessInput](MicroEngine.Core.HeadlessInput.md)

Keys, a gamepad and a pointer that tests set by hand.

 [HeadlessPlatform](MicroEngine.Core.HeadlessPlatform.md)

A windowless platform: deterministic time by default ([RealTime](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_RealTime) is false), no devices, and a renderer that counts calls (or, with
[Rasterize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Rasterize), also records them and draws them with the CPU reference rasterizer). Used by automated tests
and CI to play whole games through scripts.

 [HeadlessRenderer](MicroEngine.Core.HeadlessRenderer.md)

Counts draw calls and tracks camera balance so tests can assert rendering ran correctly. Created with a graphics
device, it also rasterizes: it records each draw as a line of text ([Commands](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Commands)) and draws it on
[Frame](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Frame) with the CPU reference rasterizer, which follows the engine's rendering rules exactly.

 [HeadlessTouchScreen](MicroEngine.Core.HeadlessTouchScreen.md)

A touch screen for tests: fingers go down, move and lift by hand, the size, safe area and density can change (a
rotation, a notch), vibrations are counted, and [DrawOverlay](MicroEngine.Core.HeadlessTouchScreen.md#MicroEngine_Core_HeadlessTouchScreen_DrawOverlay) records what the overlay draws.

 [InputScript](MicroEngine.Core.InputScript.md)

A deterministic input script, keyed by update number. Text format, one command per line:

```csharp
# comment
30 press confirm          # down for 2 updates
60 hold right 90          # down for 90 updates
60 down jump / 75 up jump # explicit edges
200 capture title.png
240 touch 1 dpad 1 0      # finger 1 down on the d-pad, one radius to the right (or moves there)
250 tap 2 jump            # finger 2 taps the "jump" control (lifted 2 updates later)
260 touch 3 0.5,0.5       # finger 3 at the middle of the screen (fractions of its size)
300 lift 1
320 prompts gamepad playstation  # name controls as on a PlayStation pad (keyboard, gamepad, touch or auto)
900 quit
```

"@+N" instead of an absolute number means N updates after the previous command. Touch commands go through the
game's [VirtualControls](MicroEngine.Input.VirtualControls.md) with the same hit testing as real fingers.

 [LaunchArguments](MicroEngine.Core.LaunchArguments.md)

The parsed launcher command line (see [GameLauncher](MicroEngine.Core.GameLauncher.md)).

 [MonotonicClock](MicroEngine.Core.MonotonicClock.md)

Measures wall-clock time for desktop platforms.

 [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

The platforms a host can choose from, in order of preference: the first one that can run here is the default.
Hosts select by name (a <code>--platform</code> argument, a setting), never game code.

 [PlatformConfigurationException](MicroEngine.Core.PlatformConfigurationException.md)

A platform request that cannot be met: unknown or unsupported profile, or a part the profile does not let a host replace.

 [PlatformProfile](MicroEngine.Core.PlatformProfile.md)

A named way to build an [IPlatform](MicroEngine.Core.IPlatform.md): an official backend ("sdl", "raylib", "headless") or one a
developer registers. The profile decides which parts a host may replace ([Replaceable](MicroEngine.Core.PlatformProfile.md#MicroEngine_Core_PlatformProfile_Replaceable)), because some
subsystems share state with the window and cannot be swapped independently.

 [PlatformRequest](MicroEngine.Core.PlatformRequest.md)

What a host asks of a platform: the game's settings, window options and any subsystem it supplies itself.
A backend reads the window options and must not open its own audio device when [OpensOwnAudio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_OpensOwnAudio) is false.

 [RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md)

An [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md) that keeps a list of the shapes drawn, for tests.

 [RunOptions](MicroEngine.Core.RunOptions.md)

Options shared by every launcher: scripted input, captures and run limits.

 [RunResult](MicroEngine.Core.RunResult.md)

How a run ended.

 [SaveStore](MicroEngine.Core.SaveStore.md)

Small JSON persistence for settings and progress. Files live under the user's data folder
([DefaultDirectory](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultDirectory_System_String_)) unless a directory is supplied (tests use a temporary one) or a launcher
chose another medium through [DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage) (the browser keeps saves in localStorage).
Corrupt or missing data yields null instead of crashing the game; corrupt data is set aside
("&lt;slot&gt;.corrupt.json" for files) so the next save cannot destroy it. A slot that exists but could not be
read (locked file, blocked browser storage) is never written by this store, because the game is then
running with fresh progress that would replace the real one. Failed writes return false instead of
throwing, so a full disk or browser storage quota leaves the game running with its progress in memory.

 [StartScreen](MicroEngine.Core.StartScreen.md)

The screen a game opens on while it loads: its start-screen image (<code>MicroEngineSplashImage</code>, by default the
icon) centred on its colour (<code>MicroEngineSplashColor</code>), and "Made with MicroEngine" below when the game asks
for it (<code>MicroEngineSplashBranding</code>). Phones and browsers show the same image and colour before the engine
starts (launch screens, the loading page), so the player sees one continuous screen from the tap to the first frame.
[GameLoop](MicroEngine.Core.GameLoop.md) draws it once, before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load), on platforms that run in real time; it adds no
delay, and an error while loading is reported as before.

 [WindowSizing](MicroEngine.Core.WindowSizing.md)

The window sizes desktop platforms open and allow, shared by every backend so they agree.

### Structs

 [AchievementStatus](MicroEngine.Core.AchievementStatus.md)

A snapshot of one achievement: its definition, accumulated progress and whether it is unlocked.

 [AssetInfo](MicroEngine.Core.AssetInfo.md)

One loaded asset, as [Loaded](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Loaded) lists it.

 [AssetStatistics](MicroEngine.Core.AssetStatistics.md)

Totals over a scope and its children ([Statistics](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Statistics)).

 [FrameSample](MicroEngine.Core.FrameSample.md)

One frame as [FrameProfiler](MicroEngine.Core.FrameProfiler.md) measured it. Times are in milliseconds.

 [Percentiles](MicroEngine.Core.Percentiles.md)

Percentiles of one frame measurement, in milliseconds.

 [RecordingOverlayCanvas.Shape](MicroEngine.Core.RecordingOverlayCanvas.Shape.md)

One recorded drawing call.

### Interfaces

 [IDisplayControl](MicroEngine.Core.IDisplayControl.md)

What a platform can change about its window and presentation while the game runs ([Screen](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Screen) uses it).
Platforms without a window (headless) or whose host owns these choices (browsers, phones) leave it out or report
that a change did not apply.

 [IPlatform](MicroEngine.Core.IPlatform.md)

Everything a backend provides: a window (or nothing, headless), input devices, audio,
texture creation and a renderer that draws into the virtual canvas.

 [IPlatformDiagnostics](MicroEngine.Core.IPlatformDiagnostics.md)

Facts a platform can report about itself for logs and profiles: the graphics API in use, VSync, the graphics adapter
when the backend can name it. Keys are short lower-case words; values are free text.

 [ISaveStorage](MicroEngine.Core.ISaveStorage.md)

Where a [SaveStore](MicroEngine.Core.SaveStore.md) keeps its slots: named texts that are written whole. Failures surface
as `IOException` or `UnauthorizedAccessException`.

### Enums

 [AppOrientation](MicroEngine.Core.AppOrientation.md)

Screen orientations a game allows on phones (<code>MicroEngineOrientation</code> in its project).

 [AudioOutput](MicroEngine.Core.AudioOutput.md)

Where a run's sound may go ([AudioOutput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_AudioOutput), the launcher's <code>--audio</code>).

 [GpuPreference](MicroEngine.Core.GpuPreference.md)

Which GPU a game prefers on machines with an integrated and a dedicated one. The system has the last word: on
Windows, Direct3D 11 and OpenGL use the adapter Windows assigns to the application (Settings, Display, Graphics), which
no backend overrides; SDL passes the preference to its Metal (macOS, iOS) and GPU (Direct3D 12, Vulkan, Metal)
renderers; raylib's OpenGL ignores it.

 [PlatformParts](MicroEngine.Core.PlatformParts.md)

The subsystems a host may supply to a profile instead of the profile's own.

 [WindowMode](MicroEngine.Core.WindowMode.md)

How the game's window covers the screen ([WindowMode](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_WindowMode), [WindowMode](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_WindowMode)).

</div>
