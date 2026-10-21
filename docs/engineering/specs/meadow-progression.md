# MR-2 — Two-level Meadow Run progression
Status: approved2026-10-21 by root after independent meadow_phase2_map design PASS and targeted compatibility corrections.
FR001/002,NFR001/002/004/005/006; ADR0004/0012 and KPF-1 unchanged engine contracts.

## Boundaries and design
Keep parameterless LevelGame, static Solids/Coins/Hazard/Flag and original reset behavior
identical for existing callers/tests. Add LevelGame(int levelNumber) accepting1or2 (other values
ArgumentOutOfRangeException before construction effects). Add instance LevelNumber,Terrain,
CoinPositions,Danger,Goal; these immutable views are chosen by level. New constructor builds
StaticWorld from Terrain. No engine level/scene/transition system or gameplay inheritance.
Body physics/geometry dimensions,3coins,frame buffer and movement remain unchanged.
Level1 exact existing content; level2 floor unchanged, obstacles at(384,384,96,48),
(480,360,96,72),(864,336,144,24),(1200,384,144,48); coins(240,400,24,24),
(720,400,24,24),(1392,400,24,24); hazard(1536,412,48,20);flag(1824,336,48,96).
All retain1920x480world and explicit768viewport/camera bounds. No fake new mechanic.

Game-owned public sealed CampaignGame has a parameterless constructor, public LevelGame Current {get;}, public int LevelNumber {get;}, public bool Completed {get;} (final only), public void Update(TimeSpan delta, InputSnapshot input),
public void ClearJumpBuffer(). Startlevel1. Current.Completed=true after first flag but campaign.Completed=false; after second flag both true. Level1 completion freezes atflag, freshjump advances to newlevel2
and consumes update/heldmovement; heldjump alone does nothing; unknown/wrong-case edges do nothing. Death resets currentlevel only
through existing LevelGame handling (freshjump). Level2 completion freezes finalvictory;
freshjump creates newlevel1/resetwholecampaign and consumesupdate. No samecompletionupdate
advance/reset. Validate null then0<delta<=100ms before any phase mutation. No disk/persistent
progress/lives/checkpoints beyond currentlevel. ClearJumpBuffer always forwards currentlevel.

Production Platformer uses CampaignGame; GamePlatform retains existing (IFramePlatform,LevelGame) constructor and adds named static GamePlatform ForCampaign(IFramePlatform,CampaignGame), delegating to a private action-based constructor. No ambiguous null overload; provider ownership/order unchanged. Presentation keeps
old LevelGame Draw/Advance output and uses instancegeometry; add named DrawCampaign(ICanvas,CampaignGame) and AdvanceCampaign(TimeSpan,CampaignGame) methods for HUD
Level1/2 or2/2 and accurate Space-to-continue/replay/retry text. Reset animation when active
level instance changes, reuse same seven ImageSet leases and window. Original CLI unchanged.

## Asset Readiness — ready
All MR-2 functions reuse existing Kenney New Platformer Pack1.1 original CC0-1.0 sevenPNGs,
characterXML/license/provenance under examples/Platformer/Assets. Existing902characteratlas,
128characterframes/64tile regions and2048explicitproviderbudget unchanged. No missing assets,
new downloads, transformations, music/SFX/UI art/enemy/VFX required for this scope. References:
[existing asset specification](kenney-platformer.md), [phase roadmap/shortlist](../evidence/meadow-2/roadmap.md).
Level2 uses whole grass tops/centers with clippededge source ratios as before; sprite/collider
alignment and readable course/coin/hazard/flag/HUD contrast validated via nativecapture.
License/file hashes unchanged; do not claim unavailable audio/particles as implemented.

## Execution packet
ME071 process/roadmap/assets/design; ME072 consumer implementation/tests after reviewedspec;
ME073 independent native/protected regression proof after implementation. Allowed surfaces:
examples/Platformer.Model/LevelGame.cs,newCampaignGame.cs; PlatformerPresentation.cs,
GamePlatform.cs,Program.cs; checks/Platformer.Contracts/Course.cs,newCampaignTests.cs,newCampaignPresentationTests.cs,Platformer.Contracts.csproj;
checks/Platformer.NativeProbe/Program.cs,checks/PlatformerNativeSmoke.ps1 and linked docs/logs.
No engine/assets/dependencies/oldgame changes. Local featurecommits only; no push/main.

## Acceptance and evidence
Preserve all original6platformer and all283baseline tests. New tests runnableRED beforecode:
constructor/contentimmutability+invalidIDs; originallevel1route/replay equivalence; public-input
level1continue/level2win/finalrestart; heldedge/consumedupdate; currentleveldeath/retry and
invalidinputatomicity in terminalstates. Public course adapts instancecontent; each level must finish within2000updates using existing Course rule and exact per-level coin/hazard/obstacle/flag positions. Invalidlevel0/3 tests show existing activegame state unchanged; move _world construction from fieldinitializer after ID validation.
Native retains every previous originalcourse/pixel/defaultbudget/decoder/replay/CLI assertion,
then drives both campaignlevels and retry/finalrestart with realrenderer and samewindow.
Validate actual level2 HUD/changed geometry/captures. Add managed counting IImageRegionCanvas proof of exactly seven acquisitions across both levels/finalrestart and disposal; animation reset test uses different active instances with RunNumber0.
Run Platformer.Contracts and PlatformerNativeSmoke then full283+newcases, Adventure/Rpg
representative native regressions, originalasset integrity/protectedpaths and independent
fresharchiveverification. Record failures/skips. New two-level flow needs owner subjective
progression/clarity/feel evaluation after objectiveproof; not manual repetition of tests.
Risk item3 consumer contract evolution, preserve default callers and existinggame semantics.
Rollback scopednewcampaign/additivelevelcontent, no data/history mutation.
