using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A name entry: a text field that greets on Enter, and buttons that copy the whole field and paste over it through
/// the clipboard, for players without a keyboard's Ctrl+C and Ctrl+V. In the field itself, Shift with the arrows or a
/// drag of the pointer selects, and Ctrl (Command on macOS) with A, C, X and V selects all, copies, cuts and pastes.
/// </summary>
public sealed class TextEntryExample : Scene
{
    /// <summary>The name field, for tests.</summary>
    public TextField Field { get; private set; } = null!;
    /// <summary>The line under the buttons, for tests.</summary>
    public Label Status { get; private set; } = null!;
    /// <summary>The buttons, for tests.</summary>
    public Button CopyButton { get; private set; } = null!;
    /// <summary>The paste button, for tests.</summary>
    public Button PasteButton { get; private set; } = null!;

    #region field
    public override void Enter()
    {
        Status = new Label("TYPE A NAME, THEN ENTER") { Color = UiTheme.Default.Muted };
        Field = new TextField { Placeholder = "YOUR NAME", MaxLength = 16, Columns = 120 };
        Field.Submitted += name => Status.Text = name.Length > 0 ? $"HELLO, {name.ToUpperInvariant()}" : "THE NAME IS EMPTY";
        CopyButton = new Button("COPY", Copy);
        PasteButton = new Button("PASTE", Paste);
        Widgets.Content = new Stack { Spacing = 8 }
            .Add(Field)
            .Add(new Stack { Orientation = Orientation.Horizontal, Spacing = 8 }.Add(CopyButton).Add(PasteButton))
            .Add(Status);
        Widgets.Focus(Field);
    }

    // The buttons act on the whole field: select it all, then copy, or paste over it.
    private void Copy()
    {
        Field.SelectAll();
        Status.Text = Field.Copy() ? "COPIED" : "NOTHING TO COPY";
    }

    private void Paste()
    {
        Field.SelectAll();
        Status.Text = Field.Paste() ? "PASTED" : "NOTHING TO PASTE";
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop(); // Esc leaves the field first, then the example
    }

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "TEXT ENTRY");
}
