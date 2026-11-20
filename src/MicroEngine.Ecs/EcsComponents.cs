using Arch.Core;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Ecs;

using World = Arch.Core.World;

/// <summary>Blends a component between its value when the step started and its value now, for drawing.</summary>
/// <param name="from">The value when the step started.</param>
/// <param name="to">The value now.</param>
/// <param name="blend">How far the frame lies between the two (0 to 1).</param>
public delegate T Blender<T>(in T from, in T to, float blend) where T : struct;

/// <summary>Adds a component's simulation state to a hash, for a component type that does not implement <see cref="IStateHashed"/>.</summary>
public delegate void ComponentHash<T>(in T component, StateHasher hash) where T : struct;

/// <summary>Writes a row's component to the transform of the scene entity it is linked to.</summary>
public delegate void WriteTransform<T>(in T component, ref Transform local) where T : struct;

/// <summary>Reads the transform of a scene entity into the component of the row linked to it.</summary>
public delegate void ReadTransform<T>(in Transform local, ref T component) where T : struct;

/// <summary>
/// The component types a store's rows are made of, and what the store does with each: whether it is simulation state
/// (part of the state hash), whether drawing blends it between two steps, and whether it is the place of a row linked
/// to a scene entity. Build one for the game, usually in a static field, and give it to every store
/// (<see cref="EcsExtensions.AddEcs(Scene, EcsComponents)"/>).
/// </summary>
/// <remarks>
/// Every component type of every row must be added here. A NativeAOT build cannot make the library's arrays for a
/// type nobody named, so adding a type also names it for the library (<c>ArrayRegistry.Add</c>); and so that a
/// forgotten type is found long before such a build, a store refuses a row with a component that is not here in every
/// build, at the end of the system that made it.
/// </remarks>
/// <example>
/// <code>
/// public static readonly EcsComponents Components = new EcsComponents()
///     .AddState&lt;Position&gt;().AddState&lt;Velocity&gt;().AddTag&lt;Frozen&gt;().Add&lt;Sprite&gt;()
///     .Interpolate&lt;Position&gt;(static (in Position a, in Position b, float t) =&gt; new Position { Value = Vector2.Lerp(a.Value, b.Value, t) })
///     .SyncTransform&lt;Position&gt;(static (in Position p, ref Transform local) =&gt; local.Position2D = p.Value);
/// </code>
/// </example>
public sealed class EcsComponents
{
    internal enum Kind { Plain, State, Tag }

    /// <summary>One component type: what is known of it without its type argument.</summary>
    internal abstract class Entry(Type type, Kind kind)
    {
        public Type Type => type;
        public Kind Kind => kind;
        public string Name { get; } = type.FullName ?? type.Name;
        public bool Interpolated { get; protected set; }
        public bool Synced { get; protected set; }
        public abstract void Hash(World world, StateHasher into, StateHasher scratch);
        public abstract IEcsSnapshot CreateInterpolation(EcsStore store);
        public abstract void Pull(World world, IWorld objects);
        public abstract void Push(World world, IWorld objects);
    }

    private sealed class Entry<T>(Kind kind, ComponentHash<T>? hash) : Entry(typeof(T), kind) where T : struct
    {
        private static readonly QueryDescription All = new QueryDescription().WithAll<T>();
        private static readonly QueryDescription Linked = new QueryDescription().WithAll<T, SceneLink>();
        private Blender<T>? _blend;
        private WriteTransform<T>? _write;
        private ReadTransform<T>? _read;

        public void SetBlend(Blender<T> blend) => (_blend, Interpolated) = (blend, true);
        public void SetSync(WriteTransform<T> write, ReadTransform<T>? read) => (_write, _read, Synced) = (write, read, true);

        // A sum of one hash per row: the same rows give the same sum in whatever order the library keeps them, so
        // a row that left an archetype and came back does not change the hash.
        public override void Hash(World world, StateHasher into, StateHasher scratch)
        {
            ulong sum = 0;
            int count = 0;
            foreach (ref var chunk in world.Query(in All))
            {
                int rows = chunk.Count;
                var values = hash is null ? default : chunk.GetSpan<T>();
                for (int i = 0; i < rows; i++)
                {
                    scratch.Reset();
                    scratch.Add(chunk.Entity(i).Id);
                    hash?.Invoke(in values[i], scratch);
                    sum += scratch.Value;
                }
                count += rows;
            }
            // A type no row has adds nothing: adding a component type to the game does not change old hashes.
            if (count == 0) return;
            into.Add(Name);
            into.Add(count);
            into.Add(sum);
        }

        public override IEcsSnapshot CreateInterpolation(EcsStore store) => new EcsInterpolation<T>(store, _blend!);

        public override void Pull(World world, IWorld objects)
        {
            if (_read is not { } read) return;
            foreach (ref var chunk in world.Query(in Linked))
            {
                var values = chunk.GetSpan<T>();
                var links = chunk.GetSpan<SceneLink>();
                for (int i = 0; i < chunk.Count; i++)
                    if (links[i].Mode == EcsLink.EntityMovesRow && objects.IsAlive(links[i].Entity))
                        read(in objects.Local(links[i].Entity), ref values[i]);
            }
        }

        public override void Push(World world, IWorld objects)
        {
            var write = _write!;
            foreach (ref var chunk in world.Query(in Linked))
            {
                var values = chunk.GetSpan<T>();
                var links = chunk.GetSpan<SceneLink>();
                for (int i = 0; i < chunk.Count; i++)
                    if (links[i].Mode == EcsLink.RowMovesEntity && objects.IsAlive(links[i].Entity))
                        write(in values[i], ref objects.Local(links[i].Entity));
            }
        }
    }

