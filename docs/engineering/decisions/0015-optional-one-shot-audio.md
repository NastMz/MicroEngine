# ADR-0015 — Optional one-shot audio, independent of the frame loop
Status: accepted2026-10-22 by root under delegated authority after independent audio_scope_map review and targeted readiness/ownership correction PASS.

## Decision and limits
Add independent engine/Audio (net10.0, no packages/Loop) with IOneShotAudio : IDisposable,
void Play(string cue), void StopAll(). Cue is opaque nonblank string, not engine event/genre.
No audio on IFramePlatform/ICanvas, no mandatorydevice/headless dependency. Providers own
samples/device; game choosesprovider and maps its own transientevents to cues. Play requests
one-shot, not a synchronousaudibility guarantee. Replaying a cue restarts its one voice; distinct
cues mayoverlap. StopAll stops ownedvoices; no music/mixer/spatialstreaming/volumeAPI incontract.
Null/blankcue ArgumentException-family, unknowncue KeyNotFoundException before effects;
disposed ObjectDisposedException, wrongthread InvalidOperationException in Desktop.

DesktopOneShotAudio(IReadOnlyDictionary<string,byte[]> waves) implements it with existing
Raylib-cs8.1.0, no versionchange. Configuredopaque1..16distinctordinalkeys, copiedencodedWAV
bytes, max1MiB/file, boundedPCM16mono/stereo8k..48k/3s decodeprofile explicitadapterlimit, not
universalengineformatconstraint. Validate allencodedheaders/lengths/format before nativeinit.
Backend fixed0.5gain pervoice for the selected quiet source set; the sum of measured source peaks times0.5 is below0.45 for all four Meadow voices. This is not a universal clipping guarantee for arbitrary provider content.
Oneaudiosession/process, owningthread, independentfromwindow. Reserve beforeinit, releaseonly
afterprovenclose; failedclose poisonsreservation. LoadWaveFromMemory/IsWaveValid ->loadsound/
IsSoundValid ->UnloadWavefinally; sounds/device separatelyowned. Partialconstructorfailure
attempts allpriorunloads/reverseorderanddeviceclose; preserveprimaryandcleanupfailures.
Dispose terminal/idempotent: stopall, reverseunloadall, close evenafterfailure, aggregateerrors,
no unsafe retry; failedclose retainsreservation. Play/StopAllafterdispose andwrongthread have
no nativeeffects. Internal fakeAPI seam tests lifecycle, neverpublicnativehandles.

Game SoundPlatform composition (not engine) owns audio plus existingGamePlatform in FrameLoop
factory. Acquireaudio thenwindow; failedwindow acquisition disposesaudio, preservingbotherrors.
Finalcleanup attemptswindow thenaudio andaggregates so ExecutionReport captures failures.
AudioDeviceUnavailableException alone mayfallback tosilent mode with explicitwarning; corrupt
content/contracterrors are not swallowed. Headlessaudioabsence isvalid; noaudioobjectrequired.
No automaticstop/pauseonfocus inprovider; game choosesStopAllonpause/restart/levelreplacement.

## Alternatives and consequences
Nooponlywouldnotvalidateaudiblefeedback. NativecallsingameviolateNFR005. Extendingcanvas/loop
wouldcoupleaudio to rendering unnecessarily. Genericcueinterface avoids premature PCM/codec/
assetmanager/mixercontracts; configuredbackend candecodeotherformatslater with ownprofile.
Standalonegamewrapper preservesexistinglifecycle and makes acquisition/cleanupobservable.
One-shot scheduling uses nativecallbackthread internally; allpubliccalls remainowner-threadbound.
Futurevoices/mixing/music need realconsumerrequirements, not extensionsbyanalogy.

## Sources
[raylib-cs v8.1.0](https://github.com/raylib-cs/raylib-cs/tree/v8.1.0),
[raudio implementation](https://github.com/raysan5/raylib/blob/master/src/raudio.c).
Nativeaudibility/devicequality is not proved by Playreturn; waveforms/managedfake/nativecalls
prove technicalintegration, ownerlistensonlyafter meaningfuldelivery.

## Exact provider profile and error classification
The Desktop decoder accepts the following bounded profile; other providers may differ.

| Input | Acceptance |
| --- | --- |
| Encoded bytes | non-null, 44 bytes through 1,048,576 bytes inclusive; copy before validation/use |
| Container | RIFF then WAVE; RIFF length + 8 equals the copied array length |
| Chunks | unsigned length checked against remaining bytes before arithmetic; honor odd-byte padding; skip unknown chunks; reject duplicate fmt or data chunks |
| Format | fmt length at least16; PCM code1, exactly16-bit, channels1 or2 |
| Rate/alignment | sample rate8,000..48,000Hz inclusive; blockAlign=channels*2, byteRate=rate*blockAlign |
| Samples | nonempty data; data length divisible by blockAlign; duration at most3seconds |
| Malformed/profile violation | InvalidDataException before any native call; native decode/load failures are operational failures, never silent fallback |

The new public Desktop AudioDeviceUnavailableException is thrown only when Init returned
normally but Ready is false AND constructor cleanup succeeded. Init exceptions are not
reclassified. Program's audio factory catches only that exact typed exception, writes
"audio:unavailable: continuing silently" to stderr once, and returns null. Corrupt content,
unknown cues, playback errors and any cleanup failure propagate normally. Failed cleanup
retains all errors and must never become the typed fallback.

SoundPlatform ownership trace: audio acquire -> window acquire -> FrameLoop owns wrapper.
Audio acquisition failure does not call the window factory. Window acquisition failure
attempts audio disposal; preserve window error plus audio cleanup error if both fail.
Final wrapper disposal is terminal/idempotent: window disposal then audio disposal, always
attempt both and aggregate failures for ExecutionReport. No audio ownership remains outside
the wrapper. All factory arguments validate before effects. Feedback is primed once after
audio acquisition and before first session update, so the initial-sync rule drops no new cue.
