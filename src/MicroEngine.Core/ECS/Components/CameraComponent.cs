using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Component for camera control data.
/// Commands are set by the scene's input layer and consumed by CameraControllerSystem.
/// Pure data component - contains no logic.
/// </summary>
public struct CameraComponent : IComponent
{
    /// <summary>Gets or sets the Camera2D instance.</summary>
    public Camera2D Camera { get; set; }

    /// <summary>Gets or sets the movement speed in units per second.</summary>
    public float MovementSpeed { get; set; }

    /// <summary>Gets or sets the zoom speed in units per second.</summary>
    public float ZoomSpeed { get; set; }

    /// <summary>Gets or sets the minimum zoom level.</summary>
    public float MinZoom { get; set; }

    /// <summary>Gets or sets the maximum zoom level.</summary>
    public float MaxZoom { get; set; }

    /// <summary>Gets or sets the default position for camera reset.</summary>
    public Vector2 DefaultPosition { get; set; }

    // --- Commands (set by scene, consumed by system) ---

    /// <summary>
    /// Gets or sets the movement direction (normalized -1 to 1).
    /// Input-agnostic: can be from WASD, arrows, joystick, touch swipe, etc.
    /// </summary>
    public Vector2 MoveDirection { get; set; }

    /// <summary>
    /// Gets or sets the zoom delta (-1, 0, or 1).
    /// Input-agnostic: can be from Q/E keys, mouse wheel, pinch gesture, etc.
    /// </summary>
    public float ZoomDelta { get; set; }

    /// <summary>
    /// Gets or sets whether a camera reset was requested.
    /// Input-agnostic: can be from R key, button press, gesture, etc.
    /// </summary>
    public bool ResetRequested { get; set; }
}
