# Licensing for a public repository (proposal for legal review)

Internal document, not published on the site. **Draft for counsel: not legal advice, not approved, not a licence.**
Nothing here grants rights; [LICENSE](../../LICENSE) remains the only notice in force. It extends the approved SDK
model ([distribution proposal](distribution-proposal.md), [SDK licence draft](microengine-sdk-license-draft.md)) to a
repository whose source is visible. Context and audit: [PUBLIC-1](../evaluations/public-1.md). Sources read
2026-10-31.

## 1. The categories, and where MicroEngine falls

| Category | Defining test | Examples | Fits MicroEngine's goals? |
| --- | --- | --- | --- |
| Open source (OSI) | The [Open Source Definition](https://opensource.org/osd): free redistribution (1), derived works allowed (3), no discrimination against fields of endeavour (6) | MIT, Apache-2.0, GPL | **No**: forbidding redistribution of the engine or competing forks fails criteria 1, 3 and 6 |
| Free software (FSF) | The [four freedoms](https://www.gnu.org/philosophy/free-sw.html): run, study, redistribute, distribute modified copies | GPL, MIT | **No**, for the same reasons |
| Permissive | Open source with few conditions (keep the notice) | MIT, Zlib, Apache-2.0 | No: anyone could resell the engine |
| Copyleft | Open source; derived works must use the same licence (LGPL/MPL: per library or file) | GPL, LGPL, MPL-2.0 | No: still allows redistribution and competing forks; would also bind games |
| Source-available | Source is visible; the licence restricts use, distribution or purpose | Defold License, PolyForm Shield, BUSL, Elastic-2.0, Unreal Engine EULA | **Yes**: the only category that can express the goals |
| Proprietary with public code | All rights reserved, source merely visible | today's LICENSE, Unity's reference-only C# source | Possible but useless to game developers: nobody may build games from it |
| SDK / binary licence (EULA) | Rights to use compiled tools and ship a runtime, no source | the [SDK licence draft](microengine-sdk-license-draft.md) | Yes for binaries; does not cover people who build from the visible source |

**Conclusion:** the result must be called *source-available*, never open source or free software, and must not be
presented as OSI-approved.

## 2. Existing licences compared

| Licence | Read | Make and sell games | Ship the runtime | Modify | Redistribute (modified) engine | Competing engine | Becomes open source | Fit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| [Defold License](https://defold.com/license/) (Apache-2.0 derived) | yes | yes, no royalties | yes | yes, changes need not be shared | yes, but not commercialised "as a Game Engine Product" | not sold; free forks allowed | no | **closest in purpose**, but allows free redistribution and free competing forks |
| [PolyForm Shield 1.0.0](https://polyformproject.org/licenses/shield/1.0.0) | yes | yes | yes | yes | yes, for non-competing purposes | **no**: "goods and services compete even when provided free of charge" | no | strong noncompete, plain language, 32-day cure; still allows non-competing redistribution and its "competing" test is broad and uncertain for games |
| PolyForm Strict / Noncommercial | yes | no (Strict) / not commercially | no | no / yes | no | no | no | too restrictive: commercial games are a goal |
| [BUSL 1.1](https://mariadb.com/bsl11/) | yes | only via an Additional Use Grant | via the grant | yes | yes | per grant | **yes**, after at most four years per version | conversion to GPL defeats the long-term goal |
| [Elastic License 2.0](https://www.elastic.co/licensing/elastic-license) | yes | yes | yes | yes | yes | only "as a managed service" | no | aimed at hosted services, not engines |
| [FSL 1.1](https://fsl.software/) | yes | yes, except competing use | yes | yes | yes | not for two years | **yes** (Apache/MIT after two years) | conversion defeats the goal |
| Unreal Engine EULA (source on GitHub for licensees) | licensees only | yes, with royalties above a threshold | yes, no engine tools or source | yes | only to other licensees, through Epic's GitHub | no | no | the access model (fork only to contribute or share with licensees) matches; royalties and accounts do not. Terms page not readable here (HTTP 403): verify |
| Unity C# reference source | yes | no (reference only) | no | no | no | no | no | proprietary-with-public-code, no build rights |

## 3. Recommended structure

Three instruments, two of them merged:

1. **One "MicroEngine License" for the engine, source and binaries alike** (merging the source-available licence and
   the SDK licence of the existing draft). A developer who compiles the engine from the visible source must get
   exactly the game rights of one who uses the published SDK; two texts invite gaps (is a game built with a
   self-compiled engine covered by the SDK licence?). The SDK draft's clauses (games belong to their creators,
   runtime redistribution, no royalties, optional attribution, termination that spares released games) become its
   core; a source section is added.
2. **The demos** (Meadow Run, Courtyard Key, Trail Duel, Neon Rift) stay **all rights reserved**: readable as
   examples, not reusable as games. Option for the owner: allow copying code snippets into one's own games
   (a narrow sample-code grant), which helps developers without letting anyone republish a demo.
3. **Third-party assets keep their own licences** (CC0 packs, credited in each game's `CREDITS.md`); procedural
   content is part of each demo. Nothing new is needed beyond keeping `THIRD-PARTY-NOTICES.md` and credits complete.

### Rights the MicroEngine License would grant (for counsel to draft)

| Topic | Proposed term |
| --- | --- |
| Who | Anyone, worldwide, free of charge, non-exclusive, non-transferable |
| Read, clone, build | Allowed, including on CI services, for any permitted purpose |
| Modify | Allowed, to (a) develop games, (b) evaluate the engine, (c) prepare contributions to the official repository |
| Make games | Allowed for any purpose, commercial included; no royalties, no revenue limit, no registration |
| Ship in a game | The runtime (compiled engine assemblies and native libraries), modified or not, only as part of a game; no source, SDK tools or templates |
| Redistribute the engine or SDK | **Not allowed** on its own, modified or not, free or paid: no mirrors, packages, templates, asset-store items or "engine distributions" |
| Forks | Allowed only as the GitHub fork GitHub's Terms already permit (section 4), to propose changes; not to publish or maintain a separate engine |
| Competing products | Not allowed: an engine, framework, SDK or game-creation tool derived from MicroEngine, free or paid (definition to adapt from Defold's "Game Engine Product" and PolyForm Shield's noncompete) |
| Attribution | Optional in games ("Made with MicroEngine"); copyright and third-party notices must stay in source copies and in shipped runtimes |
| Patents | Licence to the owner's patent claims needed to use the engine, ending for anyone who sues over them (as in Apache-2.0 and PolyForm) |
| Termination | On breach after written notice and a cure period (PolyForm: 32 days); games already released keep their runtime right |
| Versions | Each version under the licence it ships with; earlier MIT copies of the previous implementation keep MIT (section 5) |
| Warranty, liability, law | "As is", limited liability; governing law and venue for counsel (also open in the SDK draft) |
| Name | MicroEngine License (the approved name "MicroEngine SDK License" may stay if the source section is added) |

**Compatibility.** Raylib and Raylib-cs (Zlib), the .NET runtime and Emscripten (MIT) allow proprietary
redistribution with their notices; CC0 assets impose nothing; the site's OFL fonts are not part of the engine.
None of them forces a licence on MicroEngine's own code. The open points are GitHub's Terms (section 4) and the
MIT history (section 5).

## 4. GitHub's Terms always apply to a public repository

By making a repository public, the owner lets other users view and fork it, and grants them a licence to use,
display and reproduce the content **through GitHub** ([Terms D.5](https://docs.github.com/en/site-policy/github-terms/github-terms-of-service)).
The MicroEngine License cannot take that away; it can only govern what happens outside GitHub (clones, builds,
copies, distribution). Counsel should confirm the licence text is consistent with D.5 (forks on GitHub are allowed,
publishing a fork as a product is not).

## 5. Contributions

- **Inbound = outbound by default.** GitHub's Terms D.6: content added to a repository with a licence notice is
  licensed under that licence. Under a restrictive licence that gives the owner only the rights everyone has: the
  owner could not relicense contributed code (a commercial edition, a later licence, the SDK packages). So outside
  code needs an agreement **before** it is merged.
- **DCO is not enough.** The Developer Certificate of Origin certifies where code came from; it grants no relicensing
  right.
- **Recommended: a contributor licence agreement (CLA)** in the style of the [Apache ICLA](https://www.apache.org/licenses/contributor-agreements.html):
  the contributor keeps copyright and grants a perpetual, worldwide, royalty-free, irrevocable copyright and patent
  licence, **with the right to sublicense and relicense under any terms**; represents the work is original (or
  names third-party parts) and that an employer does not claim it; no obligation for MicroEngine to use it.
  Alternative: copyright assignment (stronger, deters contributors). A corporate CLA for employees.
- **Process.** A GitHub App such as CLA Assistant records signatures per pull request (it needs no repository
  secret); `CONTRIBUTING.md` already states that outside pull requests are not merged until this exists.
- **Contributors need the right to work.** The licence must expressly allow cloning, building and modifying in order
  to prepare a contribution (row "Modify (c)" above); otherwise contributing is itself a breach.

## 6. Questions for counsel

1. Does the merged licence (section 3) achieve "no redistribution, no competing forks" in the owner's jurisdiction,
   and is the "competing product" definition enforceable and clear enough for game developers?
2. Is a GitHub fork made under Terms D.5 a "distribution" under the licence, and how should the licence refer to it?
3. The 68 commits of the current implementation whose tree carried the MIT `LICENSE` file (2026-10-29/30, never
   public, never distributed): does publishing that history create any argument that those snapshots are MIT
   licensed? (Analysis in [PUBLIC-1](../evaluations/public-1.md), "MIT history".)
4. Old-implementation history (MIT, published while the repository was public): confirm it stays MIT and that the
   current implementation is a separate work (rewrite, ADR 0001), so MIT on the old code does not extend to it.
5. Contributor agreement: CLA with relicensing or assignment; employer rights; minors.
6. Governing law, venue, warranty and liability wording; whether the owner licenses as a person or a company.
7. Wording for "source-available" on the site and in package metadata (NuGet `PackageLicenseFile`) so no page
   implies open source.

## 7. What can be done now and what waits

| Now (no legal effect) | Waits for counsel and the owner |
| --- | --- |
| Keep `LICENSE` all-rights-reserved; state "source-available, not open source" in README and CONTRIBUTING | The MicroEngine License text and its effective version |
| Accept issues; refuse outside pull requests until a CLA exists | The CLA text and its signing process |
| Keep third-party notices and credits complete | Any statement on the site that the source is public |
