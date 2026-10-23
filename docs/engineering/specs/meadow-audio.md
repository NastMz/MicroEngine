# MR-6 — Four event-driven short sounds
Status: approved2026-10-22 by root after independent ADR0015/spec review PASS, before source construction.
FR001/002,NFR001/002/004/005, ADR0004; [audio decision](../decisions/0015-optional-one-shot-audio.md).

## Asset Readiness
Currentgamehas no soundfiles. Select four originals from MoxieCat8-bitPlatformerSFX,
https://opengameart.org/content/8-bit-platformer-sfx-0 (publisherCC0, individualWAVs, no embedded
license). AuthoredNOTICE isnotanupstreamlicense; provenance records individualURLs/SHA and
pageCC0declaration. jump_1.wav -> jump, button.wav -> coin, playerhurt.wav -> defeat,
spring.wav -> level-complete. AllactualPCM16mono44100Hz,0.287..0.501s. No musicselection,
newimage/font/VFX or visualscope. Distinct shortsyntheticcues avoidmixingmultipleaudiopacks;
association is a boundedchoice not establishedaestheticfit. Preserveoriginals; provider gain0.5,
fourvoices measured peak sum times0.5 remains below0.45; recordwaveformpeak/RMS/duration toavoidfalse
clippingclaims. Nativecodec/sampleownershipverified, subjectivevolume/fit ownergate atdelivery.

## Game events and policy
LevelGame emits get-only IReadOnlyList<LevelEvent> EventsThisUpdate, enum Jumped,CoinCollected,
Defeated,LevelCompleted. Clear aftervalidinputvalidation, beforeupdate; invalidcalls leave prior
list/stateunchanged. Launch emitJumped once inclbufferedlanding; coin loopemitone pernewcoin,
Defeated onlyontransition beforecoin/goal; Completed once. Temporalorder preserved. Terminal
holds/retryvalidupdates clearlist; freshretry/continue/start/pause/restart emitsnooldgameevents.
No eventqueue inCore, no durablelog/multicastbus. Headlessmodels/oldgamesneednoaudio.
MeadowSoundFeedback called once aftereachsuccessfulMeadowSession.Update. IfnotAdvanced, no
cue; paused/title/consumed transitions never replaystaleevents. Map gameevents to fourstrings.
StopAll whennotRunning orcampaign/currentinstance/run changes; initialpublish justsyncs,
neverplaysoldhistory. While paused no newcues; reset/leveltransitionstopsanyoldoneshot.
Multiplecueevents inoneupdate preserveorder, samecue restartsvoice; sounddoesnotmodifygame.

## Backend/lifecycle
IOneShotAudio genericcontract/backendprofile perADR0015. Optionalabsence/null valid, never
initializesaudio implicitlyinheadless/tracerconsumers. Providerunavailable fallback logs clear
warning, gameplaycontinues; invalidcontent/load/Play/cleanupfailuresnotignored. NewSoundPlatform
owns existingIFramePlatform andoptionalIOneShotAudio, forwardsPoll/Canvas/Begin/End, staticCreate
factories clean audioifwindowfails. Disposeattempts bothwindow thenaudio/aggregates. FrameLoop
continuesowningoneprovider. No new productionCLIflag or changedusage/exits; legacyCLIbadinput
stillrejectsbeforeanynativeacquisition. Physicalaudibility not claimed from nativePlay success.

## Tests, edit surfaces and delivery
ME083 preparation; ME084 contracts/provider+fakefault tests; ME085 events/consumer/native;
ME086 independentclosure. Allowed newengine/Audio/*, engine/Desktop/DesktopOneShotAudio.cs,
DesktopAudioApi.cs,WaveFile.cs,Desktop.csproj; checks/Desktop.Contracts/AudioTests.cs(new);
Platformer.Model/LevelGame.cs,newLevelEvent.cs; Platformer/Program.cs,newMeadowSoundFeedback.cs,
newSoundPlatform.cs,Assets/Audio/*; Platformer.Contracts/newAudioFeedbackTests.cs,Course.cs,csproj;
Platformer.NativeProbe/Program.cs,PlatformerNativeSmoke.ps1;newAudioNativeProbe/newAudioSmoke/
newVerifyPlatformerAudioAssets; linkedengineeringdocs/log. No otherengine/action/loop/Hosting,
othergames/models/assets/defaultinput/dependencyversion changes. Risk3+4+5 (newoptionalcontract,
callbackresourceownership, projectreference); independentreview required. Localfeaturecommits,
no newmain/pushbeforeapplicableownerfeedback. Normalrevert scopednewaudio/events/integration.

RED tests before source for modelnew events, stale suppression/paused/headless/fakeprovider,
backendvalidationbeforenative, partialfailureaggregate/reversecleanup/poisonedreservation,
wrongthread/noeffects/afterdispose/unknowncue andplayingstop/volume. Preserve all309tests,
old517level1route andsession/patrol/controlsemantics. FakeSoundPlatform proves partialfactory/
window+audio cleanup anderroraggregation inreport. Assetcheckpositive+isolatedcorruptionnegative.
Full309+newmanaged, threeexistingnativeharnesses/CLI7/threeoldassetverifiers, new independent
headlessaudio-device nativeprobe loads/plays/stopsfouroriginals/cleans/reopens and checksactual
soundvalid/state, no screenrequired. Ifhardwareunavailable recordlimitedproof, no fakePASS.
Game nativeprobe routes publicinputs withrecordingaudio provingeventassociation and noaltered
rendering/state; no productiondebugflags. Ownergate is onlyeventassociation/level/comfort.

## Readiness evidence and precise acceptance
The committed Assets/Audio/provenance.json (retrieved2026-10-22) records each source URL,
SHA-256, actual PCM metadata, duration, normalized peak and RMS. All four files are original
mono PCM16/44100Hz. NOTICE.md is authored provenance, not a fabricated embedded license.
The four original voices at gain0.5 have a measured-peak-sum bound below0.45; this is not proof
of perceived loudness, device distortion or aesthetic coherence. Those require listening.
Use the exact WAV/profile and typed-fallback/cleanup acceptance tables in ADR-0015. Program
primes feedback before the first session tick. Native proof checks successful decode/load,
device readiness and request/stop/close/reopen, not audibility or sample-exact DAC output.

Additional ownership regressions: foreign already-ready device is rejected without Init/Close;
wrong-thread SoundPlatform disposal has no effects and does not poison later owner cleanup.
Both were observed RED before correction. Actual native probe also preserves a foreign device
and then proves normal device close/reopen. These tighten ownership, not functional scope.
