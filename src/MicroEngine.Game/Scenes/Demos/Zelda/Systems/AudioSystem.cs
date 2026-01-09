using MicroEngine.Core.Audio;
using MicroEngine.Core.ECS;
using MicroEngine.Core.Events;
using System;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

/// <summary>
/// System responsible for playing audio effects triggered by events.
/// </summary>
public sealed class AudioSystem : ISystem, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly ISoundPlayer _soundPlayer;
    private bool _isSubscribed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioSystem"/> class.
    /// </summary>
    public AudioSystem(EventBus eventBus, ISoundPlayer soundPlayer)
    {
        _eventBus = eventBus;
        _soundPlayer = soundPlayer;
    }

    /// <summary>
    /// Initializes the audio system and subscribes to play sound events.
    /// </summary>
    public void Initialize(World world)
    {
        if (!_isSubscribed)
        {
            _eventBus.Subscribe<PlaySoundEvent>(OnPlaySound);
            _isSubscribed = true;
        }
    }

    /// <summary>
    /// Updates the audio system.
    /// </summary>
    public void Update(World world, float deltaTime)
    {
        // No per-frame update needed for event-based audio
    }

    private void OnPlaySound(PlaySoundEvent e)
    {
        if (e.Clip != null)
        {
            _soundPlayer.PlaySound(e.Clip);
        }
    }

    /// <summary>
    /// Unsubscribes from events when disposed.
    /// </summary>
    public void Dispose()
    {
        if (_isSubscribed)
        {
            _eventBus.Unsubscribe<PlaySoundEvent>(OnPlaySound);
            _isSubscribed = false;
        }
    }
}
