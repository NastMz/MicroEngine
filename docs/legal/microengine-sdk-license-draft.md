# MicroEngine SDK License — DRAFT

> **Superseded (2026-10-31).** The owner chose the MIT License for the engine and all-rights-reserved demo games
> ([ADR 0015](../decisions/0015-mit-engine-reserved-demos.md)). This document is kept as a record and is not in force.

> **Draft for legal review. Not in force and not published.** This text puts the distribution model the owner
> approved on 2026-10-30 into licence form so that counsel has a concrete starting point. Bracketed items
> `[TO CONFIRM: ...]` are open. Until counsel approves a final text and the owner publishes it with the SDK, no
> licence is granted by this document (see `LICENSE` at the repository root).

## Approved model this draft implements

- Proprietary, private source code.
- Free use: personal, educational and commercial.
- No royalties and no revenue limits.
- Games made with the SDK belong to their creators.
- The runtime may be distributed as part of games.
- The SDK or engine may not be redistributed or resold on its own.
- "Made with MicroEngine" attribution is optional.
- Name: MicroEngine SDK License.

---

## MicroEngine SDK License

Copyright (c) 2025-2026 Kevin Martinez [TO CONFIRM: licensor name, individual or company]. All rights reserved.

### 1. Definitions

- **"SDK"** means the MicroEngine software development kit distributed by the Licensor in compiled form, including
  its libraries, packages, project templates, tools and documentation.
- **"Runtime"** means the compiled MicroEngine libraries that a Game needs to run, as identified by the Licensor in
  the SDK documentation [TO CONFIRM: list of runtime files].
- **"Game"** means a game or other interactive application You create using the SDK that adds substantial content
  and functionality of Your own beyond the SDK.
- **"You"** means the individual or legal entity using the SDK.

### 2. Licence grant

Subject to this licence, the Licensor grants You a worldwide, non-exclusive, non-transferable, royalty-free licence
to:

1. install and use the SDK to develop, test and build Games, for personal, educational and commercial purposes;
2. reproduce and distribute the Runtime, unmodified, only as part of a Game, in any form and through any channel.

No fees, royalties or revenue share are due, whatever the revenue of a Game. [TO CONFIRM: no registration or
reporting requirement.]

### 3. Your Games

You own Your Games and everything You create in them, including code, art, audio, names and revenue. The Licensor
claims no rights in Your Games. This licence gives You no rights to the Licensor's trademarks, except as stated in
section 5.

### 4. Restrictions

You may not:

1. distribute, sell, rent, lease, sublicense or otherwise make the SDK, or any part of it other than the Runtime
   inside a Game, available to third parties;
2. distribute the Runtime on its own, or as part of an engine, framework, SDK, template pack, asset pack or other
   development tool;
3. use the SDK to create a product that competes with the SDK as a game engine or development tool
   [TO CONFIRM: keep or remove this clause];
4. decompile, disassemble or reverse engineer the SDK, except to the extent applicable law expressly permits it;
5. remove or alter copyright notices, licence notices or third-party notices in the SDK or the Runtime.

### 5. Attribution

Attribution is optional. You may state that a Game is "Made with MicroEngine", and may use the MicroEngine name for
that statement only, without implying endorsement. [TO CONFIRM: logo usage guidelines.]

### 6. Third-party components

The SDK and the Runtime include third-party components under their own licences, listed in the third-party notices
distributed with the SDK. Those licences govern those components, and You must keep their notices when You
distribute the Runtime in a Game.

### 7. Updates

Each version of the SDK is licensed under the licence that accompanies it. [TO CONFIRM: whether later licence
versions may apply to updates You choose to install.]

### 8. Termination

This licence ends automatically if You breach it. On termination You must stop using and distributing the SDK.
Copies of Games You distributed before termination [TO CONFIRM: remain licensed to their end users / may continue to
be distributed].

### 9. Disclaimer of warranty

[TO CONFIRM by counsel] THE SDK IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT.

### 10. Limitation of liability

[TO CONFIRM by counsel] TO THE MAXIMUM EXTENT PERMITTED BY LAW, THE LICENSOR IS NOT LIABLE FOR ANY DAMAGES ARISING
FROM THE USE OF, OR INABILITY TO USE, THE SDK.

### 11. Governing law

[TO CONFIRM by counsel: governing law and venue.]

---

## Notes for counsel

- **Earlier MIT versions.** Versions of the previous implementation published while the repository was public were
  released under the MIT License, and recipients of those copies keep their rights. That implementation is kept in
  the repository history (tags `legacy-final` and `reference/pre-rebuild`) [TO CONFIRM: which commits were public].
  The current implementation is a greenfield rewrite made after the repository became private and has never been
  published as source; the owner is its sole author according to the repository history.
- **Runtime redistribution** must allow shipping the Runtime with Games on any platform the SDK targets, including
  browser builds where the Runtime is downloaded by players.
- **Third-party obligations** the SDK must satisfy are listed in `THIRD-PARTY-NOTICES.md` (MIT, zlib, OFL and public
  domain components; .NET runtime notices in browser builds).
- **Placement after approval:** the final text replaces the holding notice in `LICENSE`, ships inside every NuGet
  package (`PackageLicenseFile`, with `PackageRequireLicenseAcceptance`) and template, and is published on the site
  next to the third-party notices.
