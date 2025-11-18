namespace MicroEngine.Core.Physics;

/// <summary>
/// Manages collision rules between physics layers.
/// Uses a 32x32 bit matrix to determine which layers can collide with each other.
/// </summary>
public sealed class CollisionMatrix
{
    private const int LAYER_COUNT = CollisionLayer.TOTAL_LAYER_COUNT;
    private const uint ALL_LAYERS_ENABLED_MASK = uint.MaxValue;

    private readonly uint[] _matrix = new uint[LAYER_COUNT];

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionMatrix"/> class.
    /// By default, all layers can collide with each other.
    /// </summary>
    public CollisionMatrix()
    {
        // Initialize with all collisions enabled (all bits set)
        for (int i = 0; i < LAYER_COUNT; i++)
        {
            _matrix[i] = ALL_LAYERS_ENABLED_MASK;
        }
    }

    /// <summary>
    /// Sets whether two layers can collide.
    /// </summary>
    /// <param name="layer1">First layer.</param>
    /// <param name="layer2">Second layer.</param>
    /// <param name="canCollide">True to enable collision, false to disable.</param>
    public void SetCollision(CollisionLayer layer1, CollisionLayer layer2, bool canCollide)
    {
        SetCollisionInternal(layer1.Id, layer2.Id, canCollide);
    }

    /// <summary>
    /// Checks if two layers can collide.
    /// </summary>
    /// <param name="layer1">First layer.</param>
    /// <param name="layer2">Second layer.</param>
    /// <returns>True if layers can collide, false otherwise.</returns>
    public bool CanCollide(CollisionLayer layer1, CollisionLayer layer2)
    {
        return CanCollideInternal(layer1.Id, layer2.Id);
    }

    /// <summary>
    /// Checks if two layer masks can collide.
    /// </summary>
    /// <param name="mask1">First layer mask.</param>
    /// <param name="mask2">Second layer mask.</param>
    /// <returns>True if any layers in the masks can collide, false otherwise.</returns>
    public bool CanCollide(int mask1, int mask2)
    {
        // Check if any bit in mask1 can collide with any bit in mask2
        for (int i = 0; i < LAYER_COUNT; i++)
        {
            if ((mask1 & (1 << i)) != 0)
            {
                // Layer i is in mask1, check against all layers in mask2
                uint layerMatrix = _matrix[i];
                if ((layerMatrix & mask2) != 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Ignores collision between two layer IDs.
    /// </summary>
    /// <param name="layer1Id">First layer ID (0-31).</param>
    /// <param name="layer2Id">Second layer ID (0-31).</param>
    public void IgnoreLayerCollision(int layer1Id, int layer2Id)
    {
        SetCollisionInternal(layer1Id, layer2Id, false);
    }

    /// <summary>
    /// Enables collision between two layer IDs.
    /// </summary>
    /// <param name="layer1Id">First layer ID (0-31).</param>
    /// <param name="layer2Id">Second layer ID (0-31).</param>
    public void EnableLayerCollision(int layer1Id, int layer2Id)
    {
        SetCollisionInternal(layer1Id, layer2Id, true);
    }

    /// <summary>
    /// Resets the collision matrix to default state (all collisions enabled).
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < LAYER_COUNT; i++)
        {
            _matrix[i] = ALL_LAYERS_ENABLED_MASK;
        }
    }

    private void SetCollisionInternal(int layer1Id, int layer2Id, bool canCollide)
    {
        uint mask = (uint)(1 << layer2Id);

        if (canCollide)
        {
            // Enable collision (set bit)
            _matrix[layer1Id] |= mask;
            _matrix[layer2Id] |= (uint)(1 << layer1Id);
        }
        else
        {
            // Disable collision (clear bit)
            _matrix[layer1Id] &= ~mask;
            _matrix[layer2Id] &= ~(uint)(1 << layer1Id);
        }
    }

    private bool CanCollideInternal(int layer1Id, int layer2Id)
    {
        uint mask = (uint)(1 << layer2Id);
        return (_matrix[layer1Id] & mask) != 0;
    }
}
