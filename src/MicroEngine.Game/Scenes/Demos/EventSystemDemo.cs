using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Events;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates event-driven architecture using EventBus.
/// Shows decoupled communication with event chains and statistics.
/// </summary>
public sealed class EventSystemDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;
    private EventBus _eventBus = null!;

    private const string SCENE_NAME = "EventSystemDemo";
    
    private Entity _button1;
    private Entity _button2;
    private Entity _trigger;
    private Entity _target;
    
    private readonly List<string> _eventLog = new();
    private int _totalEventsPublished;
    private int _totalEventsHandled;
    private float _targetActivationTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSystemDemo"/> class.
    /// </summary>
    public EventSystemDemo()
        : base(SCENE_NAME)
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _eventBus = context.Services.GetService<EventBus>();
        _inputBackend = context.InputBackend;
        _renderer = context.Renderer;
        _logger = context.Logger;
        _logger.Info(SCENE_NAME, "Event System demo loaded - demonstrating EventBus patterns");

        CreateEntities();
        SubscribeToEvents();
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_targetActivationTimer > 0)
        {
            _targetActivationTimer -= deltaTime;
        }

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
        }
        else if (_inputBackend.IsKeyPressed(Key.One))
        {
            PressButton(1);
        }
        else if (_inputBackend.IsKeyPressed(Key.Two))
        {
            PressButton(2);
        }
        else if (_inputBackend.IsKeyPressed(Key.Space))
        {
            EnterTrigger();
        }
        else if (_inputBackend.IsKeyPressed(Key.C))
        {
            ClearLog();
        }

        // Process queued events
        _eventBus.ProcessEvents();
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderer.Clear(new Color(20, 25, 30, 255));

        RenderEntities();
        RenderUI();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        UnsubscribeFromEvents();
        _logger?.Info(SCENE_NAME, "Event System demo unloaded");
    }

    private void CreateEntities()
    {
        // Button 1
        _button1 = World.CreateEntity("Button1");
        World.AddComponent(_button1, new TransformComponent { Position = new Vector2(100, 200) });

        // Button 2
        _button2 = World.CreateEntity("Button2");
        World.AddComponent(_button2, new TransformComponent { Position = new Vector2(250, 200) });

        // Trigger
        _trigger = World.CreateEntity("Trigger");
        World.AddComponent(_trigger, new TransformComponent { Position = new Vector2(200, 350) });

        // Target
        _target = World.CreateEntity("Target");
        World.AddComponent(_target, new TransformComponent { Position = new Vector2(400, 300) });
    }

    private void SubscribeToEvents()
    {
        _eventBus.Subscribe<ButtonPressedEvent>(OnButtonPressed);
        _eventBus.Subscribe<TriggerEnteredEvent>(OnTriggerEntered);
        _eventBus.Subscribe<TargetActivatedEvent>(OnTargetActivated);
    }

    private void UnsubscribeFromEvents()
    {
        _eventBus.Unsubscribe<ButtonPressedEvent>(OnButtonPressed);
        _eventBus.Unsubscribe<TriggerEnteredEvent>(OnTriggerEntered);
        _eventBus.Unsubscribe<TargetActivatedEvent>(OnTargetActivated);
    }

    private void PressButton(int buttonNumber)
    {
        var evt = new ButtonPressedEvent { ButtonNumber = buttonNumber };
        _eventBus.Queue(evt);
        _totalEventsPublished++;
        AddToLog($"Button {buttonNumber} pressed");
        _logger.Debug(SCENE_NAME, $"Button {buttonNumber} pressed - event queued");
    }

    private void EnterTrigger()
    {
        var evt = new TriggerEnteredEvent();
        _eventBus.Queue(evt);
        _totalEventsPublished++;
        AddToLog("Trigger entered");
        _logger.Debug(SCENE_NAME, "Trigger entered - event queued");
    }

    private void OnButtonPressed(ButtonPressedEvent evt)
    {
        _totalEventsHandled++;
        AddToLog($"Button {evt.ButtonNumber} event handled");
        
        // Button press triggers the trigger (event chain)
        var triggerEvt = new TriggerEnteredEvent();
        _eventBus.Queue(triggerEvt);
        _totalEventsPublished++;
        
        _logger.Info(SCENE_NAME, $"Button {evt.ButtonNumber} event handled - triggering chain");
    }

    private void OnTriggerEntered(TriggerEnteredEvent evt)
    {
        _totalEventsHandled++;
        AddToLog("Trigger event handled");
        
        // Trigger activates the target (event chain continues)
        var targetEvt = new TargetActivatedEvent();
        _eventBus.Queue(targetEvt);
        _totalEventsPublished++;
        
        _logger.Info(SCENE_NAME, "Trigger event handled - activating target");
    }

    private void OnTargetActivated(TargetActivatedEvent evt)
    {
        _totalEventsHandled++;
        _targetActivationTimer = 2f;
        AddToLog("Target activated!");
        _logger.Info(SCENE_NAME, "Target activated - event chain complete");
    }

    private void AddToLog(string message)
    {
        _eventLog.Insert(0, message);
        if (_eventLog.Count > 5)
        {
            _eventLog.RemoveAt(5);
        }
    }

    private void ClearLog()
    {
        _eventLog.Clear();
        _totalEventsPublished = 0;
        _totalEventsHandled = 0;
        _logger.Info(SCENE_NAME, "Event log cleared");
    }

    private void RenderEntities()
    {
        // Button 1
        var button1Transform = World.GetComponent<TransformComponent>(_button1);
        _renderer.DrawRectangle(button1Transform.Position, new Vector2(80, 50), new Color(100, 150, 255, 255));
        _renderer.DrawText("BTN 1", new Vector2(button1Transform.Position.X - 20, button1Transform.Position.Y - 8), 14, Color.White);

        // Button 2
        var button2Transform = World.GetComponent<TransformComponent>(_button2);
        _renderer.DrawRectangle(button2Transform.Position, new Vector2(80, 50), new Color(100, 150, 255, 255));
        _renderer.DrawText("BTN 2", new Vector2(button2Transform.Position.X - 20, button2Transform.Position.Y - 8), 14, Color.White);

        // Trigger
        var triggerTransform = World.GetComponent<TransformComponent>(_trigger);
        _renderer.DrawRectangle(triggerTransform.Position, new Vector2(100, 60), new Color(255, 200, 100, 255));
        _renderer.DrawText("TRIGGER", new Vector2(triggerTransform.Position.X - 30, triggerTransform.Position.Y - 8), 14, Color.White);

        // Target
        var targetTransform = World.GetComponent<TransformComponent>(_target);
        var targetColor = _targetActivationTimer > 0 
            ? new Color(100, 255, 100, 255) 
            : new Color(150, 150, 150, 255);
        _renderer.DrawCircle(targetTransform.Position, 40, targetColor);
        _renderer.DrawText("TARGET", new Vector2(targetTransform.Position.X - 30, targetTransform.Position.Y - 8), 14, Color.White);
    }

    private void RenderUI()
    {
        var layout = new TextLayoutHelper(_renderer, startX: 500, startY: 10, defaultLineHeight: 20);
        var infoColor = new Color(200, 200, 200, 255);
        var dimColor = new Color(150, 150, 150, 255);

        layout.DrawText("Event System Demo", 20, Color.White)
              .AddSpacing(5)
              .DrawText("Demonstrates decoupled event-driven communication", 12, dimColor)
              .AddSpacing(10)
              .DrawText("How it works:", 16, Color.White)
              .DrawText("1. Press [1] or [2] to trigger a button", 12, dimColor)
              .DrawText("2. Button publishes ButtonPressedEvent", 12, dimColor)
              .DrawText("3. Trigger receives event, queues TriggerEnteredEvent", 12, dimColor)
              .DrawText("4. Target receives event, queues TargetActivatedEvent", 12, dimColor)
              .DrawText("5. Target lights up green for 2 seconds", 12, dimColor);

        // Statistics
        layout.AddSpacing(10)
              .DrawText("Statistics:", 16, Color.White)
              .DrawKeyValue("Published", _totalEventsPublished.ToString(), 14, dimColor, infoColor)
              .DrawKeyValue("Handled", _totalEventsHandled.ToString(), 14, dimColor, infoColor)
              .DrawKeyValue("Queued", _eventBus.QueuedEventCount.ToString(), 14, dimColor, infoColor);

        // Event Log
        layout.AddSpacing(10)
              .DrawText("Event Log (last 5):", 16, Color.White);

        foreach (var logEntry in _eventLog)
        {
            layout.DrawText(logEntry, 12, new Color(180, 180, 180, 255));
        }

        // Controls
        layout.SetY(520)
              .DrawText("Controls:", 16, Color.White)
              .DrawText("[1] Press Button 1 | [2] Press Button 2", 14, dimColor)
              .DrawText("[SPACE] Enter Trigger | [C] Clear Log", 14, dimColor)
              .DrawText("[ESC] Menu", 14, dimColor);
    }

    // Custom Events
    private sealed class ButtonPressedEvent : IEvent
    {
        public int ButtonNumber { get; set; }
        public bool IsHandled { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    private sealed class TriggerEnteredEvent : IEvent
    {
        public bool IsHandled { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    private sealed class TargetActivatedEvent : IEvent
    {
        public bool IsHandled { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
