using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

/// <summary>
/// Renders sprites for Zelda demo entities with depth sorting.
/// </summary>
public class RenderSystem : ISystem
{
    private readonly IRenderer2D _renderer;
    private CachedQuery? _renderQuery;
    private readonly List<Entity> _sortedEntities = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderSystem"/> class.
    /// </summary>
    /// <param name="renderer">Renderer responsible for drawing sprites.</param>
    public RenderSystem(IRenderer2D renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Updates sprite rendering order and draws visible entities.
    /// </summary>
    public void Update(World world, float deltaTime)
    {
        _renderQuery ??= world.CreateCachedQuery(typeof(TransformComponent), typeof(SpriteComponent));

        _sortedEntities.Clear();
        foreach (var entity in _renderQuery.Entities)
        {
            if (world.IsEntityValid(entity))
            {
                _sortedEntities.Add(entity);
            }
        }

        // Sort by Layer, then by Y position for depth sorting
        _sortedEntities.Sort((a, b) =>
        {
            if (!world.IsEntityValid(a) || !world.IsEntityValid(b))
            {
                return 0;
            }
            
            var sA = world.GetComponent<SpriteComponent>(a);
            var sB = world.GetComponent<SpriteComponent>(b);
            if (sA.Layer != sB.Layer)
            {
                return sA.Layer.CompareTo(sB.Layer);
            }
            
            var tA = world.GetComponent<TransformComponent>(a);
            var tB = world.GetComponent<TransformComponent>(b);
            return tA.Position.Y.CompareTo(tB.Position.Y);
        });

        foreach (var entity in _sortedEntities)
        {
            if (!world.IsEntityValid(entity))
            {
                continue;
            }
            var transform = world.GetComponent<TransformComponent>(entity);
            var sprite = world.GetComponent<SpriteComponent>(entity);

            if (!sprite.Visible || sprite.Texture == null)
            {
                continue;
            }

            if (sprite.Region.HasValue)
            {
                var region = sprite.Region.Value;
                
                // TexturePro handles source rectangle and destination rectangle
                var sourceRect = region.Source;
                
                // Handle flips by negating source rect dimensions
                float srcW = sprite.FlipX ? -sourceRect.Width : sourceRect.Width;
                float srcH = sprite.FlipY ? -sourceRect.Height : sourceRect.Height;
                var finalSource = new Rectangle(sourceRect.X, sourceRect.Y, srcW, srcH);

                var destRect = new Rectangle(
                    transform.Position.X, 
                    transform.Position.Y, 
                    sourceRect.Width * transform.Scale.X, 
                    sourceRect.Height * transform.Scale.Y
                );

                _renderer.DrawTexturePro(
                    sprite.Texture,
                    finalSource,
                    destRect,
                    transform.Origin,
                    transform.Rotation * (180f / MathF.PI), // Deg
                    sprite.Tint
                );
            }
            else
            {
                _renderer.DrawTexture(sprite.Texture, transform.Position, sprite.Tint);
            }
        }
    }
}
