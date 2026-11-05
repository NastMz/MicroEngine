using MicroEngine.Core;
using MicroEngine.Kernel;

namespace MicroEngine.Saves;

/// <summary>
/// Saves as an engine module: registers <see cref="Store"/> as the engine's <see cref="SaveStore"/> service, so other
/// modules (achievements) and the game's systems find it in the services. It schedules no frame system and writes
/// nothing by itself: a slot is written when someone saves it. A composition that never adds the module has no store
/// service, and a game that never creates a store writes no file.
/// </summary>
/// <example>
/// <code>
/// var store = new SaveStore("com.example.mygame");
/// builder.Add(new SavesModule(store));
/// // later, anywhere with the services:
/// services.Get&lt;SaveStore&gt;().Save("progress", progress);
/// </code>
/// </example>
public sealed class SavesModule : IEngineModule
{
    /// <summary>Creates the module over <paramref name="store"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> is null.</exception>
    public SavesModule(SaveStore store) => Store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>The store the module registers.</summary>
    public SaveStore Store { get; }

    /// <summary>Registers <see cref="Store"/> as the <see cref="SaveStore"/> service.</summary>
    public void Configure(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Add(Store);
    }
}
