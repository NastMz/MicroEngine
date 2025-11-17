namespace MicroEngine.Core.ECS;

/// <summary>
/// Interface for systems that process entities with specific component configurations.
/// Systems contain the logic that operates on component data.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Updates the system logic for one frame.
    /// </summary>
    /// <param name="world">The world containing entities and components.</param>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    void Update(World world, float deltaTime);
}
