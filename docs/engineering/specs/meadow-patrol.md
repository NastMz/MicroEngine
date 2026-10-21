# MR-4 — Avoid-only spiked slime
Status: approved2026-10-21 by root after independent enemy_scope_map review and targeted contact-premise recheck.
FR001/002,NFR001/002/004/005/006; existing public Loop/TwoD/Desktop contracts unchanged.

## Scope and ordering
Add one patrol only to level2, after raisedroute and before finalcoin. No stomp, bounce,
enemy defeat, HP/lives, randomAI, audio, menu, save or newenginephysics/ECS/AI/animation API.
Game-owned immutable PatrolContent(Rect Spawn,float MinX,float MaxX,float Speed) in
Platformer.Model, authored through LevelContent nullable Patrol get-only; First null,
Second spawn(1152,392,40,40),MinX1056,MaxX1248,Speed60. Bounds are leftedgepositions.
Public LevelGame.EnemyBody is nullableRect, computed from current patrol X and spawnY/size.
Startmovingright. Exactbound equality reverses immediately; overshoot is reflected in the sameupdate. For each valid nonterminalupdate, moveplayer asbefore then advancepatrol
by Speed*delta with exactovershootreflection atleft/right. Delta<=100ms ensures max6units,
farbelow patrolspan. No mutableactorcollection/spawnregistry or general dynamiccollision system.
Strict StaticWorld.Overlaps(player,enemy) after movement latchesDead/Freeze before spike/fall/
coin/goal; avoid-onlycontactfromanydirection incltop. Freeze enemyposition andanimation on
Dead/Completed. Freshretry resets currentlevel/patrolposition/direction/animation; retryinput
consumed. Null/delta validation before mutation. Inactiveframe already freezes updates through
Loop and clears gamejumpbuffer; no new nativeinput rules. Level1 unchanged.

## Asset Readiness — ready before construction
Original Kenney New Platformer Pack1.1 public page https://kenney.nl/assets/new-platformer-pack
and embedded existingCC0-1.0license; same archivehash/source URL recorded in originalprovenance.
Requiredmissingresources: actualspikedenemy walkposes, not importedplayercolorvariants.
Select only original Sprites/Enemies/Default/slime_spike_walk_a.png and walk_b.png, both64x64,
RGBAtransparent, samecartoonstyle; two poses visuallyinspected, no separatepack required.
New patrol-provenance.json records archive/source/fileSHA; oldprovenance/files unchanged.
Render at64x64, centered on40x40collider (x-12,y+height-64), samebaseline432; visualpadding
explicit, not pixel-perfectcollision. Spikes communicate avoid-only rule (no stomp).
Advance framephase every120ms using same simulationdelta; phase freezes in terminalstates,
resets onlevelinstance/run; terminalupdates retain currentphase, never Reset eachframe. Simpletwofilephase logic is gamepresentation, not a new engine
animator; existing ImageSet owns both extraimages, total9 acrosscampaign, originallevel1 still7.
No imageflipping/backendAPI change. HUD existingsecondline may clarify "Avoid spiked slime"
while activelevel2; preserve prompts/CLIhelp. Verify realPNGpixels/pose distinction/collider
composition in nativecapture andmanagedcanvas cache/disposal tests; no claim ofaestheticacceptance.

## Execution packet and acceptance
ME077 assets/spec; ME078 model/presentation/tests; ME079 independentproof. Allowed:
LevelContent.cs,LevelGame.cs,new PatrolContent.cs; PlatformerPresentation.cs;
Assets/slime_spike_walk_a.png,slime_spike_walk_b.png,patrol-provenance.json;
checks/Platformer.Contracts/Course.cs,newPatrolTests.cs,CampaignPresentationTests.cs;
checks/Platformer.NativeProbe/Program.cs,PlatformerNativeSmoke.ps1;
new checks/VerifyPlatformerPatrolAssets.ps1; linkedengineeringdocs/log. No engine/CampaignGame/
GamePlatform/Program/deps/othergames or oldassetbytes changes. Normal scopedrevert, no data migration.
Risk item3 shared gameconsumerstate, independentlyreview beforeobjectiveclosure.

RunnableRED beforeimplementation: level2nullableenemy/spawn, simulationreflection/bounds,
invalidatomicity/freeze/retryinitialdirection, publicroutecontactdeathbeforefarspikes thenretry,
avoidancecompletion using testdriverjump nearactualenemy (not productionautojump); Level1
still517steps/replay and noenemy. Existing293tests preserved; game/rendererstate remains pure.
Managedrenderer checks bothrealposekeys,120msphase/freeze/reset and9leases/releaseonce.
Native originalKPF/MR2/MR3 proofs retained, add enemyoriginalPNGpixels/pose/framebuffer,
publiccontactdeath/currentretry/avoidancecampaigncompletion and no newwindow/providerlease
recreation across levels. Adapt testdriver to anticipate enemy from publicEnemyBody; optional
stopcondition letscontacttest approachviaordinaryinputs, not teleport/privatewrites.
Allthreeproductionnativeharnesses, full293+newtests, original/newassetintegrity, independent
fresharchive/protectedparity andparentnative spotcheck. Onlynewmovinghazard readability/feel
needs ownerexperience afterproof; no routinehumanapproval for localtechnical or freeassets.
