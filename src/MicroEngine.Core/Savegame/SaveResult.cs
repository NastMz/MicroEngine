namespace MicroEngine.Core.Savegame;

/// <summary>
/// Represents the result of a save operation.
/// </summary>
public readonly struct SaveResult
{
    /// <summary>
    /// Gets a value indicating whether the save was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the save failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the path where the save was written (if successful).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Creates a successful save result.
    /// </summary>
    /// <param name="filePath">The path where the save was written.</param>
    /// <returns>A successful SaveResult.</returns>
    public static SaveResult Ok(string filePath) => new SaveResult 
    { 
        Success = true, 
        FilePath = filePath 
    };

    /// <summary>
    /// Creates a failed save result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed SaveResult.</returns>
    public static SaveResult Error(string errorMessage) => new SaveResult 
    { 
        Success = false, 
        ErrorMessage = errorMessage 
    };
}

/// <summary>
/// Represents the result of a load operation.
/// </summary>
/// <typeparam name="T">The type of data loaded.</typeparam>
public readonly struct LoadResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the load was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the load failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the loaded data (if successful).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful load result.
    /// </summary>
    /// <param name="data">The loaded data.</param>
    /// <returns>A successful LoadResult.</returns>
    public static LoadResult<T> Ok(T data) => new LoadResult<T> 
    { 
        Success = true, 
        Data = data 
    };

    /// <summary>
    /// Creates a failed load result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed LoadResult.</returns>
    public static LoadResult<T> Error(string errorMessage) => new LoadResult<T> 
    { 
        Success = false, 
        ErrorMessage = errorMessage 
    };
}
