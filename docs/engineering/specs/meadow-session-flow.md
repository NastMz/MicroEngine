# MR-5 — Game-owned start/pause/restart
Status: accepted2026-10-21 by root under technical delegation after independent session_flow_map design PASS and targeted ordering/surface corrections.
FR001/002,NFR001/002/004/005; existinggame/engine contracts retained.

## Adapter boundary
[ADR0014](../decisions/0014-game-session-input.md): DesktopActions adds pause=P/restart=R
held/pressed at end, eachonecoalesced; defaultfiveunmodified. Escapecontinuesexit-only.
No new engine pause/lifecycle/UI API, nativeaccess in game, focusautopause or keyremapping.

## Exact game contract
New public enum SessionPhase { Title, Running, Paused }, sealed MeadowSession parameterless
startsTitle with CampaignGame Campaign {get;private set;} newinstance, SessionPhase Phase {get;},
int Selection {get;} (0Resume,1Restart), bool AdvancedThisUpdate {get;} and public void
Update(TimeSpan,InputSnapshot),ClearJumpBuffer(). Existing CampaignGame/LevelGame unchanged.
Validate null then0<delta<=100ms before anymutation includingAdvancedThisUpdate.
EachvalidUpdate setsAdvanced=false. Priority independent ofedgelistorder: restart first,
thenphase-appropriatepause, thenotheractions; atmostone transition, wholeupdateconsumed.
Freshrestart in anyphase createsnewCampaign, PhaseRunning, Selection0. Titlefreshjump starts
Running without alsojump/movement, otherwiseignoreinput (pauseignored); heldedges don'tact.
Runningpause clearsjumpbuffer, PhasePaused, Selection0, no campaignupdate. Otherwise forwards
Update toCampaign andsetsAdvanced=true (includingitsdeath/continue/replay behavior).
Pausedpause clearsbuffer, resets Selection0 andresumes sameCampaign; otherwise firstfreshup/down togglesselection
once; firstfreshjump confirmscurrentselection (0resume sameCampaign,1newCampaign restart).
Process pausedup/down/jump in suppliedorder; at most one selectiontoggle pervalidupdate, then firstjump confirms andconsumes. Example [up,down,jump] toggles once then confirms Restart. Resume resets Selection0. Stopafterfirstjumptoavoidmultipleactions;
heldjump/movement andallotheredges discarded. Nothing accumulatespaused. Restart/resumeticks
clearbuffer/consumeallinput; nexttickusesonlyfreshsnapshot. ExistingLoopfocusdiscard remains,
no automaticpauseonfocusloss or newpublicactive-statecontract.

Presentation named AdvanceSession(TimeSpan,MeadowSession) onlyadvancescampaignanimation if
AdvancedThisUpdate=true; DrawSession requires ILabelCanvas +IImageRegionCanvas before effects.
Legacy DrawCampaign remains optional-label compatible; this stricterprecondition applies only to newDrawSession. Draw underlyingfrozenCampaign with existingart then highcontrastpanel Title orPaused. Title:
"Meadow Run", "Space: start", "P: pause | R: restart | Esc: exit". Paused: "Paused", selection
"> Resume"/"  Resume" and "> Restart campaign"/"  Restart campaign", "Up/Down + Space | P: resume".
RunningexistingHUD adds conciseP/Rhelp withoutlabeloverflow. Nonewimages/caches/windows.
GamePlatform adds namedForSession(IFramePlatform,MeadowSession) privateactionctorforwarding
currentCampaignbuffer throughsession. Retain existingconstructors/factories/nullcompatibility.
ProductionProgram usesMeadowSession, updatedconsolehelp. CLIflags/exitcodes/counters/cleanup
unchanged; smoke may stay onTitle, fail-update stillthrows beforeanysessionupdate.

## Asset Readiness — ready/reuse
Existing originalKenneyart/provenance andDesktopdefaultlabels suffice. Nofonts/icons/menuPNG/
externaldownload needed. Highcontrastpanel/selectionlabels technicalnativecapture+managedtests;
no inventedmissingcontent, no generalfont/UIframework. Same9texturesatmaxacrossgame, pause/menu
mustnotrelease/reacquire. No audio/mix/particlerequirement inferred.

## Execution and proof
ME080 ADR/spec;ME081 source/tests;ME082 independentnativeclosure. Allowed exact surfaces: engine/Desktop/DesktopActions.cs;
checks/Desktop.Contracts/DesktopActionTests.cs;
examples/Platformer.Model/MeadowSession.cs (new);
examples/Platformer/PlatformerPresentation.cs;
examples/Platformer/GamePlatform.cs;
examples/Platformer/Program.cs;
checks/Platformer.Contracts/SessionTests.cs (new);
checks/Platformer.Contracts/CampaignPresentationTests.cs;
checks/Rpg.Contracts/SessionInputCompatibilityTests.cs (new);
checks/Platformer.NativeProbe/Program.cs;
checks/PlatformerNativeSmoke.ps1; linkedengineeringdocs/log.
No otherengine/defaultpolicy/model/assets/
dependencychanges. Riskitem3 sharedinputprofile evolution. Rollback scopedsource normally.

RED beforeimplementation for P/Rheld/pressed/oldaliases+exactoldfiveprefix, sessionTitleconsumption,
pausefreezesbody/enemy/coin/level/clock/animation, resume/restartconsumption, menuordering/selection,
wrongcase/helddiscard, invalidatomicity, restartfromlevel2/death/finalcompletion. Representative legacy LevelGame/CampaignGame and RPG explore/battle tests ignore pause/restart as before; static audit confirms other consumers only act on known actions. Existing298tests
retained, CampaignGame standalone still startsRunningasis. Managed9lease/phase tests retain allproof;
sessionpause/resume samephaseimage, restartresets; nativeactualoverlay/selection/clearresume with
samewindow andactualsourcepixels. Fakekeyboardprovider tests coverphysicalP/Rmapping; don't
claim OSkeyinjection evidence. Publicsessioninputdrivesnativejourney withoutproductiondebugflags.
RunDesktop/Platformer focused, full298+newcases,3native/3assetverifiers, independentfresharchive/
protectedparity andparentnativeexit0. Subjectivegate only actualstart/pause/resume/restartclarity;
technicalchecks automated. No main/pushbefore applicableownerfeedback.
