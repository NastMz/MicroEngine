using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>How a sound's volume falls between an emitter's <see cref="SoundEmitter.MinDistance"/> and its <see cref="SoundEmitter.MaxDistance"/>.</summary>
public enum SoundRolloff
{
    /// <summary>A straight line from full volume to silence: predictable, and what a small level wants.</summary>
    Linear,
    /// <summary>Falls fast near the source and slowly far from it, as sound does in the open (volume in proportion to one over the distance), brought to silence at the maximum distance.</summary>
    Inverse,
    /// <summary>An S-shaped curve: nearly full volume for a while, a fall in the middle, a gentle arrival at silence.</summary>
    Smooth,
}

/// <summary>
/// A sound with a place in a scene's world (<see cref="SceneAudio.Emit(string, EntityId, bool, float)"/>): each step
/// its volume follows its distance to the listener and its pan its side of the listener. Its properties can be
/// changed while it plays.
/// </summary>
public sealed class SoundEmitter
{
    internal SoundEmitter(SceneAudio owner, string? sound, AudioClip? clip, bool loop)
    {
        (Owner, Sound, Clip, Loop) = (owner, sound, clip, loop);
        (MinDistance, MaxDistance, Rolloff, Bus) = (owner.MinDistance, owner.MaxDistance, owner.Rolloff, owner.Bus);
    }

    internal SceneAudio? Owner;
    internal VoiceHandle Voice;
    internal float Age;
    internal float SentVolume = -1, SentPan = -1, SentPitch = -1;
    internal float LastDistance = -1;
    internal bool Started;
    // A sound that plays once: its length in seconds of the sound, how much of it has gone by on the scene's clock,
    // and whether it is to be given a voice the next time it is in range.
    internal double Length, Progress;
    internal bool Wanted = true;

    /// <summary>The registered sound it plays, or null when it plays a clip.</summary>
    public string? Sound { get; }
    /// <summary>The clip it plays, or null when it plays a registered sound.</summary>
    public AudioClip? Clip { get; }
    /// <summary>Whether the sound repeats until the emitter is stopped; a sound that does not is over when it ends.</summary>
    public bool Loop { get; }
    /// <summary>The entity it follows, or <see cref="EntityId.None"/> for an emitter at a fixed <see cref="Position"/>. When the entity is destroyed the emitter stops.</summary>
    public EntityId Entity { get; internal set; }
    /// <summary>Where it is in the world, in world units. Set it to move an emitter that follows no entity; one that follows an entity reads the entity's place every step.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Its volume at <see cref="MinDistance"/> or closer, 0 to 1 (1 by default). A value that is not a number counts as 1.</summary>
    public float Volume { get; set; } = 1;
    /// <summary>Its pitch: the playback speed factor, 1 for the sound's own (the scene's Doppler shift, if any, multiplies it). A value that is not a number counts as 1.</summary>
    public float Pitch { get; set; } = 1;
    /// <summary>Within this distance of the listener it plays at its full <see cref="Volume"/> (world units; the scene's <see cref="SceneAudio.MinDistance"/> when the emitter was made).</summary>
    public float MinDistance { get; set; }
    /// <summary>From this distance on it is silent, and holds no voice (world units; the scene's <see cref="SceneAudio.MaxDistance"/> when the emitter was made).</summary>
    public float MaxDistance { get; set; }
    /// <summary>The shape of the fall between the two distances.</summary>
    public SoundRolloff Rolloff { get; set; }
    /// <summary>
    /// A curve of the game's own instead of <see cref="Rolloff"/>: given how far the listener is between
    /// <see cref="MinDistance"/> (0) and <see cref="MaxDistance"/> (1), it returns the gain (clamped to 0..1). Null
    /// (the default) for <see cref="Rolloff"/>.
    /// </summary>
    public Func<float, float>? Curve { get; set; }
    /// <summary>The bus it plays through, read when its sound starts.</summary>
    public string Bus { get; set; }
    /// <summary>The voice policy's rank of its sound, read when its sound starts (0 by default).</summary>
    public int Priority { get; set; }

