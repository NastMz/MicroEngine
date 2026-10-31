namespace MicroEngine.Raylib.Tests;

/// <summary>
/// Native tests need a real display or sound server. Where one is missing they are reported as skipped (a developer
/// machine, the plain test matrix); the native CI job sets MICROENGINE_NATIVE_TESTS=1, so a missing prerequisite
/// fails there instead of hiding a broken setup.
/// </summary>
internal static class Native
{
    public static bool Required => Environment.GetEnvironmentVariable("MICROENGINE_NATIVE_TESTS") == "1";

    public static void Require(bool available, string missing)
    {
        if (available) return;
        if (Required) Assert.Fail($"{missing} (required: MICROENGINE_NATIVE_TESTS=1)");
        Assert.Inconclusive(missing);
    }
}
