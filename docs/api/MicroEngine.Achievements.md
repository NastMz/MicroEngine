# Namespace MicroEngine.Achievements

<div data-pagefind-weight="0.3">

Achievements as an optional engine module: the module that registers the tracker as a service of the engine and writes its progress.

Guide: [Achievements](../manual/achievements.md)  

### Classes

 [AchievementsModule](MicroEngine.Achievements.AchievementsModule.md)

Achievements as an engine module: registers [Tracker](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_Tracker) as the engine's [AchievementTracker](MicroEngine.Core.AchievementTracker.md)
service and schedules [SystemName](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_SystemName) in [PostFixed](MicroEngine.Kernel.FramePhase.md), which writes pending progress
about once a second of simulated time ([FlushInterval](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_FlushInterval); unlocks are written at once by the tracker) and
raises [Stepped](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_Stepped) every step; progress is written too when the platform suspends the game. A composition
that never adds the module has no tracker, no system and no achievement file. The <code>Game</code> façade adds it when the game
declares achievements (<code>TrackAchievements</code> before the engine is composed).

</div>