    /// <summary>The gain the last step gave it for its distance, 0 to 1, before <see cref="Volume"/>; 0 out of range.</summary>
    public float Gain { get; internal set; }
    /// <summary>The stereo position the last step gave it: 0 (left) to 1 (right).</summary>
    public float Pan { get; internal set; } = 0.5f;
    /// <summary>The pitch the last step gave it, Doppler shift included.</summary>
    public float PlayedPitch { get; internal set; } = 1;
    /// <summary>
    /// Whether the emitter is still in its scene: false once stopped, once its entity was destroyed, once the scene
    /// left and, for a sound that plays once, once the sound's length has gone by on the scene's clock (at its
    /// pitch), whether or not it was heard. It is the same with and without a device that plays.
    /// </summary>
    public bool IsActive => Owner is not null;
    /// <summary>Whether a voice of the mixer is playing its sound now (false out of range, on a device without sound, and when every voice was busy with sounds of higher priority).</summary>
    public bool IsAudible => Owner is { } owner && owner.Scene.Audio.IsPlaying(Voice);

    /// <summary>Stops the sound, fading it out over <paramref name="fadeSeconds"/>, and takes the emitter out of the scene.</summary>
    public void Stop(float fadeSeconds = 0.05f) => Owner?.Remove(this, fadeSeconds);
}

/// <summary>
/// A scene's sounds in space (<see cref="Scene.Sounds"/>): a listener and emitters in the scene's world units. After
/// every <see cref="Scene.Update"/> each emitter's volume follows its distance to the listener and its pan the side
/// it is on, moved there over the step so nothing clicks; an emitter that follows an entity goes where the entity
/// goes and stops when it is destroyed; every emitter stops when the scene leaves.
/// </summary>
/// <remarks>
/// <para>The listener is, in this order: the entity given to <see cref="Follow"/>, the point set in
/// <see cref="Listener"/>, the centre of the scene's <see cref="Scene.WorldCamera"/>, or the world's origin.</para>
/// <para>An emitter out of range holds no voice of the mixer: a level can have hundreds of emitters, and only those
/// within earshot count against the voice limit. A looping emitter that comes into range starts where it would be had
/// it played all along.</para>
/// <para>Emitters are sound, not state: nothing here is part of the scene's state hash, and nothing a game decides
/// should depend on whether an emitter is audible, which differs between a device that plays and a silent one.</para>
/// </remarks>
/// <example>
/// <code>
/// public override void Enter()
/// {
///     Sounds.Follow(_hero);                               // the listener is the hero
///     Sounds.Emit("torch", torch, loop: true);            // this entity makes this sound
/// }
///
/// // later, in Update
/// Sounds.Emit("explosion", barrelPosition);               // a sound at a place, once
/// </code>
/// </example>
public sealed class SceneAudio : SceneSystem
{
    private readonly List<SoundEmitter> _emitters = [];
    private EntityId _listenerEntity;
    private Vector2? _listener;
    private float _minDistance = 16, _maxDistance = 320, _panDistance, _doppler, _speedOfSound = 3400;

    /// <summary>Runs after the scene's other systems (physics, which moves the entities emitters follow).</summary>
    public override int Order => 1000;

    /// <summary>
    /// The listener's place in the world. Setting it fixes the listener there (and ends <see cref="Follow"/>); set it
    /// to null to go back to the centre of the scene's camera. Reading it gives where the listener is now.
    /// </summary>
    public Vector2? Listener
    {
        get => IsAttached ? ListenerPosition() : _listener;
        set
        {
            _listener = value is { } v && float.IsFinite(v.X) && float.IsFinite(v.Y) ? v : null;
            _listenerEntity = default;
        }
    }

    /// <summary>Makes <paramref name="entity"/> the listener from now on: the hero, usually. When it is destroyed the listener stays where it was.</summary>
    public void Follow(EntityId entity) => _listenerEntity = entity;

