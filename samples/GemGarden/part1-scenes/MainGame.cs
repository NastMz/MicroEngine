using MicroEngine;
using MicroEngine.Core;
using GemGarden.Scenes;

namespace GemGarden;

/// <summary>Gem Garden: its settings and the first scene.</summary>
public sealed class MainGame : Game
{
    #region settings
    /// <summary>A 320x180 virtual screen, scaled to the window without blurring the pixels.</summary>
    public override GameSettings Settings { get; } = new("Gem Garden") { Background = Color.Hex("1a1c2c") };
    #endregion

    #region load
    protected override void Load()
    {
        Scenes.Push(new TitleScene(this));
    }
    #endregion
}
