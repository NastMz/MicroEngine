using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Navigation;

/// <summary>What an agent of <see cref="NavAgents"/> is doing.</summary>
public enum NavAgentStatus
{
    /// <summary>It has no order: it stands where it is.</summary>
    Idle,
    /// <summary>It has an order and no route yet: its route is being searched, and it stands meanwhile.</summary>
    Planning,
    /// <summary>It walks its route, or follows its field.</summary>
    Moving,
    /// <summary>Its order ended: it stands at its destination, as near it as it could get, or beside those that arrived before it.</summary>
    Arrived,
}

/// <summary>How an agent of <see cref="NavAgents"/> moves, in the grid's world units and seconds.</summary>
public sealed record NavAgentSettings
{
    private readonly float _radius, _speed, _acceleration = float.PositiveInfinity, _tolerance, _slowing, _separation = 1, _avoidance = 1, _weight = 1;

    /// <summary>Settings for a body of <paramref name="radius"/> that walks at <paramref name="speed"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
    public NavAgentSettings(float radius, float speed)
    {
        Radius = radius;
        Speed = speed;
    }

    /// <summary>
    /// The body's radius: other bodies are kept that far from its centre, walls too (as a square of that half-side,
    /// like <see cref="NavGrid.FindPath"/>), and a body wider than a cell is led only where it fits. Zero or more.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float Radius { get => _radius; init => _radius = value >= 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Radius), value, "The radius is zero or more."); }

    /// <summary>The speed it walks at, in units per second. Positive.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive and finite.</exception>
    public float Speed { get => _speed; init => _speed = value > 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Speed), value, "The speed is positive."); }

    /// <summary>How fast its velocity changes, in units per second squared; infinite (the default) changes it at once.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public float Acceleration { get => _acceleration; init => _acceleration = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Acceleration), value, "The acceleration is positive."); }

    /// <summary>How near its destination counts as there. 0 (the default) stops it on the point itself.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float ArrivalTolerance { get => _tolerance; init => _tolerance = value >= 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(ArrivalTolerance), value, "The tolerance is zero or more."); }

    /// <summary>The distance from its destination at which it starts to slow down; 0 (the default) walks at full speed to the end.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float SlowingDistance { get => _slowing; init => _slowing = value >= 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(SlowingDistance), value, "The distance is zero or more."); }

    /// <summary>How strongly it steers away from bodies that are close (<see cref="Steering.Separate"/>): 1 by default, 0 for none.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float Separation { get => _separation; init => _separation = value >= 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Separation), value, "The weight is zero or more."); }

    /// <summary>How strongly it turns aside from bodies it is about to run into (<see cref="Steering.Avoid"/>): 1 by default, 0 for none.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float Avoidance { get => _avoidance; init => _avoidance = value >= 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Avoidance), value, "The weight is zero or more."); }

    /// <summary>
    /// How hard it is to push: when two bodies would overlap each gives way in proportion to the other's weight, and
    /// a body that stands (it arrived, or has no order) holds its ground eight times as hard as one that walks. 1 by
    /// default; a very large weight makes a body others go around. Positive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive and finite.</exception>
    public float Weight { get => _weight; init => _weight = value > 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Weight), value, "The weight is positive."); }

    /// <summary>
    /// Whether it takes part in the crowd (the default): it keeps apart from the others, never overlaps them and
    /// they go around it. False for what passes through bodies (a flyer, a ghost, a projectile that homes).
    /// </summary>
    public bool Collides { get; init; } = true;

    /// <summary>
    /// Whether costly ground slows it down: its speed is divided by the cost of the cell it is in
    /// (<see cref="NavGrid.SetCost"/>), so a cell that costs 3 is walked at a third of the speed. False by default.
    /// </summary>
    public bool SlowedByCost { get; init; }

    /// <summary>
    /// Whether the game moves the body itself (a character controller, a physics body, an animation): the system
    /// then only steers. Each step the game moves its body by <see cref="NavAgent.DesiredVelocity"/> and tells the
    /// agent where it ended (<see cref="NavAgent.Position"/>). Other agents go around it and never push it. False by
    /// default: the system moves the agent along the grid (<see cref="NavGrid.Slide"/>).
    /// </summary>
    public bool MovedByGame { get; init; }
}

