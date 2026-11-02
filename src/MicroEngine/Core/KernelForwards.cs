using System.Runtime.CompilerServices;

// The backend-free math and colour types moved to MicroEngine.Kernel.dll with their namespace unchanged, so source
// compiles as before; these forwards keep code compiled against an earlier MicroEngine.dll finding them.
[assembly: TypeForwardedTo(typeof(MicroEngine.Color))]
[assembly: TypeForwardedTo(typeof(MicroEngine.RectF))]
[assembly: TypeForwardedTo(typeof(MicroEngine.MathX))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Ease))]
