using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

/// <summary>
/// Tests for resource loading failure scenarios and error handling.
/// </summary>
public sealed class ResourceLoadingFailureTests
{
    private sealed class TestResource : IResource
    {
        private static uint _nextId = 1;

        public ResourceId Id { get; init; }
        public string Path { get; init; } = string.Empty;
        public bool IsLoaded { get; init; }
        public long SizeInBytes { get; init; }
        public ResourceMetadata? Metadata { get; init; }
        public bool IsDisposed { get; private set; }

        public static TestResource Create(string path)
        {
            return new TestResource
            {
                Id = new ResourceId(_nextId++),
                Path = path,
                IsLoaded = true,
                SizeInBytes = 100,
                Metadata = null
            };
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class FailingLoader : IResourceLoader<TestResource>
    {
        public IReadOnlyList<string> SupportedExtensions => new[] { ".fail" };

        public TestResource Load(string path, ResourceMetadata? metadata = null)
        {
            throw new InvalidDataException($"Failed to load resource: {path}");
        }

        public void Unload(TestResource resource)
        {
            resource.Dispose();
        }

        public ResourceValidationResult Validate(string path)
        {
            return ResourceValidationResult.Success();
        }
    }

    private sealed class ValidationFailingLoader : IResourceLoader<TestResource>
    {
        public IReadOnlyList<string> SupportedExtensions => new[] { ".txt" };

        public TestResource Load(string path, ResourceMetadata? metadata = null)
        {
            return TestResource.Create(path);
        }

        public void Unload(TestResource resource)
        {
            resource.Dispose();
        }

        public ResourceValidationResult Validate(string path)
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.InvalidFileData,
                "Simulated validation failure"
            );
        }
    }

    [Fact]
    public void Load_WithInvalidPath_ThrowsArgumentException()
    {
        var loader = new FailingLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<TestResource>(loader, logger);

        var exception = Assert.Throws<ArgumentException>(() =>
            cache.Load("", validateFirst: false));

        Assert.Contains("path", exception.Message);
    }

    [Fact]
    public void Load_WithNullPath_ThrowsArgumentNullException()
    {
        var loader = new FailingLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<TestResource>(loader, logger);

        var exception = Assert.Throws<ArgumentNullException>(() =>
            cache.Load(null!, validateFirst: false));

        Assert.Contains("path", exception.Message);
    }

    [Fact]
    public void Load_WhenLoaderThrows_PropagatesException()
    {
        var loader = new FailingLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<TestResource>(loader, logger);

        var exception = Assert.Throws<InvalidDataException>(() =>
            cache.Load("test.fail", validateFirst: false));

        Assert.Contains("Failed to load resource", exception.Message);
    }

    [Fact]
    public void Load_WhenValidationFails_ThrowsInvalidOperationException()
    {
        using var tempDir = new TempDirectory();
        var filePath = System.IO.Path.Combine(tempDir.Path, "test.txt");
        File.WriteAllText(filePath, "content");

        var loader = new ValidationFailingLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<TestResource>(loader, logger);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            cache.Load(filePath, validateFirst: true));

        Assert.Contains("Simulated validation failure", exception.Message);
    }

    [Fact]
    public void Load_AfterFailure_CacheRemainsClean()
    {
        var loader = new FailingLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<TestResource>(loader, logger);

        try
        {
            cache.Load("test.fail", validateFirst: false);
        }
        catch (InvalidDataException)
        {
            // Expected
        }

        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void Load_WithNonExistentFile_ValidationFailsWithFileNotFound()
    {
        var validator = new ResourceValidator();

        var result = validator.Validate(
            "nonexistent/file.txt",
            new[] { ".txt" }
        );

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.FileNotFound, result.ErrorCode);
    }

    [Fact]
    public void Load_WithUnsupportedExtension_ValidationFailsWithUnsupportedExtension()
    {
        using var tempDir = new TempDirectory();
        var filePath = System.IO.Path.Combine(tempDir.Path, "test.xyz");
        File.WriteAllText(filePath, "content");

        var validator = new ResourceValidator();

        var result = validator.Validate(filePath, new[] { ".txt", ".png" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.UnsupportedExtension, result.ErrorCode);
        Assert.Contains(".xyz", result.ErrorMessage);
    }

    [Fact]
    public void Load_WithEmptyFile_ValidationFailsWithInvalidFileData()
    {
        using var tempDir = new TempDirectory();
        var filePath = System.IO.Path.Combine(tempDir.Path, "empty.txt");
        File.WriteAllText(filePath, "");

        var validator = new ResourceValidator();

        var result = validator.Validate(filePath, new[] { ".txt" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.InvalidFileData, result.ErrorCode);
        Assert.Contains("empty", result.ErrorMessage);
    }

    [Fact]
    public void Load_WithFileTooLarge_ValidationFailsWithFileTooLarge()
    {
        using var tempDir = new TempDirectory();
        var filePath = System.IO.Path.Combine(tempDir.Path, "large.txt");

        var largeContent = new string('X', 1024);
        File.WriteAllText(filePath, largeContent);

        var validator = new ResourceValidator(maxFileSizeBytes: 512);

        var result = validator.Validate(filePath, new[] { ".txt" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.FileTooLarge, result.ErrorCode);
        Assert.Contains("512", result.ErrorMessage);
    }

    [Fact]
    public void Load_MultipleFailures_DoesNotLeakResources()
    {
        var loader = new FailingLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<TestResource>(loader, logger);

        for (int i = 0; i < 10; i++)
        {
            try
            {
                cache.Load($"test{i}.fail", validateFirst: false);
            }
            catch (InvalidDataException)
            {
                // Expected
            }
        }

        Assert.Equal(0, cache.CachedCount);
        Assert.Equal(0, cache.TotalMemoryUsage);
    }

    [Fact]
    public void Unload_WithNonExistentPath_DoesNotThrow()
    {
        var loader = new FailingLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<TestResource>(loader, logger);

        var exception = Record.Exception(() => cache.Unload("nonexistent.fail"));

        Assert.Null(exception);
    }
}

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MicroEngineTests",
            Guid.NewGuid().ToString()
        );
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
