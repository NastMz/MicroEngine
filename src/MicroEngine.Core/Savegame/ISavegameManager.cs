namespace MicroEngine.Core.Savegame;

/// <summary>
/// Interface for managing savegame files.
/// Provides save, load, delete, and list operations.
/// </summary>
public interface ISavegameManager
{
    /// <summary>
    /// Saves game data to a file.
    /// </summary>
    /// <typeparam name="T">The type of game data to save.</typeparam>
    /// <param name="data">The game data to save.</param>
    /// <param name="fileName">The name of the save file.</param>
    /// <param name="saveName">Optional user-friendly save name.</param>
    /// <returns>A SaveResult indicating success or failure.</returns>
    SaveResult Save<T>(T data, string fileName, string? saveName = null);

    /// <summary>
    /// Loads game data from a file.
    /// </summary>
    /// <typeparam name="T">The type of game data to load.</typeparam>
    /// <param name="fileName">The name of the save file.</param>
    /// <returns>A LoadResult with the loaded data or error message.</returns>
    LoadResult<T> Load<T>(string fileName);

    /// <summary>
    /// Deletes a save file.
    /// </summary>
    /// <param name="fileName">The name of the save file to delete.</param>
    /// <returns>True if the file was deleted successfully.</returns>
    bool Delete(string fileName);

    /// <summary>
    /// Gets the full path to a save file.
    /// </summary>
    /// <param name="fileName">The name of the save file.</param>
    /// <returns>The full path to the save file.</returns>
    string GetSavePath(string fileName);

    /// <summary>
    /// Checks if a save file exists.
    /// </summary>
    /// <param name="fileName">The name of the save file.</param>
    /// <returns>True if the save file exists.</returns>
    bool Exists(string fileName);

    /// <summary>
    /// Lists all save files in the save directory.
    /// </summary>
    /// <returns>An array of save file names.</returns>
    string[] ListSaves();

    /// <summary>
    /// Gets metadata for a save file without loading the full data.
    /// </summary>
    /// <param name="fileName">The name of the save file.</param>
    /// <returns>The save metadata, or null if the file doesn't exist or is invalid.</returns>
    SaveMetadata? GetMetadata(string fileName);
}
