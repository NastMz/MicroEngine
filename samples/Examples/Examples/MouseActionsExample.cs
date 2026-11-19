using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// The mouse through actions, as a strategy or point-and-click game uses it: a click orders the scout to a place, the
/// wheel zooms, holding another button drags the view, and the button that gives the order is chosen by pressing it.
/// A click on the widget is the widget's, never an order.
/// </summary>
public sealed class MouseActionsExample : Scene
{
    private static readonly RectF Field = new(0, 0, 640, 360);
    private static readonly string[] Actions = ["order", "grab", "zoom-in", "zoom-out"];
    private readonly Camera2D _camera = new(320, 180);
    private Button _button = null!;
    private IDisposable? _listening;
    private Vector2 _scout = new(320, 180), _target = new(320, 180);

    /// <summary>Where the scout is, in the field.</summary>
    public Vector2 Scout => _scout;
    /// <summary>Where the scout was told to go.</summary>
    public Vector2 Target => _target;
    /// <summary>The view.</summary>
    public Camera2D Camera => _camera;
    /// <summary>Whether the example waits for the control that will give the order.</summary>
    public bool Listening => _listening is not null;

    #region bind
    /// <summary>Mouse buttons and the wheel are bound like keys, next to the keys that do the same.</summary>
    public static void BindDefaults(InputMap map) => map
        .Unbind("order").Bind("order", PointerButton.Primary)             // a click; on a touch screen, a tap
        .Unbind("grab").Bind("grab", PointerButton.Secondary)             // held to drag the view
        .Unbind("zoom-in").Bind("zoom-in", InputControl.WheelUp, Key.Equals)
        .Unbind("zoom-out").Bind("zoom-out", InputControl.WheelDown, Key.Minus);
    #endregion

    public override void Enter()
    {
        BindDefaults(Input);
        _camera.CenterOn(_scout);
        Widgets.Anchor = Anchor.TopLeft;
        Widgets.Content = _button = new Button("", Listen) { Offset = new Vector2(8, 28) };
    }

    public override void Exit()
    {
        _listening?.Dispose();
        foreach (var action in Actions) Input.Unbind(action);
        if (Game.TryGetCapability<ICursorControl>(out var cursor)) cursor.SetRelative(false);
    }

    public override void Update(float dt)
    {
        if (_listening is null && Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

        #region read
        // A click on the button above never gets here: the widgets took it before the actions were read.
        if (Input.Pressed("order")) _target = Vector2.Clamp(_camera.ScreenToWorld(Input.Pointer.Position), Field.Position, Field.Position + Field.Size);

        // The wheel presses its action once per notch, like a key tapped once: the same line serves the wheel and the keys.
        if (Input.Pressed("zoom-in")) _camera.Zoom = Math.Min(3f, _camera.Zoom * 1.25f);
        if (Input.Pressed("zoom-out")) _camera.Zoom = Math.Max(0.5f, _camera.Zoom / 1.25f);

        // Dragging the view: how far the pointer moved in this update, in canvas pixels.
        if (Input.Down("grab")) _camera.Position -= Input.Pointer.Delta / _camera.Zoom;
        #endregion

        #region cursor
        // Where the platform has a cursor to control, lock it while dragging: it stays where the drag began and the
        // mouse never runs into the edge of the screen. Without one (a phone), the drag works all the same.
        if (Game.TryGetCapability<ICursorControl>(out var cursor))
        {
            if (Input.Pressed("grab")) cursor.SetRelative(true);
            if (Input.Released("grab")) cursor.SetRelative(false);
        }
        #endregion

        var to = _target - _scout;
        _scout = to.Length() <= 90 * dt ? _target : _scout + Vector2.Normalize(to) * 90 * dt;
        _button.Text = _listening is not null
            ? "CLICK A BUTTON OR TURN THE WHEEL (ANY KEY: KEEP)"
            : "ORDER: " + string.Join(" / ", Input.GetBindings("order")!.Controls.Select(c => PointerNames.Of(c) ?? c.Key.ToString()));
    }

    #region rebind
    /// <summary>Waits for the next mouse button or wheel turn, wherever the pointer is, and gives it the order.</summary>
    public void Listen()
    {
        if (_listening is not null) return;
        // The press goes to the listener alone: it is not an order, and it does not click the button again.
        _listening = Game.InputSystem.ListenForPress(press =>
        {
            _listening = null;
            if (press.Kind != InputEventKind.KeyDown) Assign(Input, "order", press);   // a key keeps the binding
        }, PressKinds.All);
    }

    /// <summary>
    /// Gives <paramref name="action"/> the mouse control of <paramref name="press"/> in place of the ones it had. An
    /// action that used that control gets the ones given up, so no button does two things.
    /// </summary>
    public static void Assign(InputMap map, string action, in InputEvent press)
    {
        if (InputControl.FromPress(press) is not { Device: InputDevice.Mouse } control) return;
        var given = map.GetBindings(action)!.Controls.Where(c => c.Device == InputDevice.Mouse).ToArray();
        if (given.Contains(control)) return;
        foreach (var other in Actions.Where(a => a != action))
            if (map.Unbind(other, control)) map.Bind(other, given);
        map.BindFrom(action, press, replace: true);
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        r.PushCamera(_camera);
        for (int y = 0; y <= Field.Height; y += 40)
        for (int x = 0; x <= Field.Width; x += 40)
            r.FillRect(new RectF(x - 1, y - 1, 2, 2), Color.Hex("566c86"));
        r.DrawRect(Field, Color.Hex("ef7d57"), 2);
        r.DrawLine(_scout, _target, 1, Color.Hex("ffcd75").WithAlpha(0.5f));
        r.FillCircle(_target, 3, Color.Hex("ffcd75"));
        r.FillCircle(_scout, 6, Color.Hex("41a6f6"));
        r.PopCamera();

        Font.Draw(r, "CLICK: ORDER   WHEEL: ZOOM   RIGHT BUTTON: DRAG THE VIEW", new Vector2(4, 158), UiTheme.Default.Muted);
        Font.Draw(r, FormattableString.Invariant($"ZOOM {_camera.Zoom:0.00}   LAST DEVICE: {Input.LastDevice}"), new Vector2(4, 168), UiTheme.Default.Text);
        Heading.Draw(r, Font, "MOUSE ACTIONS");
    }
}
