namespace MicroEngine.Core.Resources;

/// <summary>
/// Font resource for text rendering.
/// </summary>
public interface IFont : IResource
{
    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets the font family name.
    /// </summary>
    string Family { get; }
}
