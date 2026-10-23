# MR-6 — Four one-shot audio cues
Baselineaa385cb; feat/audio-feedback. Local work units; cached stacked-to-main convention, no PR/push/main for newaudio.
Forecast~700 authored lines split contracts/provider and consumer events; heuristic advisory. Serial managed/native checks.

## Specs
S1. "Funciona perfectamente, continua" accepts MR5sessionflow, not futureaudio. Recordpositivefeedback/integrateevaluatedscope only.
S2. Priorowner: "Sonidos y música." / "Continúa autónomamente con el roadmap de maduración, incorporando progresivamente enemigos, feedback visual, audio, menús y otras necesidades justificadas." NextboundedfourSFX, no music/mixer/streaming.
S3. Priorowner: "Antes de implementar cada incremento, inventaría los recursos que necesita y compáralos con los ya existentes." / "Prioriza recursos CC0 o con permisos claros y compatibles con nuestro uso." Selectactualfiles/formats/provenance before consumerconstruction, maintain contextmix proof.
S4. Priorowner: "El núcleo del motor debe permanecer independiente de implementaciones concretas de renderizado, física, audio, entrada y plataforma." GenericoptionalindependentAudio contract; RaylibonlyDesktop, no genrekeys inengine andheadlessabsencevalid.
S5. Priorowner: "No crees sistemas genéricos gigantescos de forma anticipada, pero tampoco obligues al juego a reconstruir infraestructura técnica que debería proporcionar MicroEngine." Cohesive one-shotprovider with ownership/failuretests; events/mapping/pausepolicygameowned. No newFrameLoop/lifecycle/UI API.
S6. Priorowner: "No solicites mi revisión para cambios puramente técnicos ya verificables." Finalmanualgate only perceivedsoundassociation/levels/comfort, automatedstate/cleanup/hash/nativechecksagentowned.

## Tasks
- T1 / S1-S6 / inline + map/design review / in progress / ME083 assets/ADR/contract.
- T2 / S4-S5 / inline / pending / ME084 optionalAudio/replaceableDesktop provider plusfakefailuretests.
- T3 / S2-S6 / inline / pending / ME085 transientgameevents/fourcueintegration/headlessandnativeproof.
- T4 / S1-S6 / independent final verification / pending / ME086 fullregressions/resources/delivery.

## Log
L1. Owner2026-10-22: "Funciona perfectamente, continua" afterMR5. Priorroadmap explicitlyauthorizesincrementalaudio andsameassetreadiness, no speculativeallfeatures. Rootaa385cb recordsMR5acceptance andnormalmainFF6204b48..aa385cb, unrelated.gitignorehashpreserved. Newbranchfeat/audio-feedback; no remote, RDDoff; memorymirrorpendingruntimeidentityunavailable. Mappingbudgetdelegated audio_scope_map, parentinline writer(noWritertrigger).
L2. OfficialOGA MoxieCat8-bitPlatformerSFXCC0 individualjump/button/playerhurt/springWAVs downloadedpubliclytotemp; eachmonoPCM16/44100Hz,0.287..0.501s, files25530/25372/44190/29498bytes. No existingaudioasset/backend. PrimaryRaylib-csv8.1.0/nativeimplementationresearchcorrectedC7claim: LoadSoundFromWave copies/converts, inputWave mustUnloadfinally. GenericproviderMUSTNOTallowlistgameeventnames, unlikeinitialagentrecommendation. Arbitraryboundedcuekeys/otherproviderssupported; no nativehandlesoutsideDesktop.

L3. Independent preimplementation review required individualprovenance, exactfallback/deviceerror distinction, lifecycletrace andWAVprofile. Added sourceoriginals withperfileSHA/accessdate/PCMdurationpeakRMS; exactRIFF/chunk/profiletable; unavailableonlyInitreturnsnotReadyaftercleanclose, stderrwarning once, no fallback forothererrors. SoundPlatform owns bothresources/exactcleanuptrace; no soundoutsidewrapper. Feedbackprimedbeforefirsttick. Targetedreview beforeapproval.

L4. Independent targetedPASS allfourcorrections; rootapprovesADR0015/MR6beforecode. ME083complete/ME084claimed. Actualquietsourcepeaks justifygain0.5 withmeasuredfourpeakssum<0.45; no universalclippingclaim. No native/publish/memoryauthority changes.

L5. ME084 runnableDesktopaudioRED6againststubs ->46GREEN, additionalchunkorder/padding/duplicate/durationboundary/volume-failure regressions ->49GREEN. GenericAudioindependentofLoop/vendor, Desktopownednativewavesfinallyunloaded/partialcleanup/reverseclose/reservation/thread/errors tested. ME085 gameeventsRED4->33PlatformerGREEN inworktree; providerworkunitcommittedseparately forreviewability. No nativeaudioproofclaimed yet.
