# 0004 — Load external image and audio files

Status: Accepted (2026-10-30). Amends [0002](0002-code-first-content.md) for games that opt in.

## Context

The owner asked for a fourth game, a run & gun in the style of the NES classics, whose art, music and
sound effects come primarily from existing, permissively licensed packs that the project selects and
integrates. ADR 0002 kept the engine free of file loading because every game generated its content in
code. Building this game is also meant to measure how well the engine handles a new genre.

## Decision

- The core gains file loading that stays backend-independent and testable headlessly:
  `PixelImage.FromPng`/`PixelImage.Load` (8-bit greyscale, RGB, palette and RGBA PNGs, non-interlaced)
  and `AudioClip.FromWav` (PCM 8/16-bit WAV) decode in managed code.
- Compressed music (OGG Vorbis, MP3) is streamed by the audio device through
  `IAudioDevice.LoadMusicStream`; the raylib backend uses its built-in decoders, the silent device only
  records the handle. Games register files through `AudioMixer.AddSoundFile`/`AddMusicFile`.
- `ContentFolder` resolves a game's `Content/` directory next to the executable with a clear error
  when a file is missing.
- Games still choose: the three original games stay fully code-first. A game that ships files keeps them
  under `games/<Name>/Content`, copied to the output, with a `CREDITS.md` that lists each source, author,
  URL and licence. Only CC0 or attribution licences that the credits satisfy are accepted.

## Consequences

- The repository now contains binary assets (about 12 MB for Neon Rift, mostly music). Only the files a
  game uses are committed, never whole packs.
- Image loading has no native dependency, so headless tests can assert on real asset dimensions.
- Music decoding depends on the backend; headless runs do not decode compressed music.
