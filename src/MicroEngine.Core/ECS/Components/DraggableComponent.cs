using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Component for draggable entity data.
/// Commands are set by the scene's input layer and consumed by DragSystem.
/// Pure data component - contains no logic.
/// </summary>
public struct DraggableComponent : IComponent
{
    /// <summary>Gets or sets whether the entity is currently being dragged.</summary>
    public bool IsDragging { get; set; }

    /// <summary>Gets or sets the offset from drag position to entity center.</summary>
    public Vector2 DragOffset { get; set; }

    /// <summary>
    /// Gets or sets whether to make the entity kinematic during drag.
    /// Useful for physics bodies that should ignore forces while being dragged.
    /// </summary>
    public bool MakeKinematicOnDrag { get; set; }

    // --- Commands (set by scene, consumed by system) ---

    /// <summary>
    /// Gets or sets whether a drag start was requested.
    /// Input-agnostic: can be from mouse click, touch tap, gamepad button, etc.
    /// </summary>
    public bool StartDragRequested { get; set; }

    /// <summary>
    /// Gets or sets the world position of the drag cursor.
    /// Input-agnostic: can be from mouse, touch, gamepad cursor, stylus, etc.
    /// </summary>
    public Vector2 DragPosition { get; set; }

    /// <summary>
    /// Gets or sets whether a drag stop was requested.
    /// Input-agnostic: can be from mouse release, touch lift, button release, etc.
    /// </summary>
    public bool StopDragRequested { get; set; }
}
