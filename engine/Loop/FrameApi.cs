namespace MicroEngine.Loop;

/// <summary>Checks explicitly declared frame API preconditions without provider-specific privileges.</summary>
public static class FrameApi
{
    /// <summary>Returns when valid; otherwise reports intentional invalid API use, not an operational failure.</summary>
    /// <remarks>The provider owns the predicate. This guard is not a security or ownership boundary.</remarks>
    public static void Require(bool condition, string? message = null)
    {
        if (!condition)
            throw new FrameApiException(string.IsNullOrWhiteSpace(message) ? "Invalid frame API use." : message);
    }
}
