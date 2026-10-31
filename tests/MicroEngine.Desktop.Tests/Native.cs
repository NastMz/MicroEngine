namespace MicroEngine.Desktop.Tests;

/// <summary>
/// Native tests need a real display or sound device. Where one is missing they are reported as skipped; with
/// MICROENGINE_NATIVE_TESTS=1 (the native CI jobs) a missing prerequisite fails instead of hiding a broken setup.
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