/// <summary>
/// One agent of <see cref="NavAgents"/>: a body with a position that takes orders (<see cref="MoveTo"/>,
/// <see cref="Follow"/>, <see cref="Stop"/>) and tells how it is doing (<see cref="Status"/>). The system creates it
/// (<see cref="NavAgents.Add"/>) and moves it each step; the game keeps the reference, reads
/// <see cref="Position"/> to draw, and removes it when the thing it stands for is gone.
/// </summary>
public sealed class NavAgent
{
    internal NavAgents? Owner;
    internal int Index;
    internal SpatialHandle Handle;
    internal Vector2 Place, Drive, Wanted, Target, StallFrom, Aim;
    internal NavAgentSettings Config;
    internal NavAgentStatus State;
    internal PathStatus Search = PathStatus.Found;
    internal readonly List<Vector2> Points = [];
    internal readonly List<int> Cells = [];
    internal int CellAt, TargetNode = -1, EndNode = -1;
    internal bool Inside, Exact, Queued, Landing, Blocked;
    internal float StallTime, Nearest = float.PositiveInfinity, Waited;
    internal DistanceField? Field;

    internal NavAgent(NavAgents owner, Vector2 position, NavAgentSettings settings)
    {
        Owner = owner;
        Place = position;
        StallFrom = position;
        Config = settings;
    }

    /// <summary>
    /// Where the body's centre is. Setting it moves the body there at once (a teleport, or the place the game moved
    /// it to when it is <see cref="NavAgentSettings.MovedByGame"/>); its order stands.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The position is not finite.</exception>
    public Vector2 Position
    {
        get => Place;
        set
        {
            if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(nameof(value), value, "The position must be finite.");
            Place = value;
            Owner?.Moved(this, teleported: !Config.MovedByGame);
        }
    }

    /// <summary>How the agent moves. It may be replaced while the agent walks.</summary>
    public NavAgentSettings Settings
    {
        get => Config;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Config = value;
            Owner?.Resized(this);
        }
    }

    /// <summary>The body's radius (<see cref="NavAgentSettings.Radius"/>).</summary>
    public float Radius => Config.Radius;

    /// <summary>What the agent is doing.</summary>
    public NavAgentStatus Status => State;

    /// <summary>
    /// How the search for the agent's route ended: <see cref="PathStatus.Found"/> when the route reaches
    /// <see cref="Destination"/>, <see cref="PathStatus.Unreachable"/> when it leads as near as the agent gets, and
    /// <see cref="PathStatus.Searching"/> while a route is being searched.
    /// </summary>
    public PathStatus Path => Search;

    /// <summary>The point of the latest <see cref="MoveTo"/>.</summary>
    public Vector2 Destination => Target;

    /// <summary>The points the agent still walks through, in order; empty when it has no route.</summary>
    public IReadOnlyList<Vector2> Route => Points;

    /// <summary>
    /// The velocity the body moved with in the latest step, in units per second: zero when it stands. What others
    /// pushed it by is not counted.
    /// </summary>
    public Vector2 Velocity => Drive;

    /// <summary>
    /// The velocity the agent wants in this step, after steering around the others: what a game that moves the body
    /// itself (<see cref="NavAgentSettings.MovedByGame"/>) applies. Zero when the agent stands.
    /// </summary>
    public Vector2 DesiredVelocity => Wanted;

    /// <summary>
    /// Whether the agent has an order and made almost no way for a while (<see cref="NavAgents.BlockedAfter"/>):
    /// somebody stands in the only passage, or a wall the grid does not know holds it. It keeps trying, and searches
    /// its route again once; the game decides whether to give up (<see cref="Stop"/>) or order something else.
    /// </summary>
    public bool IsBlocked => Blocked;

    /// <summary>Free for the game: the unit, enemy or object this agent stands for.</summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Orders the agent to walk to <paramref name="point"/>. Its route is searched by the system's next step (later
    /// when many agents wait and <see cref="NavAgents.SearchBudget"/> is small); an agent that already walks keeps
    /// its previous route until the new one is known. A point that cannot be reached sends it as near as it gets.
    /// Calling it every step with a target that moves costs a search only when the target enters another cell.
    /// </summary>
    /// <exception cref="InvalidOperationException">The agent was removed from its system.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="point"/> is not finite.</exception>
    public void MoveTo(Vector2 point)
    {
        var owner = Attached();
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentOutOfRangeException(nameof(point), point, "The point must be finite.");
        owner.Send(this, point);
    }

    /// <summary>
    /// Orders the agent to follow <paramref name="field"/> to its goal: each step it heads for the cell the field
    /// gives (<see cref="DistanceField.Next"/>), with no search of its own, and arrives in the goal's cell or beside
    /// those that arrived before it. For many agents with one goal. The game computes the field again when the goal
    /// or the grid changes; the agents read whatever it holds.
    /// </summary>
    /// <exception cref="InvalidOperationException">The agent was removed from its system.</exception>
    /// <exception cref="ArgumentException"><paramref name="field"/> measures another grid.</exception>
    public void Follow(DistanceField field)
    {
        var owner = Attached();
        ArgumentNullException.ThrowIfNull(field);
        if (field.Grid != owner.Grid) throw new ArgumentException("The field measures another grid than the agents walk.", nameof(field));
        owner.Halt(this);
        Field = field;
        State = NavAgentStatus.Moving;
    }

    /// <summary>Ends the agent's order: it stands where it is, <see cref="NavAgentStatus.Idle"/>.</summary>
    /// <exception cref="InvalidOperationException">The agent was removed from its system.</exception>
    public void Stop()
    {
        Attached().Halt(this);
        State = NavAgentStatus.Idle;
    }

    private NavAgents Attached() => Owner ?? throw new InvalidOperationException("The agent was removed from its system.");
}

