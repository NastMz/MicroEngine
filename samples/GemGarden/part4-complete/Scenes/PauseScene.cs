using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace GemGarden.Scenes;

/// <summary>An overlay over the frozen round: resume it or go back to the title.</summary>
public sealed class PauseScene(MainGame game) : Scene
{
    /// <summary>The play scene is drawn first, below this one, and stops updating while this one is on top.</summary>
    public override bool IsOverlay => true;

    public override void Enter()
    {
        Audio.PauseMusic(true);
        Widgets.Content = new Panel(new Stack { Spacing = 4 }
            .Add(new Label("PAUSED") { Color = UiTheme.Default.Accent })
            .Add(new Button("RESUME", () => Game.Scenes.Pop()) { Width = 100 })
            .Add(new Button("QUIT TO TITLE", () => Game.Scenes.Reset(new TitleScene(game), Transition.Fade())) { Width = 100 }))
        { Padding = 8 };
        Widgets.FocusChanged += change =>
        {
            if (change.From is not null) Audio.Play("select");
        };
    }

    public override void Exit() => Audio.PauseMusic(false);

    public override void Update(float dt)
    {
        if (Input.Pressed("pause") || Input.Pressed("cancel")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r) => r.FillRect(new RectF(0, 0, Game.Width, Game.Height), Color.Black.WithAlpha(0.5f));
}