    /// <summary>The distance within which new emitters play at full volume (16 by default; never negative).</summary>
    public float MinDistance { get => _minDistance; set => _minDistance = float.IsFinite(value) ? Math.Max(0, value) : _minDistance; }
    /// <summary>The distance from which new emitters are silent (320 by default).</summary>
    public float MaxDistance { get => _maxDistance; set => _maxDistance = float.IsFinite(value) ? Math.Max(0, value) : _maxDistance; }
    /// <summary>The fall of new emitters between the two distances (<see cref="SoundRolloff.Linear"/> by default).</summary>
    public SoundRolloff Rolloff { get; set; }
    /// <summary>The bus of new emitters ("sfx" by default).</summary>
    public string Bus { get; set; } = AudioEngine.SfxBus;
    /// <summary>
    /// The horizontal distance from the listener at which a sound is entirely on one side; closer, it is in between.
    /// 0 (the default) for each emitter's own <see cref="SoundEmitter.MaxDistance"/>. Half the width of the view is a
    /// good value for a game seen from the side: a sound at the edge of the screen then plays from that speaker.
    /// </summary>
    public float PanDistance { get => _panDistance; set => _panDistance = float.IsFinite(value) ? Math.Max(0, value) : _panDistance; }
    /// <summary>
    /// How much of the Doppler shift is applied: 0 (the default) for none, 1 for the shift of <see cref="SpeedOfSound"/>,
    /// more to exaggerate it. A sound coming closer rises in pitch and one going away falls, by the speed at which its
    /// distance to the listener changes; the shift is kept between half and twice the pitch. Streamed music keeps its
    /// pitch.
    /// </summary>
    public float Doppler { get => _doppler; set => _doppler = float.IsFinite(value) ? Math.Clamp(value, 0, 10) : _doppler; }
    /// <summary>The speed of sound for the Doppler shift, in world units per second (3400 by default: 340 m/s at ten units per metre).</summary>
    public float SpeedOfSound { get => _speedOfSound; set => _speedOfSound = float.IsFinite(value) && value > 0 ? value : _speedOfSound; }
    /// <summary>Seconds over which every emitter fades out when the scene leaves (0.1 by default).</summary>
    public float StopFade { get; set; } = 0.1f;

    /// <summary>The emitters in the scene, in the order they were made.</summary>
    public IReadOnlyList<SoundEmitter> Emitters => _emitters;

    /// <summary>
    /// Makes <paramref name="entity"/> the source of the registered sound <paramref name="sound"/>: it plays from where
    /// the entity is, follows it, and stops when the entity is destroyed.
    /// </summary>
    /// <param name="sound">A sound registered in <c>Audio</c> (<see cref="AudioMixer.AddSound(string, AudioClip)"/>).</param>
    /// <param name="entity">An entity of the scene's world (<see cref="Scene.Objects"/>).</param>
    /// <param name="loop">Whether it repeats until stopped (a torch, an engine) or plays once (a shout).</param>
    /// <param name="volume">Its volume at close range, 0 to 1.</param>
    /// <returns>The emitter, to change or stop it.</returns>
    /// <exception cref="KeyNotFoundException">No sound is registered under that name.</exception>
    /// <exception cref="ArgumentException">The entity is not alive in the scene's world.</exception>
    public SoundEmitter Emit(string sound, EntityId entity, bool loop = false, float volume = 1) => Add(Named(sound), null, entity, default, loop, volume);

    /// <summary>Plays the registered sound <paramref name="sound"/> from a place in the world.</summary>
    /// <returns>The emitter, to move, change or stop it.</returns>
    /// <exception cref="KeyNotFoundException">No sound is registered under that name.</exception>
    public SoundEmitter Emit(string sound, Vector2 position, bool loop = false, float volume = 1) => Add(Named(sound), null, default, position, loop, volume);

    /// <summary>Makes <paramref name="entity"/> the source of <paramref name="clip"/> (a clip held by the game or by an asset scope, not registered under a name).</summary>
    /// <exception cref="ArgumentException">The entity is not alive in the scene's world.</exception>
    public SoundEmitter Emit(AudioClip clip, EntityId entity, bool loop = false, float volume = 1) => Add(null, clip ?? throw new ArgumentNullException(nameof(clip)), entity, default, loop, volume);

    /// <summary>Plays <paramref name="clip"/> from a place in the world.</summary>
    public SoundEmitter Emit(AudioClip clip, Vector2 position, bool loop = false, float volume = 1) => Add(null, clip ?? throw new ArgumentNullException(nameof(clip)), default, position, loop, volume);

    private string Named(string sound)
    {
        ArgumentNullException.ThrowIfNull(sound);
        if (!Scene.Audio.HasSound(sound)) throw new KeyNotFoundException($"Sound '{sound}' is not registered.");
        return sound;
    }

