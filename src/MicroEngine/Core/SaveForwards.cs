using System.Runtime.CompilerServices;

// The save types moved to MicroEngine.Saves.dll (carried by the MicroEngine package) with their namespace, so saves are a
// module of their own; code compiled against an earlier MicroEngine.dll still finds them here.
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.SaveStore))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.ISaveStorage))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.FileSaveStorage))]
