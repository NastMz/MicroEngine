using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Systems;

/// <summary>
/// System for entity dragging with optional kinematic switching.
/// Processes commands from DraggableComponent (input-agnostic).
/// Stateless system - contains no state, only logic.
/// </summary>
public sealed class DragSystem : ISystem
{
    /// <summary>
    /// Updates all draggable entities based on their command state.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(World world, float deltaTime)
    {
        var entities = world.GetEntitiesWith<DraggableComponent>();

        foreach (var entity in entities)
        {
            if (!world.HasComponent<TransformComponent>(entity))
            {
                continue; // Skip entities without transform
            }

            ref var draggable = ref world.GetComponent<DraggableComponent>(entity);
            ref var transform = ref world.GetComponent<TransformComponent>(entity);

            // Start drag (input-agnostic)
            if (draggable.StartDragRequested && !draggable.IsDragging)
            {
                draggable.IsDragging = true;
                draggable.DragOffset = new Vector2(
                    draggable.DragPosition.X - transform.Position.X,
                    draggable.DragPosition.Y - transform.Position.Y
                );

                // Make kinematic if requested and entity has RigidBodyComponent
                if (draggable.MakeKinematicOnDrag && world.HasComponent<RigidBodyComponent>(entity))
                {
                    ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
                    rigidBody.IsKinematic = true;
                    rigidBody.Velocity = Vector2.Zero;
                }

                draggable.StartDragRequested = false;
            }

            // Update drag position
            if (draggable.IsDragging)
            {
                transform.Position = new Vector2(
                    draggable.DragPosition.X - draggable.DragOffset.X,
                    draggable.DragPosition.Y - draggable.DragOffset.Y
                );
            }

            // Stop drag (input-agnostic)
            if (draggable.StopDragRequested && draggable.IsDragging)
            {
                draggable.IsDragging = false;

                // Restore dynamic physics if kinematic was set
                if (draggable.MakeKinematicOnDrag && world.HasComponent<RigidBodyComponent>(entity))
                {
                    ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
                    rigidBody.IsKinematic = false;
                    rigidBody.Velocity = Vector2.Zero;
                }

                draggable.StopDragRequested = false;
            }
        }
    }
}
