using MicroEngine.Loop;
namespace Platformer.Model;

/// <summary>Authored Meadow Run moving hazard data, not a reusable engine AI contract.</summary>
public sealed record PatrolContent(Rect Spawn, float MinX, float MaxX, float Speed);