    private SoundEmitter Add(string? sound, AudioClip? clip, EntityId entity, Vector2 position, bool loop, float volume)
    {
        var emitter = new SoundEmitter(this, sound, clip, loop) { Entity = entity, Position = position, Volume = volume };
        emitter.Length = clip?.Duration ?? Scene.Audio.SoundDuration(sound!);
        if (!entity.IsNone)
        {
            if (!TryPlace(entity, out var at)) throw new ArgumentException($"{entity} is not alive in the scene's world.", nameof(entity));
            emitter.Position = at;
        }
        _emitters.Add(emitter);
        // Its sound starts with the step that made it, not one later.
        Place(emitter, ListenerPosition(), 0);
        return emitter;
    }

    /// <summary>Stops every emitter, fading each out over <paramref name="fadeSeconds"/>.</summary>
    public void StopAll(float fadeSeconds = 0.05f)
    {
        for (int i = _emitters.Count - 1; i >= 0; i--) Remove(_emitters[i], fadeSeconds);
    }

    internal void Remove(SoundEmitter emitter, float fadeSeconds)
    {
        if (!_emitters.Remove(emitter)) return;
        if (IsAttached) Scene.Audio.Stop(emitter.Voice, float.IsNaN(fadeSeconds) ? 0 : Math.Max(0, fadeSeconds));
        emitter.Voice = VoiceHandle.None;
        emitter.Gain = 0;
        emitter.Owner = null;
    }

    private bool TryPlace(EntityId entity, out Vector2 position)
    {
        position = default;
        if (!Scene.HasWorld || !Scene.Objects.IsAlive(entity)) return false;
        var world = Scene.Objects.WorldMatrix(entity).Translation;
        position = new Vector2(world.X, world.Y);
        return true;
    }

    private Vector2 ListenerPosition()
    {
        if (!_listenerEntity.IsNone)
        {
            if (TryPlace(_listenerEntity, out var at)) _listener = at;
            else _listenerEntity = default;
        }
        return _listener ?? Scene.WorldCamera?.Center ?? default;
    }

    /// <summary>The gain of a sound <paramref name="distance"/> away, between the two distances, by the curve.</summary>
    internal static float Falloff(float distance, float min, float max, SoundRolloff rolloff, Func<float, float>? curve)
    {
        if (!(distance < max)) return 0;
        if (distance <= min) return 1;
        float t = (distance - min) / (max - min);
        float gain = curve is not null ? curve(t) : rolloff switch
        {
            // One over the distance, measured in minimum distances, less what it would still be at the maximum.
            SoundRolloff.Inverse when min > 0 => (min / distance - min / max) / (1 - min / max),
            SoundRolloff.Inverse => (1 - t) * (1 - t),
            SoundRolloff.Smooth => 1 - t * t * (3 - 2 * t),
            _ => 1 - t,
        };
        return float.IsNaN(gain) ? 0 : Math.Clamp(gain, 0, 1);
    }

    /// <inheritdoc/>
    protected override void LateUpdate(float dt)
    {
        if (_emitters.Count == 0) return;
        var listener = ListenerPosition();
        for (int i = _emitters.Count - 1; i >= 0; i--)
        {
            var emitter = _emitters[i];
            if (!emitter.Entity.IsNone)
            {
                if (TryPlace(emitter.Entity, out var at)) emitter.Position = at;
                else
                {
                    Remove(emitter, 0.05f);
                    continue;
                }
            }
            emitter.Age += dt;
            // A sound that plays once is over when its length has gone by on the scene's clock, heard or not.
            emitter.Progress += dt * emitter.PlayedPitch;
            if (!emitter.Loop && emitter.Progress >= emitter.Length)
            {
                Remove(emitter, 0.02f);
                continue;
            }
            Place(emitter, listener, dt);
        }
    }

