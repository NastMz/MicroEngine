# ADR-0009 — Optional image values and context-owned resource leases

Status: accepted2026-10-17 by Codex root after checkpoint_design_review PASS; technical [autonomy](../autonomy.md), CAP-004/006, ADR-0004, [IMG-1](../specs/optional-images.md).

## Need, choice and alternatives

FirstPlayable procedural AV-1 content has never exercised image upload or ownership. Add one optional image profile and a tiny in-memory avatar badge while preserving every existing rectangle/pose. No decoder/file format/atlas/cache/asset manager/tint/rotation/scene system.

Use immutable copied CPU PixelImage values and opaque disposable IImageResource leases created by IImageCanvas : ICanvas. Resources belong to their creating provider context, not interchangeable native IDs; consumers can release outside drawing, while window shutdown attempts remaining releases in reverse order before context closure. Cached metadata survives disposal; a released lease cannot draw again. No retained-resource methods added to mandatory ICanvas/IFramePlatform.

Rejected alternatives: vendor texture handles in games (coupling); per-frame upload/dispose (unnecessary churn and deferred draw hazards); implicit forever cache of image values (no early release/budget control); mandatory image methods (breaks primitive providers); generic asset/loading framework (speculative). Explicit small lease contract chosen despite lifecycle cost because ownership is the capability being validated, not hidden in a cache.

## Constraints and consequences

Desktop creating-thread/drawing boundaries remain adapter/profile restrictions, not future universal scheduling. Create/draw during committed presentation; dispose a live lease only outside drawing, preventing premature native unload of queued draws. During FrameLoop update a prior lease may be released; after FrameLoop window cleanup it is already invalidated, so same-thread Dispose is a no-op. No runtime reconstruction/Loop rewrite required. A future asynchronous driver/profile need not use this synchronous capability.

Desktop initially limits images to256x256 and32live leases: finite local native budget, not PixelImage or all-provider limits. Valid unsupported uploads throw NotSupportedException (ordinary failure), not API misuse. Actual GPU/native failures remain reported execution failures; failed release invalidates lease, surfaces error, and window closure remains last context reclamation attempt. Closure failures retain existing poisoned-window reservation behavior.

Primary binding checked2026-10-17: [Raylib-cs8.1.0](https://github.com/raylib-cs/raylib-cs/blob/v8.1.0/Raylib-cs/interop/Raylib.cs) exposes image upload/texture validity/unload/draw; native types remain Desktop-only, plus isolated native test code. No package/version update. Native pixel/resource proof required; no portability/performance claim from one Windows adapter.

## Approval

Independent checkpoint_design_review PASS/no blockers before construction; original acquisition errors must survive compound cleanup, and zero draws still validate owner/resource/geometry. Approved under technical delegation, not a universal asset/resource architecture.
