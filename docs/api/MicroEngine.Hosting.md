# Namespace MicroEngine.Hosting

<div data-pagefind-weight="0.3">

Hosting: the launcher model every host shares (launch requests and their rules, platform selection, the host environment) and the composition a game configures once with its Configure entry point.

Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

### Classes

 [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

The composition of one host, configured once by the game's <code>Configure</code> entry point and reused by every host
that runs it: the desktop launcher (<code>Program.cs</code>), the browser page, the Android and iOS apps and tests. The
host fills its defaults first (its platforms, where content files are, where saves go), then calls the game's
<code>Configure</code>, which may replace any of them and adds what the game needs (the game itself, platforms of its own,
engine services). Nothing here is process-wide: two hosts in one process (an editor, parallel tests) never share a
composition.

 [HostEnvironment](MicroEngine.Hosting.HostEnvironment.md)

What a host knows about the process it runs in, handed to the composition instead of being read from process-wide
state: its kind, its developer variables (environment variables on desktop; the page's or the launch's variables
elsewhere), where it reports and where its files are. Tests build one with their own variables and writers.

 [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)

What one launch of a game asks for, whichever host received it: a desktop command line ([Parse](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Parse_System_Collections_Generic_IReadOnlyList_System_String__System_Func_System_String_System_Boolean__)), a
browser page's query string, a phone's launch extras or a test. Every host turns its input into this record and
hands it to the same launcher model, so the rules that follow from it are written once: a run with a script is in
deterministic time unless [Time](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Time) says otherwise ([EffectiveTime](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_EffectiveTime)) and has seed 1 unless
[Seed](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Seed) says otherwise ([ResolveSeed](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ResolveSeed_System_Int32_)).

 [ProfileChoice<TProfile\>](MicroEngine.Hosting.ProfileChoice\-1.md)

The platforms a launch tries, in order, or why none can be tried.

 [ProfileSelection](MicroEngine.Hosting.ProfileSelection.md)

How every host chooses a platform from its catalog (an ordered list of profiles, the most preferred first). The rules
are the launcher's, written once: a named platform ([Platform](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Platform), else the developer variable
<code>MICROENGINE_PLATFORM</code>) is the only candidate; [Headless](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Headless) names the headless platform;
otherwise the game's preferred backend comes first, then every other platform that can run here except the headless
one (a player who sees no window would not know why), or the catalog's default when that leaves none.

### Interfaces

 [IHostProfile](MicroEngine.Hosting.IHostProfile.md)

A named way to build a platform that a host can choose (the engine's platform profiles implement it).

### Enums

 [HostKind](MicroEngine.Hosting.HostKind.md)

The kind of program hosting a game; the composition may differ by kind (a browser has no files to save to).

</div>
