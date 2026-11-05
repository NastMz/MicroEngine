using MicroEngine.Kernel;

namespace MicroEngine.Input;

/// <summary>
/// One local player: an action map of their own (<see cref="Map"/>) read from the devices paired with them.
/// </summary>
public sealed class PlayerInput
{
    private readonly Players _players;
    internal readonly HashSet<DeviceId> Paired = [];

    internal PlayerInput(Players players, int index, InputMap map)
    {
        _players = players;
        Index = index;
        Map = map;
        Owns = id => _players.OwnerOf(id) == this;
    }

    /// <summary>The player's position, 0-based: player 1 is 0.</summary>
    public int Index { get; internal set; }

    /// <summary>The player's actions, sampled once per update from their devices.</summary>
    public InputMap Map { get; }

    /// <summary>The devices paired with this player, in id order. Empty for player 1 until something is paired with them.</summary>
    public IReadOnlyList<DeviceId> Devices => [.. Paired.Order(Comparer<DeviceId>.Create(static (a, b) => a.Value.CompareTo(b.Value)))];

    /// <summary>Whether this player hears a device (see <see cref="Players.OwnerOf"/>).</summary>
    public Func<DeviceId, bool> Owns { get; }

    /// <summary>Pairs <paramref name="device"/> with this player, taking it from whoever had it.</summary>
    public PlayerInput Pair(DeviceId device)
    {
        _players.Pair(this, device);
        return this;
    }

    /// <summary>Ends the pairing of <paramref name="device"/> with this player; false when it was not paired with them.</summary>
    public bool Unpair(DeviceId device) => Paired.Remove(device);
}

/// <summary>
/// The local players. With no setup there is one player who owns every device, so a single-player game never deals
/// with players at all (<c>Game.Input</c> is player 1's map). For local multiplayer, pair devices explicitly: a
/// device paired with a player is heard only by that player, and once player 1 has a device of their own they stop
/// hearing the devices nobody was given.
/// </summary>
/// <remarks>
/// Who hears a device (<see cref="OwnerOf"/>): the player it is paired with; else player 1, while player 1 has no
/// device of their own; else nobody (it is free to join: <see cref="UnpairedPress"/>). So keyboard and gamepad never
/// cancel each other across players, and a pad that was never assigned does not drive anyone by accident.
/// </remarks>
/// <example>
/// <code>
/// // Player 1 plays on the keyboard; anyone pressing a button on a free pad joins as the next player.
/// Players[0].Pair(keyboard);
/// Players.UnpairedPress += (device, e) =&gt; { if (Players.Count &lt; 4) Players.Join(device); };
/// var p2 = Players[1].Map.Vector("left", "right", "up", "down");
/// </code>
/// </example>
public sealed class Players : IStateHashed
{
    private readonly List<PlayerInput> _players = [];

    /// <summary>Creates the players with player 1 using <paramref name="first"/>.</summary>
    public Players(InputMap first)
    {
        ArgumentNullException.ThrowIfNull(first);
        _players.Add(new PlayerInput(this, 0, first));
    }

    /// <summary>The player at <paramref name="index"/> (0 is player 1).</summary>
    /// <exception cref="ArgumentOutOfRangeException">There is no such player.</exception>
    public PlayerInput this[int index] => _players[index];

    /// <summary>The number of players (at least 1).</summary>
    public int Count => _players.Count;

    /// <summary>The players in order.</summary>
    public IReadOnlyList<PlayerInput> All => _players;

    /// <summary>The players in order, as the list itself: the input system walks it every update without boxing an enumerator.</summary>
    internal List<PlayerInput> InOrder => _players;

    /// <summary>
    /// Raised when a device nobody hears presses something (a key, a gamepad button, a touch): the moment to let that
    /// device join (<see cref="Join"/>). Raised during the frame's input routing.
    /// </summary>
    public event Action<DeviceId, InputEvent>? UnpairedPress;

    internal void ReportUnpaired(in InputEvent e) => UnpairedPress?.Invoke(e.Device, e);

    /// <summary>
    /// Adds a player paired with <paramref name="device"/> (taken from whoever had it), with their own actions:
    /// <paramref name="map"/>, or a copy of player 1's bindings.
    /// </summary>
    public PlayerInput Join(DeviceId device, InputMap? map = null)
    {
        var player = Add(map ?? _players[0].Map.CopyBindings());
        Pair(player, device);
        return player;
    }

    /// <summary>Adds a player with <paramref name="map"/> and no device yet (pair them with <see cref="PlayerInput.Pair"/>).</summary>
    public PlayerInput Add(InputMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (_players.Exists(p => ReferenceEquals(p.Map, map))) throw new ArgumentException("Every player needs an action map of their own.", nameof(map));
        var player = new PlayerInput(this, _players.Count, map);
        _players.Add(player);
        return player;
    }

    /// <summary>Removes a player other than player 1 (the next ones move up); their devices become free.</summary>
    /// <exception cref="ArgumentException"><paramref name="player"/> is player 1.</exception>
    public bool Remove(PlayerInput player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (ReferenceEquals(_players[0], player)) throw new ArgumentException("Player 1 cannot be removed.", nameof(player));
        if (!_players.Remove(player)) return false;
        for (int i = 0; i < _players.Count; i++) _players[i].Index = i;
        return true;
    }

    /// <summary>Pairs <paramref name="device"/> with <paramref name="player"/>, taking it from whoever had it.</summary>
    public void Pair(PlayerInput player, DeviceId device)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!_players.Contains(player)) throw new ArgumentException("The player is not one of these players.", nameof(player));
        foreach (var p in _players) p.Paired.Remove(device);
        player.Paired.Add(device);
    }

    /// <summary>Whether <paramref name="device"/> is paired with any player.</summary>
    public bool IsPaired(DeviceId device) => _players.Exists(p => p.Paired.Contains(device));

    /// <summary>
    /// The player who hears <paramref name="device"/>: the one it is paired with; else player 1 while player 1 has no
    /// device of their own; else null.
    /// </summary>
    public PlayerInput? OwnerOf(DeviceId device)
    {
        foreach (var p in _players)
            if (p.Paired.Contains(device)) return p;
        return _players[0].Paired.Count == 0 ? _players[0] : null;
    }

    /// <summary>
    /// Adds the players beyond player 1 (their sampled actions) and every pairing to the state hash; with one player
    /// who has no pairing it adds nothing, so a single-player game's hash is player 1's map alone.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        if (_players.Count == 1 && _players[0].Paired.Count == 0) return;
        hash.Add(_players.Count);
        foreach (var p in _players)
        {
            var devices = p.Devices;
            hash.Add(devices.Count);
            foreach (var d in devices) hash.Add(d.Value);
            if (p.Index > 0) hash.Add(p.Map);
        }
    }
}
