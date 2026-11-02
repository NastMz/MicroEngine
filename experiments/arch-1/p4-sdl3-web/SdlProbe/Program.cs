using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using SDL;
using static SDL.SDL3;

[assembly: SupportedOSPlatform("browser")]

// Main only brings the runtime up; the page calls Probe.Init inside a user gesture, then Probe.Frame once per
// requestAnimationFrame (the browser owns the loop, as in the raylib host).
Console.WriteLine("SdlProbe: ready");

/// <summary>The smallest SDL3 "game": window on the page canvas, SDL_Renderer drawing, keyboard/mouse/touch events, audio.</summary>
public static unsafe partial class Probe
{
    private static SDL_Window* _window;
    private static SDL_Renderer* _renderer;
    private static SDL_Texture* _target;
    private static SDL_AudioStream* _stream;
    private static SDL_AudioDeviceID _device;
    private static long _frames;
    private static double _x;
    private static int _keys, _mouse, _fingers;
    private static string _lastKey = "-";
    private static double _phase;
    private static bool _tone;

    /// <summary>Opens SDL video and audio; returns "" or the error, then a line of facts after "|".</summary>
    [JSExport]
    public static string Init()
    {
        SDL_SetHint(SDL_HINT_TOUCH_MOUSE_EVENTS, "0");
        SDL_SetHint(SDL_HINT_MOUSE_TOUCH_EVENTS, "0");
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_AUDIO | SDL_InitFlags.SDL_INIT_GAMEPAD))
            return "SDL_Init: " + SDL_GetError();
        // SDL3-CS declares the flags as an enum with a 64-bit underlying type; the Mono interpreter on wasm32 calls such
        // a P/Invoke with a 32-bit slot ("function signature mismatch" in do_icall). A plain ulong parameter works.
        fixed (byte* title = "SdlProbe"u8)
            _window = CreateWindow(title, 960, 540, (ulong)SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
        if (_window == null) return "SDL_CreateWindow: " + SDL_GetError();
        _renderer = SDL_CreateRenderer(_window, (byte*)null);
        if (_renderer == null) return "SDL_CreateRenderer: " + SDL_GetError();
        _target = SDL_CreateTexture(_renderer, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, 320, 180);
        if (_target == null) return "SDL_CreateTexture: " + SDL_GetError();
        SDL_SetTextureScaleMode(_target, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
        var spec = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = 2, freq = 48000 };
        _device = SDL_OpenAudioDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec);
        string audio = "none";
        if (_device != 0)
        {
            _stream = SDL_CreateAudioStream(&spec, &spec);
            if (_stream != null && SDL_BindAudioStream(_device, _stream)) audio = SDL_GetCurrentAudioDriver() ?? "?";
        }
        int v = SDL_GetVersion();
        return $"|sdl={v / 1000000}.{v / 1000 % 1000}.{v % 1000} video={SDL_GetCurrentVideoDriver()} renderer={SDL_GetRendererName(_renderer)} audio={audio}";
    }

    [System.Runtime.InteropServices.DllImport("SDL3", EntryPoint = "SDL_CreateWindow")]
    private static extern SDL_Window* CreateWindow(byte* title, int w, int h, ulong flags);

    /// <summary>One frame: events, draw, audio top-up. Returns false after SDL_EVENT_QUIT.</summary>
    [JSExport]
    public static bool Frame()
    {
        SDL_Event e;
        while (SDL_PollEvent(&e))
        {
            switch ((SDL_EventType)e.type)
            {
                case SDL_EventType.SDL_EVENT_QUIT: return false;
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    _keys++;
                    _lastKey = e.key.scancode.ToString();
                    if (e.key.scancode == SDL_Scancode.SDL_SCANCODE_A) _tone = !_tone;
                    break;
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN: _mouse++; break;
                case SDL_EventType.SDL_EVENT_FINGER_DOWN: _fingers++; break;
            }
        }
        _frames++;
        _x = (_x + 1) % 320;
        SDL_SetRenderTarget(_renderer, _target);
        SDL_SetRenderDrawColor(_renderer, 20, 24, 40, 255);
        SDL_RenderClear(_renderer);
        SDL_SetRenderDrawColor(_renderer, 240, 180, 40, 255);
        var r = new SDL_FRect { x = (float)_x, y = 70, w = 24, h = 24 };
        SDL_RenderFillRect(_renderer, &r);
        // Textured geometry, the path MicroEngine's sprite batcher uses.
        var verts = stackalloc SDL_Vertex[3];
        verts[0] = new SDL_Vertex { position = new SDL_FPoint { x = 20, y = 160 }, color = new SDL_FColor { r = 1, g = 0, b = 0, a = 1 } };
        verts[1] = new SDL_Vertex { position = new SDL_FPoint { x = 60, y = 110 }, color = new SDL_FColor { r = 0, g = 1, b = 0, a = 1 } };
        verts[2] = new SDL_Vertex { position = new SDL_FPoint { x = 100, y = 160 }, color = new SDL_FColor { r = 0, g = 0, b = 1, a = 1 } };
        SDL_RenderGeometry(_renderer, null, verts, 3, null, 0);
        SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
        SDL_RenderDebugText(_renderer, 4, 4, $"frame {_frames}");
        SDL_RenderDebugText(_renderer, 4, 16, $"keys {_keys} last {_lastKey}");
        SDL_RenderDebugText(_renderer, 4, 28, $"mouse {_mouse} fingers {_fingers} tone {(_tone ? "on" : "off")}");
        SDL_SetRenderTarget(_renderer, null);
        int w, h;
        SDL_GetCurrentRenderOutputSize(_renderer, &w, &h);
        int scale = Math.Max(1, Math.Min(w / 320, h / 180));
        var dst = new SDL_FRect { x = (w - 320 * scale) / 2, y = (h - 180 * scale) / 2, w = 320 * scale, h = 180 * scale };
        SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        SDL_RenderClear(_renderer);
        SDL_RenderTexture(_renderer, _target, null, &dst);
        SDL_RenderPresent(_renderer);
        TopUpAudio();
        return true;
    }

    private static void TopUpAudio()
    {
        if (_stream == null) return;
        // Keep about 60 ms queued (2 channels, float): a 440 Hz tone while toggled on, silence otherwise.
        const int Rate = 48000, Target = Rate * 2 * 4 * 60 / 1000;
        int queued = SDL_GetAudioStreamQueued(_stream);
        if (queued >= Target) return;
        int frames = (Target - queued) / 8;
        var buffer = new float[frames * 2];
        for (int i = 0; i < frames; i++)
        {
            float s = _tone ? (float)(0.2 * Math.Sin(_phase)) : 0f;
            _phase += 2 * Math.PI * 440 / Rate;
            buffer[2 * i] = buffer[2 * i + 1] = s;
        }
        fixed (float* p = buffer) SDL_PutAudioStreamData(_stream, (IntPtr)p, buffer.Length * 4);
    }

    /// <summary>Diagnostics for the page and tests: "frames keys mouse fingers queuedBytes tone".</summary>
    [JSExport]
    public static string Stats() => $"{_frames} {_keys} {_mouse} {_fingers} {(_stream == null ? -1 : SDL_GetAudioStreamQueued(_stream))} {(_tone ? 1 : 0)} {_lastKey}";
}
