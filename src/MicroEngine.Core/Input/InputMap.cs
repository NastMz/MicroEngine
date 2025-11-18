namespace MicroEngine.Core.Input;

/// <summary>
/// Default implementation of input mapping system.
/// Tracks action states and updates them based on input backend.
/// </summary>
public sealed class InputMap : IInputMap
{
    private readonly Dictionary<string, InputAction> _actions;
    private readonly Dictionary<string, InputState> _actionStates;

    /// <inheritdoc/>
    public IReadOnlyList<InputAction> Actions => _actions.Values.ToList();

    /// <summary>
    /// Initializes a new instance of the <see cref="InputMap"/> class.
    /// </summary>
    public InputMap()
    {
        _actions = new Dictionary<string, InputAction>(StringComparer.OrdinalIgnoreCase);
        _actionStates = new Dictionary<string, InputState>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public void AddAction(InputAction action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _actions[action.Name] = action;
        _actionStates[action.Name] = new InputState();
    }

    /// <inheritdoc/>
    public InputAction? GetAction(string actionName)
    {
        return _actions.TryGetValue(actionName, out var action) ? action : null;
    }

    /// <inheritdoc/>
    public bool RemoveAction(string actionName)
    {
        _actionStates.Remove(actionName);
        return _actions.Remove(actionName);
    }

    /// <inheritdoc/>
    public bool IsActionPressed(string actionName)
    {
        return _actionStates.TryGetValue(actionName, out var state) && state.JustPressed;
    }

    /// <inheritdoc/>
    public bool IsActionReleased(string actionName)
    {
        return _actionStates.TryGetValue(actionName, out var state) && state.JustReleased;
    }

    /// <inheritdoc/>
    public bool IsActionHeld(string actionName)
    {
        return _actionStates.TryGetValue(actionName, out var state) && state.IsHeld;
    }

    /// <inheritdoc/>
    public float GetActionValue(string actionName)
    {
        return _actionStates.TryGetValue(actionName, out var state) ? state.Value : 0f;
    }

    /// <inheritdoc/>
    public void Update(IInputBackend inputBackend)
    {
        if (inputBackend == null)
        {
            throw new ArgumentNullException(nameof(inputBackend));
        }

        foreach (var (actionName, action) in _actions)
        {
            var previousState = _actionStates[actionName];
            var currentlyActive = false;
            var maxValue = 0f;

            foreach (var binding in action.Bindings)
            {
                var isActive = IsBindingActive(inputBackend, binding);
                if (isActive)
                {
                    currentlyActive = true;
                    maxValue = System.Math.Max(maxValue, 1f);
                }
            }

            _actionStates[actionName] = new InputState
            {
                IsHeld = currentlyActive,
                JustPressed = currentlyActive && !previousState.IsHeld,
                JustReleased = !currentlyActive && previousState.IsHeld,
                Value = maxValue
            };
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _actions.Clear();
        _actionStates.Clear();
    }

    private static bool IsBindingActive(IInputBackend inputBackend, InputBinding binding)
    {
        return binding.Type switch
        {
            InputBindingType.Keyboard => inputBackend.IsKeyDown((Key)binding.Code),
            InputBindingType.Mouse => inputBackend.IsMouseButtonDown((MouseButton)binding.Code),
            InputBindingType.Gamepad => inputBackend.IsGamepadButtonDown(
                binding.GamepadIndex,
                (GamepadButton)binding.Code),
            _ => false
        };
    }

    private struct InputState
    {
        public bool IsHeld { get; init; }
        public bool JustPressed { get; init; }
        public bool JustReleased { get; init; }
        public float Value { get; init; }
    }
}
