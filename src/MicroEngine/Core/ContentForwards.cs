using System.Runtime.CompilerServices;

// AssetLoadException moved to MicroEngine.Content.dll (the asset store) with its namespace unchanged, so source compiles
// as before; the forward keeps code compiled against an earlier MicroEngine.dll finding it.
[assembly: TypeForwardedTo(typeof(MicroEngine.Core.AssetLoadException))]
