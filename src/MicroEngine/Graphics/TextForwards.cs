using System.Runtime.CompilerServices;

// The fonts moved to MicroEngine.Text.dll (carried by the MicroEngine package) with their MicroEngine.Graphics namespace,
// so text is a module of its own; code compiled against an earlier MicroEngine.dll still finds them here.
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.IFont))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.FontExtensions))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.BitmapFont))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.ScalableFont))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.SizedFont))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Graphics.FontOptions))]
