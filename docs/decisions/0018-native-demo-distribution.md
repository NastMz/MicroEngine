# ADR 0018: Native demo downloads through the existing release pipeline

## Status

Accepted. Amends [ADR 0010](0010-delivery-pipeline.md) and [ADR 0013](0013-ci-cost-and-manual-releases.md) (the store
and releases now hold native packages too). Evidence: [PRODUCT-1](../evaluations/product-1.md).

## Context

The four demos ran natively on five systems after [PLATFORM-3](../evaluations/platform-3.md), but players could only
play them in a browser. Distributing them adds questions the web packages never raised: per-system formats, signing,
system dependencies, and what "it works" means for a file built on one machine and run on another.

## Decision

- **Formats.** Windows: a portable ZIP. Linux: a portable `tar.gz` (not AppImage or Flatpak: one self-contained folder
  needs no runtime of its own, and its requirements can be measured). macOS: a ZIP holding an ad-hoc signed `.app` for
  Apple silicon. Android: one APK for `arm64-v8a` and `x86_64`, signed with a release key kept in repository secrets.
  Every desktop package is a self-contained .NET single-file executable with SDL 3 and raylib next to it, invariant
  globalization (no ICU) and no debug information.
- **No iOS download.** Reaching an iPhone requires Apple's signing (App Store, TestFlight, ad-hoc device lists, or EU
  alternative distribution), all through a paid Apple Developer membership; a simulator build cannot be installed on a
  device. iOS keeps its simulator evidence and is documented as not downloadable.
- **One description of the targets.** `packaging/targets.json` names each target's runtime, runner, archive, reference,
  requirements and install steps; the packager, the checker, the store, the workflows and the site read it.
- **Generate, check, publish are separate.** `packaging/package.mjs` builds a package; `packaging/verify.mjs` checks the
  archive itself (structure, build info, dependencies, then the game's scripted scenarios played by the package's own
  executable against the target's reference); only a passing, release-signed package is stored. A release copies stored
  packages under player-facing names (`NeonRift-1.0.0-windows-x64.zip`), hashes every download again before publishing,
  and checks their public addresses afterwards.
- **The same store and release.** Native packages are content-addressed like web packages, per game and target
  (`nativeKey`): a change to one game rebuilds only that game's packages; a documentation change rebuilds none. No
  second release system exists.
- **Evidence per renderer.** Hosted runners have no GPU: Linux and Android emulators, macOS's paravirtual GPU and
  Windows's WARP each rasterize half-pixel and rotated sprites one texel apart from GPU drivers, so each has its own
  reference, recorded from real runs. The GPU reference stays the one checked on real hardware.
- **Honest availability.** The site lists a download only if the release it reads holds it; Android is offered only
  when the signing key is configured.

## Consequences

- A release builds and plays up to 16 packages (four games, four targets) on Windows, Linux, macOS and Android runners
  when their keys are new; afterwards they are reused.
- macOS players must allow an unnotarized app once; Windows players may see SmartScreen; Linux needs glibc 2.34. These
  are documented where players read them ([Downloads](../guide/downloads.md)).
- Android's 2026 developer-verification rules affect sideloading in some countries; the demos' publisher is not
  registered, so those players need Android's advanced flow or `adb`.
- Notarization, code signing for Windows, an iOS distribution and per-game touch layouts remain open.