/// <summary>
/// The agents that walk a <see cref="NavGrid"/>: each follows the route to where it was sent, slows down and stops
/// there, searches again when a change of the grid closes its way, keeps apart from the others and turns aside from
/// those it would run into, and never enters a wall or another body. It is what a game writes after it has a path;
/// what an agent wants stays the game's. Add it to a scene (<c>Systems.Add(new NavAgents(grid))</c>) and it is
/// stepped with the scene and is part of its state hash; or keep one and call <see cref="Step"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><b>Order.</b> Agents are stepped in the order they were added, and searches are served in the order they
/// were asked for. With the same orders in the same steps a run repeats exactly, on every machine.</item>
/// <item><b>Searches.</b> One step looks at <see cref="SearchBudget"/> nodes at most, for all agents together: when
/// many are ordered at once on a large map, the rest wait their turn (<see cref="NavAgentStatus.Planning"/>) instead
/// of stalling the step.</item>
/// <item><b>Bodies.</b> An agent is a circle for the others and a square for walls. Agents are not obstacles in the
/// grid: block cells for things that stay.</item>
/// <item><b>Memory.</b> Stepping allocates nothing once the lists have grown.</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var agents = Systems.Add(new NavAgents(grid));
/// var unit = agents.Add(spawn, new NavAgentSettings(radius: 5, speed: 80));
/// unit.MoveTo(click);                                   // an order
/// // each frame
/// r.FillCircle(unit.Position, unit.Radius, color);
/// if (unit.Status == NavAgentStatus.Arrived) Salute(unit);
/// </code>
/// </example>
public sealed class NavAgents : SceneSystem
{
    // How far ahead, in seconds, a meeting with another body matters; how much harder a standing body holds its place.
    private const float Horizon = 0.5f, Hold = 8;
    // How many times a step moves overlapping bodies apart: each pass halves what a dense crowd still overlaps by.
    private const int Passes = 4;
    // How long an agent beside those that arrived looks for a place nearer its point before it stops, in seconds.
    private const float Patience = 0.25f;

    private readonly List<NavAgent> _agents = [];
    private readonly SpatialIndex2D<NavAgent> _index;
    private readonly PathFinder _finder = new();
    private readonly List<NavAgent> _queue = [];
    private readonly List<NavRegions> _regions = [];
    private readonly List<NavAgent> _near = [];
    private static readonly Comparison<NavAgent> ByIndex = (a, b) => a.Index.CompareTo(b.Index);
    private static readonly IComparer<NavAgent> InOrder = Comparer<NavAgent>.Create(ByIndex);
    private bool _begun;
    private int _seenVersion, _budget = 4096, _token;
    private float _maxRadius, _blockedAfter = 1;

