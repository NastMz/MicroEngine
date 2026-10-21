# MR-3 — Authored content, distinct challenge
Status: approved2026-10-21 by root after independent distinct_levels_map design PASS.
FR001/002,NFR001/002/004/005/006; ADR0004/0012 unchanged. Consumer-only; no engineADR needed.

## Evaluation and choice
MR-2's constructor branches duplicate layout policy while controller/renderer already consume
instance geometry. Threecoin assumptions exist in model, HUD and test driver; camera/world
size remain shared1920x480 and768viewport, not demonstrated variability requirements.
StaticWorld accepts separate floorsegments and touching steps; images/regions already support
these compositions. No engine change, tile/editor/scene API or externalcontentformat needed.
Use a sealed immutable LevelContent class in Platformer.Model with private authored constructor,
get-only Terrain,Coins,Danger,Goal; read-only copied arrays; static First/Second and For(int)
(1/2 else ArgumentOutOfRangeException). No arbitrary user-generated content schema.
LevelGame statics alias First (referenceidentitypreserved), constructor selects one content,
collectedarray derives Coins.Count, completion uses count; public CoinCount for shared HUD.
Existing LevelNumber/Terrain/CoinPositions/Danger/Goal remain stable. One controller/worldcreation
and renderer remain; no per-level rule switch. Campaign/factory/physics/window/cache unchanged.

## Exact second content
Level1 byte-equivalent values to accepted KPF1. Level2:
floor(0,432,480,48),floor(576,432,1344,48),step(720,384,96,48),step(816,336,96,96).
Coins(240,400,24,24),(864,304,24,24),(1368,400,24,24).
Danger(1536,412,48,20);Goal(1824,336,48,96).
First96unit gap must be intentionally jumped; secondcoin requires raisedroute rather than
walking allthreecoins on floor. Falling Y>528 remains death; no new mechanic needed.
No bridging artwork drawn across the physical gap. Tiletops/centers correctly clipped.
Content explains challenge rather than simply changing coordinateoffsets. Threecoins still
chosen by content, not mandatory generic schema. No thirdlevel/enemy/audio/menu in this slice.

## Asset Readiness — ready, reused
Existing originalKenney NewPlatformerPack1.1 CC0 PNG/XML/license/provenance; no missingfiles,
no download/transform. Grass top/center construct steppedterrain and segmentedfloor; coins,
spikes/flag/player unchanged. Gap is realcollisionspace, not a decorative overlay. Same7leases,
902atlas/2048budget, characterframes/timing and sourceprovenance. Nativecapture confirms visual
gap/elevatedcoin readability; pixel/loadproof does not substitute for jump/pacing experience.
Prior publicaudio shortlist retained candidateonly; no license/listening/formatselection needed.

## Acceptance and execution
ME074 evaluation/design/readiness; ME075 implementation; ME076 verification/handoff.
Allowed: new examples/Platformer.Model/LevelContent.cs,LevelGame.cs; PlatformerPresentation.cs;
checks/Platformer.Contracts/Course.cs,CampaignTests.cs,CampaignPresentationTests.cs,newDistinctLevelTests.cs;
checks/Platformer.NativeProbe/Program.cs; linkedengineeringdocs/log. No engine/deps/asset/othergame/
CampaignGame/GamePlatform/Program changes. Rollback contentseam/newsecondlayout scopednormalrevert.
Risk item3 shared consumercontent contract, preserve legacy defaultstatics/oldcourse/replay.

New runnable tests RED beforecode prove actualgap/elevatedcoin fromexisting LevelGame observable
geometry, nojump horizontalwalk eventuallydiesingap (notatfarspikes), oldMR2route withoutgapinput
failscurrentnewchallenge (do not weaken publiccourse toteleport). Then newdriverpublicinput
anticipatesgap while oldlevel1route remains same steps/result. Eachlevel<=2000updates, allcoins,
flag, retry/finalrestart preserved. No state injection. Assert coin placement requires leaving
floor and renderer emits no groundtile insidegap plus draws elevatedcoin at contentdestination.
Immutablecontenttests/copy/read-only, originalstaticidentity and defaultvalues retained.
Full289baseline+newtests, same7lease/animationHUDtests, all3representative nativeharnesses and
originalassetchecks. Nativeprobe samples onelevel1solid coordinate and level2gap counterpart,
actual newgapcapture/elevatedcoin, drives publicroute bothlevels/retry/finalrestart; originallevel1
pixel/defaultbudget/coursechecks remain. Adapt secondleveldeathdriver toexpectfirstgapdeath,
not silentlyclaimspikedeath. Independent fresharchiveverification/protectedparity. Only new
significant gap/elevatedroute feel/clarity requires ownerplay after objectiveclosure.
