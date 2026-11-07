using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A list longer than the screen: thirty levels in a scrolling view. Up and down move the focus and the view follows it;
/// the wheel and a drag (a finger on a phone) scroll it; a click or confirm picks a level.
/// </summary>
public sealed class LongListExample : Scene
{
    /// <summary>The level picked last, or null.</summary>
    public string? Picked { get; private set; }

    /// <summary>The scrolling view, for tests.</summary>
    public ScrollView List { get; private set; } = null!;

    #region list
    public override void Enter()
    {
        var levels = new Stack();
        for (int i = 1; i <= 30; i++)
        {
            string name = $"LEVEL {i:00}";
            levels.Add(new Button(name, () => Picked = name) { Width = 120 });
        }
        // A fixed height: the list is longer, so the view scrolls and cuts it at its edges.
        List = new ScrollView(levels) { Height = 110 };
        Widgets.Content = new Panel(List);
        Widgets.Cancelled += () => Game.Scenes.Pop();
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        Heading.Draw(r, Font, "LONG LIST");
        Font.Draw(r, Picked is null ? "PICK A LEVEL" : $"PICKED {Picked}", new Vector2(8, 168), UiTheme.Default.Muted);
    }
}