    private readonly Dictionary<Type, Entry> _byType = [];
    private Entry[]? _hashed, _interpolated, _synced;
    private bool _inUse;

    /// <summary>How many component types were added.</summary>
    public int Count => _byType.Count;

    /// <summary>Whether <paramref name="type"/> was added.</summary>
    public bool Contains(Type type) => _byType.ContainsKey(type);

    /// <summary>
    /// Adds a component type that is not simulation state: what drawing needs (a sprite, a colour), a cache, anything
    /// that can be rebuilt. It is left out of the state hash.
    /// </summary>
    /// <exception cref="InvalidOperationException">The type was already added, or a store already uses these components.</exception>
    public EcsComponents Add<T>() where T : struct => Add(new Entry<T>(Kind.Plain, null));

    /// <summary>Adds a component type that is simulation state: each row's value is part of the state hash, through the component's own <see cref="IStateHashed.HashState"/>.</summary>
    /// <exception cref="InvalidOperationException">The type was already added, or a store already uses these components.</exception>
    public EcsComponents AddState<T>() where T : struct, IStateHashed =>
        Add(new Entry<T>(Kind.State, static (in T component, StateHasher hash) => component.HashState(hash)));

    /// <summary>Adds a component type that is simulation state, hashed by <paramref name="hash"/>: for a type the game cannot make implement <see cref="IStateHashed"/>.</summary>
    /// <exception cref="InvalidOperationException">The type was already added, or a store already uses these components.</exception>
    public EcsComponents AddState<T>(ComponentHash<T> hash) where T : struct
    {
        ArgumentNullException.ThrowIfNull(hash);
        return Add(new Entry<T>(Kind.State, hash));
    }

    /// <summary>Adds a component type without data whose presence on a row is simulation state (frozen, shielded, selected): which rows have it is part of the state hash.</summary>
    /// <exception cref="InvalidOperationException">The type was already added, or a store already uses these components.</exception>
    public EcsComponents AddTag<T>() where T : struct => Add(new Entry<T>(Kind.Tag, null));

    private EcsComponents Add<T>(Entry<T> entry) where T : struct
    {
        Open();
        if (!_byType.TryAdd(typeof(T), entry))
            throw new InvalidOperationException($"{typeof(T).Name} was already added to these components.");
        ArchArrays.Name<T>();
        return this;
    }

    /// <summary>
    /// Marks an added component type for drawing between two steps: each store keeps its value from the start of the
    /// step and <see cref="EcsStore.Interpolated{T}"/> reads it blended with <paramref name="blend"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The type was not added, or a store already uses these components.</exception>
    public EcsComponents Interpolate<T>(Blender<T> blend) where T : struct
    {
        ArgumentNullException.ThrowIfNull(blend);
        Added<T>(nameof(Interpolate)).SetBlend(blend);
        return this;
    }

    /// <summary>
    /// Makes an added component type the place of linked rows: when a step ends, <paramref name="toEntity"/> writes
    /// the component of each row linked with <see cref="EcsLink.RowMovesEntity"/> to its scene entity's transform, so
    /// the scene world's hierarchy, drawing, physics and spatial index see the row where it is. With
    /// <paramref name="fromEntity"/>, a row linked with <see cref="EcsLink.EntityMovesRow"/> reads its entity's
    /// transform when a step starts instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">The type was not added, or a store already uses these components.</exception>
    public EcsComponents SyncTransform<T>(WriteTransform<T> toEntity, ReadTransform<T>? fromEntity = null) where T : struct
    {
        ArgumentNullException.ThrowIfNull(toEntity);
        Added<T>(nameof(SyncTransform)).SetSync(toEntity, fromEntity);
        return this;
    }

    private Entry<T> Added<T>(string what) where T : struct
    {
        Open();
        return _byType.TryGetValue(typeof(T), out var entry)
            ? (Entry<T>)entry
            : throw new InvalidOperationException($"{what}<{typeof(T).Name}> needs the type added first: AddState<{typeof(T).Name}>() or Add<{typeof(T).Name}>().");
    }

    private void Open()
    {
        if (_inUse) throw new InvalidOperationException("A store already uses these components: add every type before the first store is made.");
    }

    /// <summary>Called by a store: from now on the set is fixed.</summary>
    internal void Use() => _inUse = true;

    /// <summary>The hashed types in name order: the order of a hash must not depend on the order the game added them in.</summary>
    internal Entry[] Hashed => _hashed ??= [.. _byType.Values.Where(e => e.Kind != Kind.Plain).OrderBy(e => e.Name, StringComparer.Ordinal)];
    internal Entry[] InterpolatedTypes => _interpolated ??= [.. _byType.Values.Where(e => e.Interpolated)];
    internal Entry[] SyncedTypes => _synced ??= [.. _byType.Values.Where(e => e.Synced)];
}

/// <summary>Names component types for the library, once each: its arrays cannot be made in a NativeAOT build for a type nobody named.</summary>
internal static class ArchArrays
{
    private static readonly HashSet<Type> Named = [];

    public static void Name<T>() where T : struct
    {
        lock (Named)
        {
            if (Named.Add(typeof(T))) ArrayRegistry.Add<T>();
        }
    }
}
