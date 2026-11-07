using System.Runtime.CompilerServices;

// The widgets moved to MicroEngine.UI.dll (carried by the MicroEngine package) with their MicroEngine.UI namespace, so
// the UI is a module of its own; code compiled against an earlier MicroEngine.dll still finds them here. DialogueBox,
// PromptText and the achievement views stay in this assembly: they read the action prompts or the achievement tracker.
#pragma warning disable CS0618 // deprecated types are forwarded until they are removed
[assembly: TypeForwardedTo(typeof(MicroEngine.UI.Typewriter))]
[assembly: TypeForwardedTo(typeof(MicroEngine.UI.UiTheme))]
[assembly: TypeForwardedTo(typeof(MicroEngine.UI.SelectionList))]
#pragma warning restore CS0618