    /// <summary>A system of agents that walk <paramref name="grid"/>.</summary>
    public NavAgents(NavGrid grid)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _index = new SpatialIndex2D<NavAgent>(grid.CellSize * 2);
        _seenVersion = grid.Version;
    }

    /// <summary>The grid the agents walk.</summary>
    public NavGrid Grid { get; }

    /// <summary>How many agents there are.</summary>
    public int Count => _agents.Count;

    /// <summary>The agent at <paramref name="index"/>, in the order they were added.</summary>
    public NavAgent this[int index] => _agents[index];

    /// <summary>
    /// The most nodes all the searches of one step look at together (4096 by default; <see cref="int.MaxValue"/> for
    /// no limit). Agents whose route is not known yet wait their turn. Positive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public int SearchBudget
    {
        get => _budget;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _budget = value;
        }
    }

    /// <summary>
    /// After how many seconds of making almost no way an agent with an order is <see cref="NavAgent.IsBlocked"/>;
    /// 1 by default. Positive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive and finite.</exception>
    public float BlockedAfter
    {
        get => _blockedAfter;
        set => _blockedAfter = value > 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "The time is positive.");
    }

    /// <summary>How many searches the agents began since the system was made (a measure of its work).</summary>
    public int Searches { get; private set; }

    /// <summary>How many nodes those searches looked at.</summary>
    public long Expanded { get; private set; }

    /// <summary>Adds an agent at <paramref name="position"/>, with no order.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is not finite.</exception>
    public NavAgent Add(Vector2 position, NavAgentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be finite.");
        var agent = new NavAgent(this, position, settings) { Index = _agents.Count };
        agent.Handle = _index.Add(agent, position);
        _agents.Add(agent);
        _maxRadius = MathF.Max(_maxRadius, settings.Radius);
        return agent;
    }

    /// <summary>Whether <paramref name="agent"/> is in this system.</summary>
    public bool Contains(NavAgent agent) => agent is not null && agent.Owner == this;

    /// <summary>Removes <paramref name="agent"/>; it takes no more orders. Returns whether it was in the system.</summary>
    public bool Remove(NavAgent agent)
    {
        if (!Contains(agent)) return false;
        Halt(agent);
        _index.Remove(agent.Handle);
        _agents.RemoveAt(agent.Index);
        for (int i = agent.Index; i < _agents.Count; i++) _agents[i].Index = i;
        agent.Owner = null;
        return true;
    }

    /// <summary>Removes every agent.</summary>
    public void Clear()
    {
        foreach (var agent in _agents) agent.Owner = null;
        _agents.Clear();
        _queue.Clear();
        _index.Clear();
        _begun = false;
        _maxRadius = 0;
    }

    /// <summary>
    /// Adds to <paramref name="results"/> (without clearing it) the agents whose body reaches within
    /// <paramref name="radius"/> of <paramref name="center"/>, in the order they were added, and returns how many:
    /// the creeps in a tower's range, the units under a selection circle.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="center"/> is not finite, or <paramref name="radius"/> is negative or not finite.</exception>
    public int Query(Vector2 center, float radius, List<NavAgent> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        int first = results.Count;
        _index.QueryCircle(center, radius + _maxRadius, results);
        int kept = first;
        for (int i = first; i < results.Count; i++)
        {
            var agent = results[i];
            float reach = radius + agent.Radius;
            if (Vector2.DistanceSquared(agent.Place, center) <= reach * reach) results[kept++] = agent;
        }
        results.RemoveRange(kept, results.Count - kept);
        results.Sort(first, kept - first, InOrder);
        return kept - first;
    }

    /// <summary>Whether the body of some agent overlaps <paramref name="area"/>.</summary>
    public bool Occupied(RectF area)
    {
        _near.Clear();
        _index.Query(area.Inflate(_maxRadius), _near);
        foreach (var agent in _near)
        {
            var nearest = Vector2.Clamp(agent.Place, new Vector2(area.Left, area.Top), new Vector2(area.Right, area.Bottom));
            float distance = Vector2.DistanceSquared(agent.Place, nearest);
            if (distance < agent.Radius * agent.Radius || (distance == 0 && area.Width > 0 && area.Height > 0)) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether <paramref name="area"/> can be blocked (a tower built, a gate shut) without harming the agents: no
    /// body overlaps it, and every agent that walks to a destination it can reach keeps a way to it. Nothing is
    /// changed: when it is true, block the area (<see cref="NavGrid.SetBlocked"/>) and the agents whose way it
    /// closes go around. It does not know the game's own fixed places (the gate creeps will come from): ask
    /// <see cref="Regions"/> for those. Agents that follow a field are not looked at.
    /// </summary>
    public bool CanBlock(RectF area)
    {
        if (Occupied(area)) return false;
        int token = ++_token;
        foreach (var agent in _agents)
        {
            if (agent.Field is not null || agent.State is NavAgentStatus.Idle or NavAgentStatus.Arrived || agent.TargetNode < 0) continue;
            int from = Grid.NodeAt(agent.Place);
            if (from < 0) continue;
            var regions = Regions(agent.Radius);
            int to = agent.Search == PathStatus.Searching || agent.EndNode < 0 ? regions.NearestReachable(from, agent.TargetNode) : agent.EndNode;
            if (to >= 0 && regions.Connected(from, to) && !regions.ConnectedWith(area, token, from, to)) return false;
        }
        return true;
    }

    /// <summary>
    /// What agents of <paramref name="radius"/> can reach on the grid: the regions the system itself asks before it
    /// searches, kept up to date with the grid. One is shared by every size that needs the same room.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite.</exception>
    public NavRegions Regions(float radius)
    {
        if (!(radius >= 0) || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius), radius, "The radius is zero or more.");
        int rings = Grid.Rings(radius);
        foreach (var regions in _regions)
            if (Grid.Rings(regions.Radius) == rings) return regions;
        var made = new NavRegions(Grid, radius);
        _regions.Add(made);
        return made;
    }

    /// <inheritdoc/>
    protected override void Update(float dt) => Step(dt);

    /// <inheritdoc/>
    protected override void Stop() => Clear();

    /// <summary>
    /// Advances every agent by <paramref name="dt"/> seconds: searches the routes asked for, steers, moves and keeps
    /// the bodies apart. A scene calls it for a system it holds; call it yourself for one kept outside a scene.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dt"/> is negative or not finite.</exception>
    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), dt, "The step must be finite and not negative.");
        if (dt == 0) return;
        var grid = Grid;
        if (grid.Version != _seenVersion)
        {
            _seenVersion = grid.Version;
            _begun = false;                   // a search in progress saw the old map: it starts over
            foreach (var agent in _agents) Review(agent);
        }
        Plan();

        foreach (var agent in _agents) Steer(agent, dt);

        foreach (var agent in _agents)
        {
            var config = agent.Config;
            if (config.MovedByGame)
            {
                agent.Drive = agent.Wanted;
                continue;
            }
            var velocity = agent.Landing ? agent.Wanted : MathX.Approach(agent.Drive, agent.Wanted, config.Acceleration * dt);
            if (velocity == Vector2.Zero)
            {
                agent.Drive = Vector2.Zero;
                continue;
            }
            var from = agent.Place;
            var to = grid.Slide(from, velocity * dt, config.Radius);
            if (agent.Landing)
            {
                // The last step of a route ends on its point, not a rounding away from it.
                var end = agent.Points[^1];
                if (Vector2.DistanceSquared(to, end) <= 1e-6f * grid.CellSize * grid.CellSize) to = end;
            }
            agent.Place = to;
            agent.Drive = (to - from) / dt;
            if (agent.Landing && to == agent.Points[^1]) Arrive(agent);
        }
        foreach (var agent in _agents) _index.Move(agent.Handle, agent.Place);

        for (int pass = 0; pass < Passes; pass++)
        {
            bool pushed = false;
            foreach (var agent in _agents) pushed |= Part(agent);
            if (!pushed) break;
            foreach (var agent in _agents) _index.Move(agent.Handle, agent.Place);
        }

        foreach (var agent in _agents) Settle(agent, dt);
    }

    /// <inheritdoc/>
    public override void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(_agents.Count);
        foreach (var agent in _agents)
        {
            hash.Add(agent.Place);
            hash.Add(agent.Drive);
            hash.Add(agent.Wanted);
            hash.Add(agent.Aim);
            hash.Add((int)agent.State);
            hash.Add((int)agent.Search);
            hash.Add(agent.Target);
            hash.Add(agent.TargetNode);
            hash.Add(agent.EndNode);
            hash.Add(agent.Field is not null);
            hash.Add(agent.Queued);
            hash.Add(agent.Blocked);
            hash.Add(agent.StallTime);
            hash.Add(agent.StallFrom);
            hash.Add(agent.Nearest);
            hash.Add(agent.Waited);
            hash.Add(agent.CellAt);
            hash.Add(agent.Points.Count);
            foreach (var point in agent.Points) hash.Add(point);
        }
        hash.Add(_queue.Count);
        foreach (var agent in _queue) hash.Add(agent.Index);
        hash.Add(_begun);
        hash.Add(_finder.Expanded);
    }

    // ------------------------------------------------------------------ orders

    internal void Moved(NavAgent agent, bool teleported)
    {
        _index.Move(agent.Handle, agent.Place);
        if (!teleported) return;
        agent.StallFrom = agent.Place;
        agent.StallTime = 0;
    }

    internal void Resized(NavAgent agent)
    {
        _maxRadius = 0;
        foreach (var other in _agents) _maxRadius = MathF.Max(_maxRadius, other.Radius);
    }

    // Ends whatever the agent was doing: its route, its field, its place in the queue of searches.
    internal void Halt(NavAgent agent)
    {
        if (agent.Queued)
        {
            if (_queue[0] == agent) _begun = false;
            _queue.Remove(agent);
            agent.Queued = false;
        }
        agent.Points.Clear();
        agent.Cells.Clear();
        agent.Field = null;
        agent.TargetNode = agent.EndNode = -1;
        agent.Search = PathStatus.Found;
        agent.Wanted = agent.Drive = Vector2.Zero;
        agent.Landing = agent.Blocked = false;
        agent.StallTime = agent.Waited = 0;
        agent.Nearest = float.PositiveInfinity;
    }

    internal void Send(NavAgent agent, Vector2 point)
    {
        var grid = Grid;
        var cell = grid.CellAt(point);
        bool inside = grid.InBounds(cell.X, cell.Y);
        int node = Math.Clamp(cell.Y, 0, grid.Height - 1) * grid.Width + Math.Clamp(cell.X, 0, grid.Width - 1);
        if (agent.Field is null && node == agent.TargetNode && inside == agent.Inside && agent.State != NavAgentStatus.Idle)
        {
            // The same cell as before: the route stands, and only its last point moves.
            agent.Target = point;
            if (agent.Queued || !agent.Exact) return;
            var end = grid.Settle(point, node, agent.Radius);
            if (agent.State == NavAgentStatus.Moving && agent.Points.Count > 0) agent.Points[^1] = end;
            else if (agent.State == NavAgentStatus.Arrived && Vector2.Distance(agent.Place, end) > Tolerance(agent))
            {
                agent.Points.Clear();
                agent.Points.Add(end);
                agent.State = NavAgentStatus.Moving;
                Moved(agent, teleported: true);
            }
            return;
        }
        agent.Field = null;
        agent.Target = point;
        agent.TargetNode = node;
        agent.Inside = inside;
        agent.EndNode = -1;
        Request(agent);
    }

    // Asks for the agent's route to be searched. It keeps walking the route it has meanwhile.
    private void Request(NavAgent agent)
    {
        agent.Search = PathStatus.Searching;
        agent.State = agent.Points.Count > 0 && agent.State == NavAgentStatus.Moving ? NavAgentStatus.Moving : NavAgentStatus.Planning;
        if (agent.State == NavAgentStatus.Planning) agent.Points.Clear();
        if (agent.Queued) return;
        agent.Queued = true;
        _queue.Add(agent);
    }

    // The map changed: an agent whose way closed, or whose way may have opened, searches again.
    private void Review(NavAgent agent)
    {
        if (agent.Field is not null || agent.Queued || agent.TargetNode < 0) return;
        var grid = Grid;
        bool walking = agent.State == NavAgentStatus.Moving;
        if (walking && agent.Cells.Count > 0 && !grid.IsOpen(agent.Cells, Math.Min(agent.CellAt, agent.Cells.Count - 1), agent.Radius))
        {
            Request(agent);
            return;
        }
        if (agent.Exact) return;
        // It was going, or went, as near as it could: is there somewhere nearer now?
        int from = grid.NodeAt(agent.Place);
        if (from >= 0 && Regions(agent.Radius).NearestReachable(from, agent.TargetNode) != agent.EndNode) Request(agent);
    }

    // ------------------------------------------------------------------ searches

    private void Plan()
    {
        int budget = _budget;
        while (_queue.Count > 0 && budget > 0)
        {
            var agent = _queue[0];
            if (!_begun && !Begin(agent))
            {
                Dequeue(agent);
                continue;
            }
            _begun = true;
            int before = _finder.Expanded;
            var status = _finder.Continue(budget);
            int spent = _finder.Expanded - before;
            budget -= spent;
            Expanded += spent;
            if (status == PathStatus.Searching) break;
            Finish(agent, status);
            Dequeue(agent);
        }
    }

    private void Dequeue(NavAgent agent)
    {
        _queue.RemoveAt(0);
        agent.Queued = false;
        _begun = false;
    }

    // Begins the agent's search, or ends its order when there is nowhere to go. The search is to the cell of the
    // agent's region nearest its target, so it always finds its goal.
    private bool Begin(NavAgent agent)
    {
        var grid = Grid;
        int start = grid.NodeAt(agent.Place);
        int reach = start < 0 ? -1 : Regions(agent.Radius).NearestReachable(start, agent.TargetNode);
        if (reach < 0)
        {
            agent.Points.Clear();
            agent.Cells.Clear();
            agent.EndNode = -1;
            agent.Exact = false;
            agent.Search = PathStatus.Unreachable;
            Arrive(agent);
            return false;
        }
        agent.EndNode = reach;
        agent.Exact = agent.Inside && reach == agent.TargetNode;
        _finder.Begin(grid.ForAgent(agent.Radius), start, reach);
        Searches++;
        return true;
    }

    private void Finish(NavAgent agent, PathStatus status)
    {
        bool exact = agent.Exact && status == PathStatus.Found;
        Grid.Route(_finder.Path, agent.Place, agent.Target, exact, agent.Points, agent.Radius);
        agent.Cells.Clear();
        agent.Cells.AddRange(_finder.Path);
        agent.CellAt = 0;
        agent.Exact = exact;
        agent.Search = status == PathStatus.Found && !exact ? PathStatus.Unreachable : status;
        agent.StallTime = agent.Waited = 0;
        agent.Nearest = float.PositiveInfinity;
        agent.StallFrom = agent.Place;
        if (agent.Points.Count > 0) agent.State = NavAgentStatus.Moving;
        else Arrive(agent);
    }

    private static void Arrive(NavAgent agent)
    {
        agent.State = NavAgentStatus.Arrived;
        agent.Points.Clear();
        agent.Wanted = agent.Drive = Vector2.Zero;
        agent.Landing = agent.Blocked = false;
        agent.StallTime = 0;
    }

    private float Tolerance(NavAgent agent) => MathF.Max(agent.Config.ArrivalTolerance, 1e-3f * Grid.CellSize);

    // ------------------------------------------------------------------ a step

    // What the agent wants this step: towards the next point of its route (or the next cell of its field), slower
    // near the end, turned by the bodies around it.
    private void Steer(NavAgent agent, float dt)
    {
        agent.Wanted = Vector2.Zero;
        agent.Landing = false;

        agent.Aim = Vector2.Zero;
        if (agent.State != NavAgentStatus.Moving) return;
        var grid = Grid;
        var config = agent.Config;
        var position = agent.Place;
        int node = grid.NodeAt(position);
        float speed = config.Speed;
        if (config.SlowedByCost && node >= 0) speed /= grid.CostOf(node);

        Vector2 target;
        bool last;
        if (agent.Field is { } field)
        {
            int next = node < 0 ? -1 : field.Next(node);
            if (next < 0) return;
            target = grid.CenterOf(next);
            last = next == node;
        }
        else
        {
            var points = agent.Points;
            if (points.Count == 0)
            {
                // The old route ran out while the new one is searched: wait for it.
                if (agent.Queued) agent.State = NavAgentStatus.Planning;
                else Arrive(agent);
                return;
            }
            // Past the points reached, and past a corner as soon as the way to the point after it is clear.
            float reach = MathF.Max(config.Radius, speed * dt);
            while (points.Count > 1)
            {
                float left = Vector2.Distance(position, points[0]);
                if (left > reach && (left > 2 * grid.CellSize || !grid.HasDirectWay(position, points[1], config.Radius))) break;
                points.RemoveAt(0);
            }
            target = points[0];
            last = points.Count == 1;
            // Where it is along the cells of its path, for the day the map changes.
            for (int i = agent.CellAt + 1, end = Math.Min(agent.Cells.Count, agent.CellAt + 5); i < end; i++)
                if (agent.Cells[i] == node) agent.CellAt = i;
        }

        var to = target - position;
        float distance = to.Length();
        Vector2 wanted;
        if (last)
        {
            if (distance <= Tolerance(agent))
            {
                if (agent.Queued) agent.State = NavAgentStatus.Planning;
                else Arrive(agent);
                return;
            }
            if (config.SlowingDistance > 0 && distance < config.SlowingDistance) speed = MathF.Max(speed * distance / config.SlowingDistance, config.Speed * 0.1f);
            if (distance <= speed * dt && agent.Field is null && !agent.Queued)
            {
                agent.Landing = true;
                agent.Wanted = to / dt;
                return;
            }
        }
        agent.Aim = distance > 0 ? to / distance : Vector2.Zero;
        wanted = agent.Aim * MathF.Min(speed, distance / dt);

        if (config.Collides && (config.Separation > 0 || config.Avoidance > 0) && _agents.Count > 1)
        {
            float keep = MathF.Max(config.Radius, 1e-3f * grid.CellSize);
            Near(agent, MathF.Max(keep, 2 * config.Speed * Horizon));
            Vector2 apart = Vector2.Zero, aside = Vector2.Zero;
            foreach (var other in _near)
            {
                if (other == agent || !other.Config.Collides) continue;
                // Two that head the same way into the same place (a door, a corner) would each give way to the other
                // for ever: the one added first goes first, and the other keeps clear of it.
                if (other.Index > agent.Index && other.State == NavAgentStatus.Moving && Vector2.Dot(agent.Aim, other.Aim) > 0) continue;
                // Room is kept between bodies that walk; one that stands is gone around, and stood beside.
                if (other.State == NavAgentStatus.Moving) apart += Steering.Separate(position, config.Radius, other.Place, other.Radius, keep);
                aside += Steering.Avoid(position, wanted, config.Radius, other.Place, other.Drive, other.Radius, Horizon);
            }
            wanted = Steering.Limit(wanted + speed * (config.Separation * apart + config.Avoidance * Steering.Limit(aside, 1)), speed);
        }
        agent.Wanted = wanted;
    }

    // The agents whose body may be within `margin` of this one's, in the order they were added.
    private void Near(NavAgent agent, float margin)
    {
        _near.Clear();
        _index.QueryCircle(agent.Place, agent.Radius + _maxRadius + margin, _near);
        if (_near.Count > 1) _near.Sort(ByIndex);
    }

    // Moves apart the bodies that overlap this one, each by its share. Pairs are handled from the earlier agent.
    private bool Part(NavAgent agent)
    {
        if (!agent.Config.Collides || _agents.Count < 2) return false;
        var grid = Grid;
        Near(agent, 0);
        bool pushed = false;
        foreach (var other in _near)
        {
            if (other.Index <= agent.Index || !other.Config.Collides) continue;
            var between = other.Place - agent.Place;
            float distance = between.Length(), overlap = agent.Radius + other.Radius - distance;
            if (!(overlap > 0)) continue;
            var along = distance > 0 ? between / distance : new Vector2(1, 0);
            float mine = Hardness(agent), theirs = Hardness(other);
            if (float.IsPositiveInfinity(mine) && float.IsPositiveInfinity(theirs)) continue;
            float share = float.IsPositiveInfinity(mine) ? 0 : float.IsPositiveInfinity(theirs) ? 1 : theirs / (mine + theirs);
            // Each gives way by its share; what a wall keeps one from giving, the other gives.
            float given = 0;
            if (share > 0) given += Give(agent, -along, overlap * share);
            if (!float.IsPositiveInfinity(theirs)) given += Give(other, along, overlap - given);
            if (given < overlap && !float.IsPositiveInfinity(mine)) Give(agent, -along, overlap - given);
            pushed = true;
        }
        return pushed;
    }

    // Moves a body along a direction by an amount, as far as walls let it, and returns how far it went.
    private float Give(NavAgent agent, Vector2 along, float amount)
    {
        var from = agent.Place;
        agent.Place = Grid.Slide(from, along * amount, agent.Radius);
        return Vector2.Dot(agent.Place - from, along);
    }

    // How far the agent still is from where it goes, in a straight line (through its field's cells for a follower).
    private float Left(NavAgent agent)
    {
        if (agent.Field is not { } field) return agent.Points.Count > 0 ? Vector2.Distance(agent.Place, agent.Points[^1]) : 0;
        int node = Grid.NodeAt(agent.Place);
        float cells = node < 0 ? float.PositiveInfinity : field.DistanceAt(node);
        return cells == 0 ? Vector2.Distance(agent.Place, Grid.CenterOf(node)) : cells * Grid.CellSize + Grid.CellSize;
    }

    private static float Hardness(NavAgent agent) =>
        agent.Config.MovedByGame ? float.PositiveInfinity : agent.Config.Weight * (agent.State == NavAgentStatus.Moving ? 1 : Hold);

    // After the bodies moved: an agent that reached those standing at its destination has arrived too, and one that
    // made no way for a while says so.
    private void Settle(NavAgent agent, float dt)
    {
        if (agent.State != NavAgentStatus.Moving) return;
        var config = agent.Config;
        if (config.Collides && !agent.Queued && _agents.Count > 1)
        {
            float slack = 0.25f * MathF.Max(config.Radius, 1e-3f * Grid.CellSize);
            Near(agent, slack);
            bool lastLeg = agent.Field is null && agent.Points.Count == 1, beside = false;
            bool close = lastLeg || (agent.Field is not null && Left(agent) < Grid.CellSize);
            foreach (var other in _near)
            {
                if (other == agent || !other.Config.Collides || other.State == NavAgentStatus.Planning) continue;
                float reach = config.Radius + other.Radius;
                if (Vector2.Distance(agent.Place, other.Place) > reach + slack) continue;
                // Those sent to the same place: the ones that arrived, and at the end of the way the ones still
                // pressing for the point, which not all of them can stand on.
                bool together = (other.State == NavAgentStatus.Arrived || (close && other.State == NavAgentStatus.Moving))
                    && (agent.Field is not null ? other.Field == agent.Field : other.Field is null && other.TargetNode >= 0 && Vector2.Distance(agent.Target, other.Target) <= reach);
                // Somebody who is not going anywhere stands on the very point it was sent to.
                bool taken = lastLeg && other.State != NavAgentStatus.Moving && Vector2.Distance(agent.Points[0], other.Place) < reach;
                if (taken)
                {
                    Arrive(agent);
                    return;
                }
                beside |= together;
            }
            // Beside those that arrived, it still looks for a place nearer the point: it has arrived once it finds none.
            float left = Left(agent);
            if (left < agent.Nearest - 1e-3f * Grid.CellSize)
            {
                agent.Nearest = left;
                agent.Waited = 0;
            }
            else if (beside && (agent.Waited += dt) >= Patience)
            {
                Arrive(agent);
                return;
            }
        }

        agent.StallTime += dt;
        if (agent.StallTime < _blockedAfter) return;
        bool stalled = Vector2.Distance(agent.Place, agent.StallFrom) < 0.1f * config.Speed * agent.StallTime;
        agent.StallTime = 0;
        agent.StallFrom = agent.Place;
        if (stalled && !agent.Blocked && agent.Field is null && !agent.Queued && agent.TargetNode >= 0)
        {
            // Pushed off its route, perhaps: one more search from where it is.
            agent.Blocked = true;
            Request(agent);
            return;
        }
        agent.Blocked = stalled;
    }
}
