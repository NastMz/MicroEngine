using System.Runtime.CompilerServices;

// The widgets moved to MicroEngine.UI.dll (carried by the MicroEngine package) with their MicroEngine.UI namespace, so
// the UI is a module of its own; code compiled against an earlier MicroEngine.dll still finds them here. PromptText
// stays in this assembly: it reads the action prompts.
[assembly: TypeForwardedTo(typeof(MicroEngine.UI.UiTheme))]
