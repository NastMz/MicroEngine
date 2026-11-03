# P7 measurements (2026-11-03, .NET 10.0.14, Microsoft Windows 10.0.26300, X64, 12 logical CPUs)

## Attachments (1000 entities, random access order)

| Operation | ns/op | bytes/op |
| --- | ---: | ---: |
| DenseStable: TryGet<class> hit (4 types attached) | 2.78 | 0 |
| DenseStable: TryGet<class> miss (type not attached) | 1.60 | 0 |
| DenseStable: Value<struct> by ref, read+write | 2.78 | 0 |
| DenseStable: All<class> iteration, every entity has it (per element) | 1.18 | 0 |
| DenseStable: All<class> iteration, 1 in 10 has it (per element found) | 2.18 | 0.002 |
| PerEntityDictionary: TryGet<class> hit (4 types attached) | 12.34 | 0 |
| PerEntityDictionary: TryGet<class> miss (type not attached) | 8.40 | 0 |
| PerEntityDictionary: Value<struct> by ref, read+write | 12.53 | 0 |
| PerEntityDictionary: All<class> iteration, every entity has it (per element) | 12.11 | 0 |
| PerEntityDictionary: All<class> iteration, 1 in 10 has it (per element found) | 96.88 | 0.002 |
| Baseline: a List-like array of class references (per element) | 0.40 | 0 |

## Transforms, interpolation and hierarchy (1000 entities)

| Operation | ns/op | bytes/op |
| --- | ---: | ---: |
| Local(id).Position2D read, ids in creation order | 1.30 | 0 |
| Local(id).Position2D read, random order (Actor.Position get) | 1.34 | 0 |
| Local(id).Position2D += delta (Actor.Position set) | 1.59 | 0 |
| Baseline: Vector2 field read from an array | 0.60 | 0 |
| DrawPosition2D(id, 0.5), root entity | 2.61 | 0 |
| Baseline: the game's own lerp (Actor.Blend over two Vector2 arrays) | 0.81 | 0 |
| WorldMatrix(id, 0.5), root entity | 32.88 | 0 |
| BeginStep (keep the step-start transforms), per entity | 0.59 | 0 |
| Baseline: copying Vector2 positions, per entity | 0.06 | 0 |
| DrawPosition2D(id, 0.5), child at depth 1 | 25.65 | 0 |
| DrawPosition2D(id, 0.5), child at depth 3 | 59.86 | 0 |

## Entity churn (particle-like use)

| Operation | ns/op | bytes/op |
| --- | ---: | ---: |
| DenseStable: Create + Attach<class> + Destroy + release, per entity (steady state) | 44.31 | 0.002 |
| PerEntityDictionary: Create + Attach<class> + Destroy + release, per entity (steady state) | 103.21 | 240.002 |

(sink 0)

## Courtyard Key, whole frames (headless, deterministic, muted, captures removed from the scripts; update + draw per frame; one warm-up pass, then best of 7 interleaved rounds)

| Script | Frames | Original µs/frame | Port (dense) µs/frame | Port (dictionary) µs/frame | Original B/frame | Port (dense) B/frame | Port (dictionary) B/frame |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| fight.script | 360 | 400.6 | 408.4 | 405.4 | 64213 | 64234 | 64245 |
| boss.script | 520 | 742.0 | 709.9 | 714.9 | 384238 | 384238 | 384202 |
| showcase.script | 140 | 1189.4 | 1198.9 | 1230.7 | 309881 | 309867 | 309769 |
| gameover.script | 720 | 212.5 | 218.8 | 213.6 | 36564 | 36592 | 36574 |
| opening.script | 1100 | 152.1 | 152.6 | 152.8 | 31442 | 31436 | 31422 |
