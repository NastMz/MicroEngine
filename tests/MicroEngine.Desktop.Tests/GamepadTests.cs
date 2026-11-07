using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.Sdl;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// Gamepads through SDL's own virtual devices, which travel the same events as physical ones: connection, the family
/// their buttons are named after, presses, the next pad taking over when one is unplugged, and the last unplugged.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed unsafe class GamepadTests
{
    [TestInitialize]
    public void NotOnMacOS()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
    }

    private static SDL_JoystickID Attach(string name, ushort vendor, ushort product)
    {
        var desc = new SDL_VirtualJoystickDesc
        {
            version = (uint)sizeof(SDL_VirtualJoystickDesc),
            type = (ushort)SDL_JoystickType.SDL_JOYSTICK_TYPE_GAMEPAD,
            vendor_id = vendor,
            product_id = product,
            naxes = (ushort)SDL_GamepadAxis.SDL_GAMEPAD_AXIS_COUNT,
            nbuttons = (ushort)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_COUNT,
            // Which gamepad buttons and axes the device has: SDL builds its gamepad mapping from these.
            button_mask = (1u << (int)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_COUNT) - 1,
            axis_mask = (1u << (int)SDL_GamepadAxis.SDL_GAMEPAD_AXIS_COUNT) - 1,
        };
        var utf8 = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* text = utf8)
        {
            desc.name = text;
            var id = SDL_AttachVirtualJoystick(&desc);
            Assert.AreNotEqual(0u, (uint)id, $"SDL could not attach a virtual pad: {SDL_GetError()}");
            return id;
        }
    }

    private static void Frame(SdlPlatform platform)
    {
        platform.PumpEvents(MicroEngine.NativeChecks.IgnoredEvents.Instance);
        platform.EndFrame(); // a hidden window still needs a frame for its events to settle
    }

    [TestMethod]
    public void Pads_connect_are_recognised_press_and_hand_over_when_unplugged()
    {
        // The hidden window never has focus, and SDL keeps pad input from unfocused windows.
        SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
        SdlPlatform platform;
        try { platform = new SdlPlatform(new PlatformRequest(new GameSettings("pads", 8, 8)) { Hidden = true, WaitForDisplay = false, Mute = true }); }
        catch (InvalidOperationException e) { Native.Require(false, $"SDL cannot open a window here: {e.Message}"); return; }
        using var owned = platform;
        platform.BeginFrame();
        Frame(platform);
        var input = platform.Input;
        // A physical pad plugged into this machine would take part in the hand-over this test checks.
        if (input.PadConnected) Assert.Inconclusive("a gamepad is already connected to this machine; unplug it to run this test");

        // A DualSense (Sony's vendor and product ids) is a PlayStation pad.
        var sony = Attach("Virtual DualSense", 0x054C, 0x0CE6);
        for (int i = 0; i < 3; i++) { platform.BeginFrame(); Frame(platform); }
        Assert.IsTrue(input.PadConnected, "connected");
        Assert.AreEqual(GamepadStyle.PlayStation, input.PadStyle, "recognised as PlayStation");

        var joystick = SDL_OpenJoystick(sony);
        Assert.IsTrue(SDL_SetJoystickVirtualButton(joystick, (int)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH, true));
        SDL_UpdateJoysticks();
        platform.BeginFrame(); Frame(platform);
        Assert.IsTrue(input.IsPadDown(PadButton.A), "the south button is A");
        Assert.IsTrue(SDL_SetJoystickVirtualButton(joystick, (int)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH, false));
        SDL_UpdateJoysticks();
        platform.BeginFrame(); Frame(platform);
        Assert.IsFalse(input.IsPadDown(PadButton.A), "released");
        SDL_CloseJoystick(joystick);

        // An Xbox pad plugged in beside it, then the first one unplugged: the second takes over.
        var xbox = Attach("Virtual Xbox", 0x045E, 0x0B12);
        for (int i = 0; i < 3; i++) { platform.BeginFrame(); Frame(platform); }
        Assert.AreEqual(GamepadStyle.PlayStation, input.PadStyle, "the first pad stays in use");
        Assert.IsTrue(SDL_DetachVirtualJoystick(sony));
        for (int i = 0; i < 3; i++) { platform.BeginFrame(); Frame(platform); }
        Assert.IsTrue(input.PadConnected, "the other pad takes over");
        Assert.AreEqual(GamepadStyle.Xbox, input.PadStyle);

        Assert.IsTrue(SDL_DetachVirtualJoystick(xbox));
        for (int i = 0; i < 3; i++) { platform.BeginFrame(); Frame(platform); }
        Assert.IsFalse(input.PadConnected, "none left");
    }
}
