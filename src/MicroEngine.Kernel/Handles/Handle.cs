using System.Diagnostics.CodeAnalysis;

namespace MicroEngine.Kernel;

/// <summary>
/// A reference to an item of a <see cref="HandlePool{T}"/>: a slot index and the generation of the item that held the
/// slot when the handle was made. When the item is removed its slot's generation changes, so an old handle no longer
/// finds anything (instead of finding whatever reuses the slot). <c>default</c> is <see cref="None"/>.
/// </summary>
/// <typeparam name="T">The kind of item, so handles of different pools cannot be mixed up.</typeparam>
/// <param name="Index">The slot.</param>
/// <param name="Generation">The generation of the slot's item; 0 only for <see cref="None"/>.</param>
public readonly record struct Handle<T>(int Index, uint Generation)
{
    /// <summary>The handle of nothing.</summary>
    public static Handle<T> None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
    /// <inheritdoc/>
    public override string ToString() => IsNone ? $"{typeof(T).Name}:none" : $"{typeof(T).Name}:{Index}#{Generation}";
}

/// <summary>
/// Items reached through generational <see cref="Handle{T}"/>s: slots are reused, and a handle to a removed item is
/// detected (stale) instead of reaching the item that took its slot.
/// </summary>
/// <typeparam name="T">The items.</typeparam>
/// <remarks>Not thread-safe.</remarks>
public sealed class HandlePool<T>
{
    private T[] _items = [];
    private uint[] _generations = [];
    private bool[] _live = [];
    private readonly Stack<int> _free = new();
    private int _used;

    /// <summary>The number of live items.</summary>
    public int Count { get; private set; }

    /// <summary>Stores <paramref name="item"/> and returns its handle; a free slot is reused with a new generation.</summary>
    public Handle<T> Add(T item)
    {
        int index;
        if (_free.Count > 0) index = _free.Pop();
        else
        {
            if (_used == _items.Length)
            {
                int size = Math.Max(4, _items.Length * 2);
                Array.Resize(ref _items, size);
                Array.Resize(ref _generations, size);
                Array.Resize(ref _live, size);
            }
            index = _used++;
            _generations[index] = 1;
        }
        _items[index] = item;
        _live[index] = true;
        Count++;
        return new Handle<T>(index, _generations[index]);
    }

    /// <summary>Whether <paramref name="handle"/> refers to a live item.</summary>
    public bool Contains(Handle<T> handle) =>
        !handle.IsNone && (uint)handle.Index < (uint)_used && _live[handle.Index] && _generations[handle.Index] == handle.Generation;

    /// <summary>The item of <paramref name="handle"/>, if it is still live.</summary>
    public bool TryGet(Handle<T> handle, [MaybeNullWhen(false)] out T item)
    {
        if (Contains(handle))
        {
            item = _items[handle.Index];
            return true;
        }
        item = default;
        return false;
    }

    /// <summary>The item of <paramref name="handle"/>.</summary>
    /// <exception cref="InvalidOperationException">The handle is <see cref="Handle{T}.None"/>, was never issued by this pool, or its item was removed (stale); the message says which.</exception>
    public T Get(Handle<T> handle)
    {
        if (TryGet(handle, out var item)) return item;
        if (handle.IsNone) throw new InvalidOperationException($"The handle is {handle}: it refers to nothing.");
        if ((uint)handle.Index >= (uint)_used) throw new InvalidOperationException($"The handle {handle} was not issued by this pool.");
        throw new InvalidOperationException($"The handle {handle} is stale: its item was removed (the slot is now at generation {_generations[handle.Index]}).");
    }

    /// <summary>Removes the item of <paramref name="handle"/>; every handle to it becomes stale. False when it was not live.</summary>
    public bool Remove(Handle<T> handle)
    {
        if (!Contains(handle)) return false;
        int index = handle.Index;
        _items[index] = default!;
        _live[index] = false;
        // Generation 0 is reserved for None.
        _generations[index] = _generations[index] == uint.MaxValue ? 1 : _generations[index] + 1;
        _free.Push(index);
        Count--;
        return true;
    }
}
