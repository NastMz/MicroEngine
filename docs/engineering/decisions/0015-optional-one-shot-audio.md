# ADR-0015 — Optional one-shot audio

Status: accepted 2026-10-22 by root under delegated technical authority after independent
`audio_scope_map` review and targeted readiness/ownership/profile corrections passed.

## Decision

Add `engine/Audio` (net10.0, no packages or Loop reference):

```csharp
public interface IOneShotAudio : IDisposable
{
    void Play(string cue);
    void StopAll();
}
```

Cue identifiers are opaque, nonblank strings. The engine has no Meadow event names, codec,
rendering, file-path or genre dependency. Providers own configured samples and the device.
Games select a provider, map their events and decide pause/reset policy. Headless absence
is valid; no audio object is implicitly created by the frame loop or window.

`Play` requests a one-shot, not synchronous audibility. Replaying the same cue restarts its
single voice; distinct cues can overlap. `StopAll` stops owned voices. There is no music,
streaming, spatial audio, mixer, voice allocator or generic volume API in this increment.
Null/blank cues reject with an ArgumentException-family error; unknown cues reject with
KeyNotFoundException before effects. Desktop uses ObjectDisposedException after disposal
and InvalidOperationException for wrong-thread access, without native effects.

## Desktop provider

`DesktopOneShotAudio(IReadOnlyDictionary<string, byte[]> waves)` implements the contract
with existing Raylib-cs 8.1.0; no package version changes. Configure 1..16 ordinal cue keys.
The provider clones and validates every encoded file before native initialization. Its
bounded WAV profile is an adapter limit, not a universal engine format restriction.

One session may own the process audio device. All public calls belong to its creating
thread. Audio initialization is independent of a graphics window. Refuse a pre-existing
foreign Raylib device without initializing or closing it. The managed reservation guards
MicroEngine instances; failed readiness queries release that reservation without closing
unowned native state. Once initialization is attempted, partial failure must clean up.

Native loading is `LoadWaveFromMemory` -> `IsWaveValid` -> `LoadSoundFromWave` ->
`IsSoundValid`; always `UnloadWave` in finally. LoadSoundFromWave copies/converts sample
data and does not own the input Wave. Sound buffers and the device are separately owned.

Partial construction failure unloads previously acquired sounds in reverse order and closes
the device, retaining the primary and cleanup failures. Dispose is terminal/idempotent:
attempt all stops, reverse unloads, then close even after earlier failures. Aggregate errors;
never retry uncertain cleanup. Release the reservation only after successful close; a failed
close leaves it poisoned. Internal fake APIs exercise fault paths without public native handles.

Selected source peaks are quiet (0.14..0.34). Desktop gain is fixed at 0.5 for this profile;
the sum of the four measured Meadow source peaks times 0.5 is below 0.45. This is not a
universal clipping or perceived-loudness guarantee for arbitrary configured content.

### Exact WAV acceptance profile

| Input | Acceptance |
| --- | --- |
| Encoded bytes | non-null, 44..1,048,576 bytes inclusive; clone before validation/use |
| Container | RIFF/WAVE; RIFF length + 8 equals the copied array length |
| Chunks | unsigned length checked against remaining bytes before arithmetic; honor odd-byte padding; skip unknown chunks; reject duplicate fmt/data chunks |
| Format | fmt length at least 16; PCM code 1, exactly 16-bit, one or two channels |
| Rate/alignment | 8,000..48,000 Hz inclusive; blockAlign = channels * 2; byteRate = rate * blockAlign |
| Samples | nonempty data, divisible by blockAlign, at most three seconds |
| Invalid/profile violation | InvalidDataException before native calls; native load/decode failure is operational |

### Unavailability and ownership trace

Public Desktop `AudioDeviceUnavailableException` is thrown only when Init returned normally,
Ready is false, and constructor cleanup succeeded. Init exceptions are not reclassified.
Program catches only that type, writes `audio:unavailable: continuing silently` once to stderr,
and returns no provider. Corrupt files, unknown cues, playback errors and cleanup failures
are never downgraded to silence. Failed cleanup must preserve errors rather than fallback.

The game-owned SoundPlatform composition preserves FrameLoop's single provider lifecycle:

1. Acquire audio, then acquire the window; the loop owns the resulting wrapper.
2. Audio acquisition failure does not call the window factory.
3. Window acquisition failure disposes audio; retain both failures if cleanup also fails.
4. Final disposal attempts window then audio, aggregating failures for ExecutionReport.
5. Validate factory arguments before effects. Guard the wrapper's creation thread before
   terminal state mutation; rejected off-thread disposal must permit later owner cleanup.

Feedback is primed after audio acquisition, before the first session tick, so initial sync
drops no new event. Game policy stops voices on pause/reset/level replacement; no automatic
provider pause/focus policy is introduced.

## Alternatives and consequences

A no-op-only solution would not validate actual sound. Native calls in game code violate
NFR-005. Adding audio to canvas/frame interfaces would couple it unnecessarily to rendering.
Opaque cues avoid premature PCM, asset-manager or mixer contracts; alternate providers may
use other formats. SoundPlatform is game composition, not a new engine acquisition framework.
The native callback thread stays inside Desktop; public operations remain owner-thread-bound.

## Sources and evidence boundary

[raylib-cs v8.1.0](https://github.com/raylib-cs/raylib-cs/tree/v8.1.0),
[raudio implementation](https://github.com/raysan5/raylib/blob/master/src/raudio.c).
Waveforms, fake lifecycle tests and native requests prove technical integration, not actual
speaker output or aesthetic quality. Subjective listening belongs to the delivery gate.