    /// <summary>Gives an emitter the volume, pan and pitch of where it is, and the voice (or none) its distance asks for.</summary>
    private void Place(SoundEmitter emitter, Vector2 listener, float dt)
    {
        var audio = Scene.Audio;
        var offset = emitter.Position - listener;
        float distance = offset.Length();
        if (!float.IsFinite(distance)) distance = float.MaxValue;
        float min = float.IsFinite(emitter.MinDistance) ? Math.Max(0, emitter.MinDistance) : 0;
        float max = float.IsFinite(emitter.MaxDistance) ? emitter.MaxDistance : 0;
        emitter.Gain = Falloff(distance, min, max, emitter.Rolloff, emitter.Curve);
        float width = _panDistance > 0 ? _panDistance : max;
        emitter.Pan = width > 0 && float.IsFinite(offset.X) ? 0.5f + 0.5f * Math.Clamp(offset.X / width, -1, 1) : 0.5f;
        float pitch = float.IsFinite(emitter.Pitch) ? Math.Clamp(emitter.Pitch, 0.01f, 100f) : 1;
        if (_doppler > 0 && dt > 0 && emitter.LastDistance >= 0 && distance < float.MaxValue)
        {
            // The speed at which the sound's path gets longer: positive going away, which lowers the pitch.
            float away = (distance - emitter.LastDistance) / dt * _doppler;
            pitch *= Math.Clamp(_speedOfSound / Math.Max(_speedOfSound + away, _speedOfSound / 4), 0.5f, 2f);
        }
        emitter.LastDistance = distance;
        emitter.PlayedPitch = pitch;
        float volume = (float.IsNaN(emitter.Volume) ? 1 : Math.Clamp(emitter.Volume, 0, 1)) * emitter.Gain;

        bool playing = audio.IsPlaying(emitter.Voice);
        if (emitter.Gain <= 0)
        {
            // Out of range: no voice. It is given one again if it comes back in range.
            if (playing) audio.Stop(emitter.Voice, 0.05f);
            emitter.Voice = VoiceHandle.None;
            (emitter.Started, emitter.Wanted) = (true, true);
            return;
        }
        if (!playing)
        {
            if (emitter.Loop)
            {
                // A device without voices hears of the sound once, so a headless run can list it.
                if (emitter.Started && !audio.SupportsVoices) return;
                // A loop waits for a free voice instead of taking one from another sound: with more loops in range
                // than voices, they would otherwise take them from each other at every step.
                if (audio.SupportsVoices && audio.Statistics is var voices && voices.ActiveVoices >= voices.MaxVoices)
                {
                    emitter.Started = true;
                    return;
                }
            }
            // A sound that plays once asks for a voice when it happens and when it comes into range, not at every
            // step: one that lost its voice to a sound of higher priority stays silent.
            else if (!emitter.Wanted) return;
            emitter.Wanted = false;
            var parameters = new PlayParameters
            {
                Volume = volume, Pan = emitter.Pan, Pitch = pitch, Loop = emitter.Loop, Bus = emitter.Bus, Priority = emitter.Priority,
                FadeIn = emitter.Started ? 0.05f : 0,
                // What comes into range part of the way through is heard from there.
                StartAt = emitter.Loop ? 0 : emitter.Progress,
            };
            emitter.Voice = emitter.Clip is { } clip ? audio.PlayVoice(clip, parameters) : audio.PlayVoice(emitter.Sound!, parameters);
            // A loop that comes into range is where it would be had it been heard all along.
            if (emitter.Started && emitter.Loop && audio.Duration(emitter.Voice) is > 0 and var length) audio.Seek(emitter.Voice, emitter.Age % length);
            emitter.Started = true;
            (emitter.SentVolume, emitter.SentPan, emitter.SentPitch) = (volume, emitter.Pan, pitch);
            return;
        }
        // Over the length of a step, so the values glide from one step's to the next's.
        float seconds = Scene.EngineStep;
        if (volume != emitter.SentVolume || emitter.Pan != emitter.SentPan)
            audio.Set(emitter.Voice, volume: volume != emitter.SentVolume ? volume : null, pan: emitter.Pan != emitter.SentPan ? emitter.Pan : null, seconds: seconds);
        if (pitch != emitter.SentPitch) audio.Set(emitter.Voice, pitch: pitch, seconds: seconds);
        (emitter.SentVolume, emitter.SentPan, emitter.SentPitch) = (volume, emitter.Pan, pitch);
    }

    /// <inheritdoc/>
    protected override void Stop() => StopAll(float.IsNaN(StopFade) ? 0 : Math.Max(0, StopFade));
}
