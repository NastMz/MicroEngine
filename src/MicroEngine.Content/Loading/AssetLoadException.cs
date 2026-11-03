// The type keeps the namespace it had when it lived in MicroEngine.dll, so code that catches it compiles unchanged;
// MicroEngine.dll forwards it here for code compiled against an earlier version.
namespace MicroEngine.Core;

/// <summary>A content file that could not be loaded: missing, unreadable, in an unsupported format, or named with the wrong letter case.</summary>
public sealed class AssetLoadException : Exception
{
    /// <summary>The path the game asked for, relative to the content source.</summary>
    public string AssetPath { get; }
    /// <summary>What the game asked for it as ("texture", "atlas", "font", "sound"...): the loader's kind.</summary>
    public string Kind { get; }

    /// <summary>Records which asset failed and why.</summary>
    public AssetLoadException(string assetPath, string kind, string message, Exception? inner = null)
        : base($"Could not load {kind} '{assetPath}': {message}", inner) => (AssetPath, Kind) = (assetPath, kind);
}
