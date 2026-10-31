# Distribution and licensing proposal (for owner review)

Internal document, not published on the site.

> **Status (2026-10-30): model approved by the owner.** Proprietary and private source; free personal, educational
> and commercial use; no royalties or revenue limits; games belong to their creators; the runtime may ship inside
> games; the SDK or engine may not be redistributed or resold on its own; "Made with MicroEngine" attribution
> optional; name: MicroEngine SDK License. The previous implementation was published under MIT and those copies keep
> their rights; the current implementation is a greenfield rewrite never published as source.
>
> Done since: `LICENSE` replaced by an all-rights-reserved holding notice; licence draft for counsel in
> [microengine-sdk-license-draft.md](microengine-sdk-license-draft.md); third-party notices in
> `THIRD-PARTY-NOTICES.md`, published on the site and shipped in every web package. Still open: the final licence
> text (legal review) and its publication with the SDK.

The original proposal follows.

## 1. Where the current licence and "open source" appear

| Place | Content | State after WEB-2 review |
| --- | --- | --- |
| `LICENSE` | MIT License, "Copyright (c) 2025 Kevin Martinez" | Unchanged, pending this decision |
| `README.md`, section "License" | links `LICENSE` as MIT | Unchanged (internal file; the repository is private) |
| Site footer | "MicroEngine is MIT licensed" | Removed |
| Site hero | "Open source under MIT · Browse the code" | Removed |
| Site header, footer, demo pages, docs | links to the GitHub repository ("Source", "Source on GitHub", "Source code", "Edit page", GitHub icon, links generated from Markdown) | Removed; links from public pages to internal files are published as plain text |
| Landing "Start from the source" | `git clone` of the private repository | Replaced by the distribution status |
| Public docs | build-from-source instructions | Reframed; engine-development setup kept as internal docs |

No other file in `src/`, `games/`, `web/`, `samples/` or `docs/` declares a licence for the engine. The site build
now fails if a page links into the repository, claims an MIT licence or open source, or tells readers to clone it
(`site/scripts/check-dist.mjs`).

## 2. What the existing MIT licence implies

- **Copies already given out stay MIT.** The MIT grant cannot be withdrawn for a copy someone already received under
  it: that person may keep using, modifying and redistributing that version. Whether this matters depends on a fact
  only the owner knows: **has any version of the repository ever been public, shared, or given to anyone else?**
  If not, no third party holds an MIT copy.
- **New versions can use a new licence.** The git history shows a single author (Kevin Martinez, two e-mail
  addresses), so the copyright holder can license future versions differently without asking contributors. Check
  that no third-party code was copied into the repository under terms that would need its own notice.
- **Practical step when the new licence is adopted:** replace `LICENSE` from a named version onward, keep the MIT
  text in history for earlier versions, and say in the release notes which versions were MIT (if any were
  distributed).
- **Closed source is not unreadable.** .NET assemblies and WebAssembly modules can be decompiled. A licence can
  forbid reverse engineering (within what local law allows), but it does not prevent it technically.

## 3. Proposed model

A proprietary, free-of-charge licence for the compiled SDK, often called a freeware EULA. Points the licence would
need to cover, each to be decided:

| Topic | Proposal | Decision needed |
| --- | --- | --- |
| Grant | Non-exclusive, worldwide, free licence to install and use the SDK to develop games | Scope (individuals, companies, education) |
| Commercial use | Allowed for games made with the SDK | Confirm without conditions (revenue caps, registration) |
| Royalties | None; no revenue share | Confirm |
| Engine runtime in games | Games may ship the engine's runtime binaries, only as part of the game | Which files count as "runtime" |
| Redistribution of the SDK | Not allowed as a standalone product, in an SDK, template pack or asset store | Confirm wording and exceptions (mirrors, CI caches) |
| Resale, sublicensing, competing products | Not allowed | Confirm |
| Modification and reverse engineering | Not allowed, except where the law grants it | Confirm; check local law |
| Attribution | "Made with MicroEngine" optional, or required (splash or credits) | Choose |
| Notices | Do not remove copyright and third-party notices | Confirm |
| Warranty and liability | Provided "as is", liability limited | Wording by counsel |
| Termination | Licence ends on breach; games already released keep their right to ship the runtime | Confirm |
| Updates | Each version under the licence it ships with | Confirm |
| Governing law and venue | To decide | Owner and counsel |
| Name | for example "MicroEngine SDK License" | Choose |

**Engine rights and game rights are separate.** The engine (SDK binaries, documentation, templates) stays the
owner's property and is licensed, never sold. Games made with it belong to their creators: code, art, audio,
names and revenue. The only engine part a game carries is the runtime, under the redistribution clause. The
demos (Meadow Run, Courtyard Key, Trail Duel, Neon Rift) are the owner's own games; their third-party assets keep
their own licences (section 4).

**Distribution mechanics to plan with the SDK milestone:** NuGet packages carry a custom licence as a file
(`PackageLicenseFile`) and can require acceptance (`PackageRequireLicenseAcceptance`); templates ship under the
same licence; packages must not embed source files or SourceLink; each package includes a third-party notices file.

## 4. Third-party components and assets

| Component | Shipped in | Licence | Obligation | Status |
| --- | --- | --- | --- | --- |
| Raylib-cs 8.1.0 (NuGet) | engine, every game | Zlib | Do not misrepresent origin; mark altered source; keep notice in source copies | Compatible with a proprietary SDK; list in notices |
| raylib 6.0 (native library, and `raylib.a` rebuilt for the web) | every game | Zlib/libpng | As above | Compatible; list in notices |
| raylib's bundled libraries (GLFW, miniaudio, stb, dr_wav/dr_mp3/dr_flac, qoi and others under `raylib/src/external`) | inside raylib | mostly Zlib, public domain or MIT-0 options | Check each one when the SDK is packaged | To verify at SDK release |
| .NET runtime and libraries (Microsoft) | every web package (`_framework/`), self-contained desktop builds | MIT | Keep the copyright and licence notice with copies | **Gap:** the web packages carry the notice inside `dotnet.js` but ship no licence file; add a notices file to the packages or the site before public launch |
| Emscripten runtime code (inside `dotnet.native.*.js`) | every web package | MIT / University of Illinois NCSA | Keep the notice | Same gap as above |
| CC0 art and audio (Courtyard Key, Trail Duel, Neon Rift) | demo games | CC0 1.0 | None; credit given voluntarily | Verified in each `CREDITS.md` (2026-10-30); Warped City's attribution-only music is not used |
| Pixelify Sans, IBM Plex Sans, IBM Plex Mono (site) | the site | SIL Open Font License 1.1 | Keep the licence with the font files; the fonts cannot be sold alone | Named in the site footer; add the OFL text to a notices page |
| Astro, Starlight, Pagefind, sharp (site build) | the site (Pagefind's script ships to browsers) | MIT (sharp: Apache-2.0; libvips LGPL, build time only) | Pagefind's bundled notice stays in its files | Compliant as shipped |
| MSTest and test SDK | tests only | MIT | None (not distributed) | — |

## 5. Decisions requested from the owner

1. Has any version of MicroEngine been public or shared? (Determines whether MIT copies exist outside.)
2. Approve the model in section 3, or adjust it.
3. Choose the attribution rule for games.
4. Name the licence and the licensor (person or company).
5. Engage counsel for the final text, governing law, warranty and liability clauses.
6. From which version the new licence applies, and the wording for earlier versions.
7. Approve adding a third-party notices page to the site and a notices file to the web packages before the
   first public deployment (section 4 gaps).

Until these are decided, the site does not state a licence for MicroEngine at all.
