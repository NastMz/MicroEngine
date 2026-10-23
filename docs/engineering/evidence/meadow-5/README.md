# MR-5 — Start, pause, resume and campaign restart
Technical review in progress; owner UX acceptance is not yet recorded.

## Play
```powershell
dotnet run --project C:\Proyectos\MicroEngine\examples\Platformer\Platformer.csproj -c Release
```
Space starts fromtitle. A/D/arrows move, Space jumps/continues/retries/replays asbefore.
P pauses/resumes. Whilepaused Up/Down changes Resume/Restartcampaign selection, Space confirms.
R explicitly restarts entirecampaign fromanyphase; Escape still exits. No save/autopause/focus
menu or remapping introduced. Pause polls/renders responsively but freezes game/animationclock.
Transitioninput is consumed; heldjump/movement duringmenu is not replayed afterresume.

## Design, assets and boundaries
[ADR0014](../../decisions/0014-game-session-input.md), [approved MR-5](../../specs/meadow-session-flow.md).
Onlyadapterchange: append semanticpause=P/restart=R to existingfixedDesktopprofile. No new Core
UI/scene/clock/lifecycle contract or nativeinput in game. Legacyknownaction consumers ignore
newstrings, with representativeLevelGame/Campaign/RPGexplore/battle regressions.
MeadowSession wraps unchangedCampaignGame; Title/Running/Paused andmenu selection belong to game.
Existing GamePlatform delegation clears currentCampaignbuffer afterrestart, focusloss behavior
unchanged/noautopause. Oldconstructors/factories remain valid. CLIusage/exits/counters/cleanup
retained; smoke canremain onTitle, fail-update still failsnormally.
AssetReadinessreadyreused existingart/defaultlabels, no newfont/menuPNG/audio/imagefiles.
StrictsessionDrawrequireslabels+imageregions beforeeffects; legacyDrawCampaign optional-label
behavior unchanged. Same9maxleases retained acrosspause/restart, no window or texture recreation.

## Author proof
DesktopnewP/R/order/querytests4RED ->40GREEN. Sessionstub6RED ->27PlatformerGREEN;
newpanel/pausedphase1RED ->28GREEN, focusreplacementbufferregression->29GREEN. RPGcompat22GREEN.
Allprior298tests retained; newtotal309. Managedtests cover titleconsumption, pausebody/enemy/
coin/level freeze, resume/restartpriority/order/helddiscard, invalidatomicity, clearlandingbuffer,
restartfromdeath/finallevel, postrestartfocusbufferforwarding and lifecycleforwarding.
Rendereractualpanellabels/selection/phasefreeze/resume/no-reacquire9leases allreleasedonce.
Nativeoriginalcourse/realart/patrol/gap/retry/CLI7 retained; newtitle/pause actualpanelpixels,
selectionarrowchange, frozenpublicstate/resume/restartsamewindow and captures PASS.
PhysicalP/R are fakekeyboardprovider tests, not OSinjectedkeypress evidence. Publicsemantic
snapshotdrivesnativegamejourney withoutproductionautomationflags.

OwnlongserialRelease309/309green,3native-NoBuildwrappers actualexit0 and3assetchecksPASS.
A laterouterPowerShellrerun hungafterPASSmarkers and wasinterrupted; not anextraexitconfirmed
run. Finalfresharchive/rebuildindependentgatepending. No requiredfailure silentlywaived.
Nativeframes inspectedtechnicalreadability only, not ownerergonomicsapproval.

## Subjective-only handoff after closure
Evaluate start/menu/pause/resume/restart clarity and whether the controls feel coherent.
No need manuallyrepeat clock/cleanup/hash/inputordering checks. Audio/music/particles/powers
remain laterstaged candidates, not bundled justtofillroadmaplist.

![Title](title.png)
![Paused](paused.png)
![Restart selected](selection.png)

## Independent objective closure — 2026-10-22
Frozen01719a6e69a0fd4ce33393b456d4a646d4786458 against6204b48682924451542712c08eb3318e7081ec57,
approveddesign9cb2fb5: session_final_verify S1-S4PASS, no blockers/corrections.
Freshgitarchive scratchMicroEngine-MR5-final-cc8ebac3aee14881be1f3e00616cf953/tree,
SDK10.0.402;309/309managed (Hosting31 Loop159 Desktop40 TwoD10 Platformer29 Adventure13
Rpg22 ExampleLaunch5), zero failures/skips. Allthreefresh-build nativewrappers andthreeasset
verifiers returnedactualexit0. Priorouterhang/unrecordedTwoDexit excluded; affectedchecks rerun
individually with explicitexit0. No marker-only success substituted for processcompletion.
Nativeoriginalcourse/art/patrol/campaign/CLI7 retained plus actualTitle/Pausedpanels/selection/
freeze/resume/restartsamewindow. Fakekeyboardtests proveP/Rprofile, not OSinjectedkeypresses.
ProtectedengineexceptDesktopActions hashba641a65bb759481c7f810a24b58617202fe9cf666ef8d54a9454c313f20f020;
allassets hash63cab8e1164b7f7e2cccff7b371c8fe8e829efc622b8eb8215239f16719cf06d unchanged.
UnchangedCampaignGame/LevelGame/LevelContent/othergames/dependencies individuallyverified.
ParentcurrentPlatformerNativeSmoke-NoBuild/session/CLI7 exited0. Rootaccepts objectiveMR5/
ME081/082; newstart/pause/resume/restart UX remains owner-owned/unaccepted. No main/push.

## Current next boundary
No furtherpreparedimplementation. The newcomplete-session interaction is meaningfulplayable
UX, unlike a technicalinput refactor; ownerexperience is appropriate now. Audio/music, effects,
powers remain stagedcandidates requiring their ownresource/readiness/contracts, not bundled.
Riskitem3 additiveadapterinputprofile/consumerflow; oldactions/knownactionconsumers, clocks,
resourceownership andprotectedboundaries independentlyverified. RDDoff, no memoryidentity.

## Owner feedback — 2026-10-22
Owner: "Funciona perfectamente, continua" afterMR5sessionhandoff. Root records positive
subjective evaluation of boundedTitle/pause/resume/restart at01719a6/d46ed80, backed by309tests
and independentnative/assetproof. No audio/music or otherfuturecapability acceptance inferred.
Normal localmain integration authorized for evaluatedMR5 only; no remote operation.
