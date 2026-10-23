# MR-6 — Four event-driven short sounds

Technical verification in progress. Owner acceptance of sound association, loudness and comfort
is not recorded. MR-5 session UX is accepted separately and integrated in local main `aa385cb`.

## Run

```powershell
dotnet run --project C:\Proyectos\MicroEngine\examples\Platformer\Platformer.csproj -c Release
```

Controls and gameplay are unchanged. Hear a short cue for an actual jump, a newly collected
coin, defeat and level completion. Title confirmation, held jump, pause/resume, retry and
campaign replacement do not replay old events. Pause/reset/level replacement stop old voices.
No music, streaming, mixer, spatial audio, new visual effects or gameplay mechanic is included.

## Assets and architecture

[ADR-0015](../../decisions/0015-optional-one-shot-audio.md),
[approved MR-6 contract](../../specs/meadow-audio.md).
Four original WAVs from [MoxieCat's CC0 publication](https://opengameart.org/content/8-bit-platformer-sfx-0),
not synthesized replacements or mixed audio packs. Provenance records the individual URLs,
SHA-256, access date, PCM16 mono/44.1kHz format, duration and normalized peak/RMS. The authored
NOTICE explicitly states no embedded license was supplied. Originals remain unmodified.

| Game event | Original file | Duration | Approx. source peak |
| --- | --- | --- | --- |
| Jump | jump_1.wav | 0.287s | 0.34 |
| Coin | button.wav | 0.289s | 0.14 |
| Defeat | playerhurt.wav | 0.501s | 0.22 |
| Level completion | spring.wav | 0.334s | 0.19 |

Playback gain0.5 gives a conservative measured four-source peak-sum bound below0.45. This is
not proof of speaker quality, perceived loudness or artistic coherence. No subjective listening
claim is inferred from decoded samples or successful playback calls.

`engine/Audio` contains only optional `IOneShotAudio`, with no packages, Loop, codec or genre
concept. Desktop owns a standalone device and configured samples; it does not require a window.
Providers can be replaced through the public interface. Headless models remain native-free.
Game-owned transient events and MeadowSoundFeedback choose cue policy. SoundPlatform composes
window/audio acquisition and cleanup so errors appear in the existing ExecutionReport.

The backend accepts a documented bounded PCM16 WAV profile, arbitrary ordinal cue keys and
owner-thread requests. It refuses foreign device ownership, validates before native calls,
cleans partial loads and poisons reservations after uncertain close. Only typed device
unavailability after successful cleanup degrades to silence with a stderr warning. Bad assets,
playback errors and cleanup failures are not swallowed. Existing Loop/Hosting/lifecycle and
input defaults remain untouched. Native wave storage is explicitly freed after sound copying.

## Author evidence

- Desktop provider tests:6 RED against stubs ->46 GREEN; chunk padding/order, duplicates,
  duration boundary and partial volume failure ->49; foreign-device ownership RED ->50 GREEN.
- Game events/feedback/ownership:4 RED against stubs ->33 GREEN; full session cue association
  and report cleanup ->35; wrong-thread wrapper disposal RED ->36 GREEN.
- Own serial suites:326 passed,0 failed/skipped. Native NoBuild runs for Platformer, Adventure,
  RPG and standalone Audio each returned actual exit0. Final frozen review must rebuild fresh
  to cover the newest source, including the foreign-device native fixture.
- Native audio initialized a real WASAPI48kHz device with no window, loaded four original
  WAVs, issued play/stop, closed and reopened. This is not a DAC/audibility measurement.
- Existing gameplay/session/patrol/pixel/CLI7 proofs remain; the game native probe additionally
  uses a recording provider with public session inputs to prove all four cue associations and
  no stale pause/reset cues. No production debug flags were added.
- Four asset verifiers passed. An isolated copied WAV with one changed sample was rejected
  by the provenance verifier (exit1); originals rechecked successfully afterwards.
- Initial sandbox VSTest connection failure was resolved by the same required tests outside
  sandbox. No required check is waived; final independent gate follows.

## Reproduce and subjective handoff

`checks/AudioNativeSmoke.ps1` tests standalone actual device ownership. Hardware unavailability
is a failed/incomplete native proof, not a fabricated pass. `checks/PlatformerNativeSmoke.ps1`
retains production CLI/lifecycle proof; `checks/VerifyPlatformerAudioAssets.ps1` checks originals.

After objective closure, evaluate only cue association, relative loudness, annoyance and overall
comfort during real play. Do not repeat automated input/cleanup/hash tests manually. This new
sonic feedback is the meaningful experience gate; it does not authorize music or richer audio
infrastructure by analogy. No main integration or push for MR-6 before applicable owner feedback.
