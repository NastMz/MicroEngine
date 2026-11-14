# AUDIO-1: exposed audio mixer controls and latency

Objective: assess, with measurements, what a game can control of the mixer through the public API, then implement the
public extensions the evidence shows are necessary. Issue NastMz/MicroEngine#124. Branch `feat/audio-1`.
Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/audio-1.md`](../../docs/evaluations/audio-1.md).

## Specs

- S1 Why: "The rebuilt audio engine has buses/voices and handles internally, while the convenient game-facing API may
  not expose all advanced capabilities. Assess the usability gap for 2D games without creating a sound-effect library."
- S2 Controls: "Public-API management of individual voices, buses, simultaneous music/streams, live volume/pitch and
  simple positional stereo control where needed."
- S3 Latency: "Playback latency and consistency of triggering audio from frame updates, especially native vs browser."
- S4 Reuse and profile: "Reuse the existing managed mixer and PCM sink; profile allocations and timing before changes."
- S5 Scope: "Distinguish generic mixer primitives from game-specific audio effects."
- S6 Done when: "Identify what already works and what public extensions, if any, are necessary, with a small game-side
  usage example and evidence for timing/latency claims."
- S7 Owner's standing rule for research issues (as TILE-1 S6): they end in implementation; the bar is whether an
  external developer could build that game today with the published SDK. Additive public API only; the hand-off between
  the game thread and the render function stays the one command queue; rendering stays allocation-free.
- S8 Same-update changes (L8): "Conservar provisionalmente la nueva semántica de comandos en el mismo paso. Cuantificar
  las 34 diferencias encontradas y determinar si generan artefactos perceptibles o regresiones funcionales."
- S9 Latency (L8): "Investigar la latencia de 120 ms frente a la propuesta de 70 ms. Medir la desviación de
  sincronización real en ambos enfoques, separando latencia de salida y precisión temporal. Si la solución es
  localizada y no introduce regresiones significativas, implementarla. No modificar el modelo temporal a ciegas."
- S10 (L8): "Corregir `SetPitch(NaN)` y añadir pruebas."
- S11 Queue (L8): "Reproducir la saturación de la cola, identificar sus consecuencias reales y corregirlas dentro de
  este lote si el alcance es razonable."
- S12 (L8): "Añadir pruebas de pausa/cambio en un mismo paso y reutilización de handles."
- S13 (L8): "Conservar las modificaciones de `docs/manual/audio.md`."
- S14 General criterion (L8): "corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar
  innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta
  evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las
  hipótesis y las diferencias perceptibles para el usuario."

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map the public audio surface against S2 | S1 S2 S5 | inline | done | evaluation §1, §3 |
| T2 Baseline profile before any change: render time, allocations, trigger timing on the SDL sink | S3 S4 | inline | done | evaluation §2 (run before the first source write) |
| T3 `AudioEngine.SetPitch`: live pitch through the existing command queue; one step's changes apply together | S2 S4 | inline | done | `f8e22ee`; RED: `SetPitch` missing (CS1061), then `Changes_of_one_step_take_effect_together_at_its_frame` failed without the merge; `AudioEngineTests` green |
| T4 `Game.Audio` (`AudioMixer`): voices with handles, voice control, buses, music voices | S2 S7 | inline | done | `c4110df`; RED: 10 missing members (CS1061); `AudioMixerVoiceTests` 8/8 |
| T5 Game-side example (sample region), manual, release notes, API reference | S6 | inline | done | `74af2e2` (`VoicesExample`, capture and audio capture inspected), `7af2942`, `f3b1ffd` (manual: outside the listed surfaces, own commit) |
| T6 Profile after the change; evaluation `docs/evaluations/audio-1.md` | S3 S4 S6 | inline | done | evaluation §2, §4, §5 |

## Log

- L1 (2026-11-13) The issue, verbatim:

  > ## Why
  > The rebuilt audio engine has buses/voices and handles internally, while the convenient game-facing API may not
  > expose all advanced capabilities. Assess the usability gap for 2D games without creating a sound-effect library.
  >
  > ## Research
  > - Public-API management of individual voices, buses, simultaneous music/streams, live volume/pitch and simple
  >   positional stereo control where needed.
  > - Playback latency and consistency of triggering audio from frame updates, especially native vs browser.
  > - Reuse the existing managed mixer and PCM sink; profile allocations and timing before changes.
  > - Distinguish generic mixer primitives from game-specific audio effects.
  >
  > ## Done when
  > Identify what already works and what public extensions, if any, are necessary, with a small game-side usage example
  > and evidence for timing/latency claims.
  >
  > **Status:** Future candidate.
- L2 (2026-11-13) Mapping. `AudioEngine` (public, `MixerAudioDevice.Engine`) already has voices with handles, stop
  with fade, live volume and pan, pause, nested buses, priorities and any number of streams
  (`src/MicroEngine.Audio/Mixing/AudioEngine.cs`). `AudioMixer` (`Game.Audio`, `src/MicroEngine.Audio/Audio.cs`)
  exposes none of it: `Play` returns `void`, no loop, bus or priority, one music track; and it keeps only device
  handles, so a sound registered by name cannot be played on the engine. Live pitch exists nowhere. The facade is in
  `src/MicroEngine.Audio`, inside the edit surfaces.
- L3 (2026-11-13) Baseline, before the first source write (harness in the session's scratch folder, default SDL
  output of this machine): rendering 32 voices takes 40.6 µs per 256-frame block (0.76 % of real time) and allocates
  nothing; game-thread calls allocate nothing; a sound's frame is rendered a median 60.9 ms after `Play` at a steady
  60 fps, and the SDL queue adds its 60 ms, so sounds reach the device about 120 ms after the update, twice what
  ADR 0033 and the manual say; after slow frames the median stays higher (87 ms after 40 ms frames, 120 ms after
  120 ms frames). Browser not measured (no browser build made in this session).
- L4 (2026-11-13) Decisions. New members instead of new overloads of `Play` (`PlayVoice`, `PlayMusicVoice`), so no
  existing call can become ambiguous. A voice plays exactly its `PlayParameters`; `SfxVolume`, `MusicVolume` and the
  once-per-step rule stay those of `Play` and `PlayMusic`. Buses by name on `Game.Audio`, kept in a table when the
  device has no mixer, so headless code behaves the same. Positional stereo stays game code over `Set` (the example).
  No ADR: additive, no new contract or threading rule.
- L5 (2026-11-13) The latency findings are not fixed here: both fixes change when existing sounds are heard and trade
  exact-frame starts against latency (evaluation §5). They wait for the owner.
- L6 (2026-11-13) The audio manual is `docs/manual/audio.md`, outside the surfaces listed for this work (which name
  `docs/guide/**`); its limitations stated what this change adds, so it was edited in a commit of its own (`f3b1ffd`)
  for the owner to keep or drop. `AGENTS.md` was not edited (other work in progress touches the same sentence): this
  record still has to be added to its list of feature records.
- L7 (2026-11-13) After the change: rendering 32 voices with a pitch, volume and pan change per block takes 40.2 µs
  per block, 0 bytes; `SetPitch` 42 ns, `AudioMixer.Set(volume, pan, pitch)` 173 ns, 0 bytes; trigger timing
  unchanged (median 60.3 ms at 60 fps).
- L8 (2026-11-13) The owner's review of the batch, verbatim:

  ```
  AUDIO-1

  * Conservar provisionalmente la nueva semántica de comandos en el mismo paso. Cuantificar las 34 diferencias encontradas y determinar si generan artefactos perceptibles o regresiones funcionales.
  * Investigar la latencia de 120 ms frente a la propuesta de 70 ms. Medir la desviación de sincronización real en ambos enfoques, separando latencia de salida y precisión temporal. Si la solución es localizada y no introduce regresiones significativas, implementarla. No modificar el modelo temporal a ciegas.
  * Corregir `SetPitch(NaN)` y añadir pruebas.
  * Reproducir la saturación de la cola, identificar sus consecuencias reales y corregirlas dentro de este lote si el alcance es razonable.
  * Añadir pruebas de pausa/cambio en un mismo paso y reutilización de handles.
  * Conservar las modificaciones de `docs/manual/audio.md`.

  Criterio general: corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las hipótesis y las diferencias perceptibles para el usuario.
  ```
