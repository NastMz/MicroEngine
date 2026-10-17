# ADR-0010 — Optional source-region image drawing

Status: accepted2026-10-17 by Codex root after checkpoint_design_review PASS; [SPR-1](../specs/sprite-regions.md), CAP-004/006/008/016, ADR-0004/0009. Independent review preceded implementation approval.

## Need and decision

IMG-1 proves whole-image resource ownership but not reusable sprite presentation. Existing external AvatarAnimation already selects idle, two walking frames, air and facing. Exercise one compact CPU-authored sprite sheet without adding engine animation state or asset formats.

Propose additive IImageRegionCanvas : IImageCanvas with one DrawImageRegion(resource,source,destination). Use existing Rect for both; source must describe positive integer pixel bounds inside its image. Optional discovery avoids breaking custom IImageCanvas providers. Destination/lifetime/sampling/alpha/failure rules inherit IMG-1; source/destination and resource are fully validated even when destination is zero. Negative regions never mean backend-specific flipping. Facing is authored as separate consumer cells, no new engine flip API.

## Alternatives and consequences

- Add region method to IImageCanvas: rejected because existing implementations would break or receive an artificial default implementation.
- Eight independent whole-image leases: possible but avoids the actual region capability and multiplies resources for one source sheet; retain existing whole-image API for providers that omit regions.
- Generic atlas/clip/animation/asset API: deferred without a real second use case. No decoder/files/cache/name lookup/automatic batching.
- Vendor draw calls in consumer: reject ADR-0004 coupling.
- New integer rectangle public type: unnecessary second geometry contract in this slice; explicitly constrain existing Rect source values. This does not promise arbitrary fractional region/filter semantics.

Public commitment is one separate optional synchronous profile. Engine remains infrastructure-independent; no universal feature support, threading, portability or export promise. Native storage and GPU limits remain adapter-owned. CPU raster authoring and animation-to-cell mapping stay consumer-private and replaceable. Sprite output must match the positively evaluated existing avatar pixels; this increment validates the path, not artwork quality.

## Approval

Independent checkpoint_design_review PASS/no blockers. Codex root approves under technical delegation; source bounds must be overflow-safe and transparent badge composition must preserve underlying torso pixels. Owner product vision and H1 criteria are unchanged.
