using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Systems;

/// <summary>
/// System for camera movement, zoom, and reset.
/// Processes commands from CameraComponent (input-agnostic).
/// Stateless system - contains no state, only logic.
/// </summary>
public sealed class CameraControllerSystem : ISystem
{
    /// <summary>
    /// Updates all camera entities based on their command state.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(World world, float deltaTime)
    {
        var entities = world.GetEntitiesWith<CameraComponent>();

        foreach (var entity in entities)
        {
            ref var cam = ref world.GetComponent<CameraComponent>(entity);

            // Movement (input-agnostic: WASD, arrows, joystick, touch, etc.)
            if (cam.MoveDirection.Magnitude > 0.01f)
            {
                var normalizedDir = cam.MoveDirection.Normalized;
                var movement = normalizedDir * cam.MovementSpeed * deltaTime / cam.Camera.Zoom;
                cam.Camera.Position = new Vector2(
                    cam.Camera.Position.X + movement.X,
                    cam.Camera.Position.Y + movement.Y
                );
            }

            // Zoom (input-agnostic: Q/E, scroll, pinch, etc.)
            if (System.Math.Abs(cam.ZoomDelta) > 0.01f)
            {
                var newZoom = cam.Camera.Zoom + cam.ZoomDelta * cam.ZoomSpeed * deltaTime;
                cam.Camera.Zoom = System.Math.Clamp(newZoom, cam.MinZoom, cam.MaxZoom);
            }

            // Reset (input-agnostic: R key, button, gesture, etc.)
            if (cam.ResetRequested)
            {
                cam.Camera.Position = cam.DefaultPosition;
                cam.Camera.Zoom = 1f;
                cam.Camera.Rotation = 0f;
                cam.ResetRequested = false;
            }

            // Clear commands for next frame
            cam.MoveDirection = Vector2.Zero;
            cam.ZoomDelta = 0f;
        }
    }
}
