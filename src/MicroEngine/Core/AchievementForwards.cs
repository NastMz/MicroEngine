using System.Runtime.CompilerServices;

// The achievement catalog and tracker moved to MicroEngine.Achievements.dll (carried by the MicroEngine package) with their
// namespace, so achievements are a module of their own; code compiled against an earlier MicroEngine.dll still finds
// them here.
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.Achievement))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.AchievementStatus))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.AchievementSaveData))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.AchievementRecord))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.AchievementTracker))]
