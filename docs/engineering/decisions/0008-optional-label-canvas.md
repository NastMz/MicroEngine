# ADR-0008 — Optional simple label profile

Status: accepted2026-10-17 by Codex root after checkpoint_design_review PASS, authority [autonomy](../autonomy.md); CAP-004/CAP-012, ADR-0004, [NL-1](../specs/optional-labels.md).

## Concrete need and choice

TurnExplorer names Advance/Return only in the console while drawing glyph rows in-window. Prior positive feedback is preserved; this is capability discovery, not a retroactive defect or full UI commitment. Add optional ILabelCanvas : ICanvas in Loop, a single Label method for short ASCII labels in a provider-owned default face. Leave ICanvas/IFramePlatform unchanged. Consumer discovers with `canvas is ILabelCanvas`; absent support retains exact primitive fallback.

Alternatives: widen ICanvas (reject: breaks primitive providers); consumer bitmap alphabet (reject: duplicates font rasterization for literal labels); Raylib calls from game (reject: vendor coupling); optional text-resource/layout/UI framework (defer: no font assets/measurement/shaping/widget use case). Small optional profile selected; future richer text may use a distinct cohesive capability, not force this method into universal coverage.

## Boundaries and costs

Short-label profile explicitly limits printable ASCII/80characters and10..64pixel requested size. Bounds keep this native convenience finite, not a Unicode/font/typography/performance promise. Provider chooses font appearance/metrics; no exact glyph rasterization guarantee, resource handle or font-file dependency. Default face owned by adapter/window lifetime; failure/lifetime checks match existing FrameApi origin semantics. Native package remains Desktop-only, tests can implement public profile without friendship or vendor reference. No new platform/thread/driver assumption: Desktop remains Windows creating-thread-only, and headless/nonlabel canvases unaffected.

Raylib native DrawText uses its default face; size20 matches the default font scaling guidance. Evidence checked2026-10-17: [official native API](https://github.com/raysan5/raylib/blob/master/tools/rlparser/output/raylib_api.txt), [font FAQ](https://github.com/raysan5/raylib/wiki/Frequently-Asked-Questions), [pinned Raylib-cs8.1.0 binding](https://github.com/raylib-cs/raylib-cs/blob/v8.1.0/Raylib-cs/interop/Raylib.cs). Installed package remains8.1.0, no upgrade or support claim from current upstream docs.

## Approval

Independent checkpoint_design_review PASS/no blockers before implementation. Native probe samples changed RGB, not alpha, and unloads images before window cleanup. Approval under existing technical delegation; no universal text/UI support promise.
