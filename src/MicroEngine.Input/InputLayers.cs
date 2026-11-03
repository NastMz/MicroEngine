namespace MicroEngine.Input;

/// <summary>
/// One stage of input routing: user interface, on-screen controls, a debug console, a rebinding screen. Events reach
/// the layers from the highest order down (<see cref="InputLayers"/>); a layer that returns true takes the event, and
/// nothing below it (other layers, the actions of gameplay) sees it.
/// </summary>
/// <remarks>
/// A layer that takes a press also gets everything that press starts: the same pointer's moves until it lifts, the
/// same key's or button's release. So a finger that went down on an on-screen button stays the button's even when it
/// slides away, and a key that opened a menu does not reach gameplay when it is let go.
/// </remarks>
public interface IInputLayer
{
    /// <summary>Handles one event; returns true to take it from every layer and from gameplay below.</summary>
    bool Handle(in InputEvent e);
}

/// <summary>The orders of the engine's own layers. Higher orders see events first; gameplay is below every layer.</summary>
public static class InputLayerOrder
{
    /// <summary>User interface: menus, dialogs, text fields (300).</summary>
    public const int UI = 300;
    /// <summary>The on-screen touch controls (200).</summary>
    public const int TouchControls = 200;
    /// <summary>Game layers that sit right above gameplay (100).</summary>
    public const int Game = 100;
}

/// <summary>
/// The input layers in routing order: highest <c>order</c> first, layers of equal order in the order they were added.
/// Events no layer takes become the polled state actions read (<see cref="InputDevices"/>) and the frame's
/// <see cref="InputSystem.Events"/>.
/// </summary>
public sealed class InputLayers
{
    private readonly List<(IInputLayer Layer, int Order, long Added)> _layers = [];
    private long _added;
    private IInputLayer[] _snapshot = [];

    /// <summary>The layers, in the order events reach them.</summary>
    public IReadOnlyList<IInputLayer> InOrder => _snapshot;

    /// <summary>The number of layers.</summary>
    public int Count => _layers.Count;

    /// <summary>Adds <paramref name="layer"/> at <paramref name="order"/> (see <see cref="InputLayerOrder"/>); adding it again moves it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="layer"/> is null.</exception>
    public void Add(IInputLayer layer, int order = InputLayerOrder.Game)
    {
        ArgumentNullException.ThrowIfNull(layer);
        Remove(layer);
        _layers.Add((layer, order, ++_added));
        _layers.Sort(static (a, b) => a.Order != b.Order ? b.Order.CompareTo(a.Order) : a.Added.CompareTo(b.Added));
        _snapshot = [.. _layers.Select(l => l.Layer)];
    }

    /// <summary>Removes <paramref name="layer"/>; false when it was not added.</summary>
    public bool Remove(IInputLayer layer)
    {
        if (_layers.RemoveAll(l => ReferenceEquals(l.Layer, layer)) == 0) return false;
        _snapshot = [.. _layers.Select(l => l.Layer)];
        return true;
    }

    /// <summary>Whether <paramref name="layer"/> was added.</summary>
    public bool Contains(IInputLayer layer) => _layers.Exists(l => ReferenceEquals(l.Layer, layer));

    internal IInputLayer? Offer(in InputEvent e)
    {
        // The snapshot is replaced, never changed, so a layer may add or remove layers while handling an event.
        foreach (var layer in _snapshot)
            if (layer.Handle(e)) return layer;
        return null;
    }
}
