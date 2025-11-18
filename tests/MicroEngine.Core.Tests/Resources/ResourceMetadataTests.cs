using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

public sealed class ResourceMetadataTests : IDisposable
{
    private readonly string _testFilesDir;

    public ResourceMetadataTests()
    {
        _testFilesDir = Path.Combine(Path.GetTempPath(), "MicroEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFilesDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFilesDir))
        {
            Directory.Delete(_testFilesDir, true);
        }
    }

    [Fact]
    public void FromFile_CreatesMetadataFromExistingFile()
    {
        var filePath = Path.Combine(_testFilesDir, "test.png");
        var content = "test content";
        File.WriteAllText(filePath, content);
        var fileInfo = new FileInfo(filePath);

        var metadata = ResourceMetadata.FromFile(filePath);

        Assert.Equal(".png", metadata.Extension);
        Assert.Equal(fileInfo.Length, metadata.FileSizeBytes);
        Assert.Equal(fileInfo.LastWriteTimeUtc, metadata.LastModified, TimeSpan.FromSeconds(1));
        Assert.Equal(DateTime.UtcNow, metadata.LoadedAt, TimeSpan.FromSeconds(1));
        Assert.Null(metadata.Version);
        Assert.Null(metadata.CustomMetadata);
        Assert.False(metadata.IsCompressed);
        Assert.Null(metadata.CompressionRatio);
    }

    [Fact]
    public void FromFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        var filePath = Path.Combine(_testFilesDir, "nonexistent.png");

        Assert.Throws<FileNotFoundException>(() => ResourceMetadata.FromFile(filePath));
    }

    [Fact]
    public void WithCustomMetadata_CreatesNewInstanceWithMetadata()
    {
        var filePath = Path.Combine(_testFilesDir, "test.png");
        File.WriteAllText(filePath, "content");
        var original = ResourceMetadata.FromFile(filePath);

        var customMetadata = new Dictionary<string, string>
        {
            ["Width"] = "1024",
            ["Height"] = "768"
        };

        var updated = original.WithCustomMetadata(customMetadata);

        Assert.NotNull(updated.CustomMetadata);
        Assert.Equal("1024", updated.CustomMetadata!["Width"]);
        Assert.Equal("768", updated.CustomMetadata["Height"]);
        Assert.Equal(original.Extension, updated.Extension);
        Assert.Equal(original.FileSizeBytes, updated.FileSizeBytes);
    }

    [Fact]
    public void WithVersion_CreatesNewInstanceWithVersion()
    {
        var filePath = Path.Combine(_testFilesDir, "test.png");
        File.WriteAllText(filePath, "content");
        var original = ResourceMetadata.FromFile(filePath);

        var updated = original.WithVersion("1.0.0");

        Assert.Equal("1.0.0", updated.Version);
        Assert.Equal(original.Extension, updated.Extension);
    }

    [Fact]
    public void WithCompression_CreatesNewInstanceWithCompressionInfo()
    {
        var filePath = Path.Combine(_testFilesDir, "test.png");
        File.WriteAllText(filePath, "content");
        var original = ResourceMetadata.FromFile(filePath);

        var updated = original.WithCompression(true, 2.5f);

        Assert.True(updated.IsCompressed);
        Assert.Equal(2.5f, updated.CompressionRatio);
        Assert.Equal(original.Extension, updated.Extension);
    }

    [Fact]
    public void Metadata_IsImmutable_CreatesNewInstances()
    {
        var filePath = Path.Combine(_testFilesDir, "test.png");
        File.WriteAllText(filePath, "content");
        var original = ResourceMetadata.FromFile(filePath);

        var withVersion = original.WithVersion("1.0.0");
        var withMetadata = withVersion.WithCustomMetadata(new Dictionary<string, string> { ["Key"] = "Value" });

        Assert.Null(original.Version);
        Assert.Null(original.CustomMetadata);
        Assert.Equal("1.0.0", withVersion.Version);
        Assert.Null(withVersion.CustomMetadata);
        Assert.Equal("1.0.0", withMetadata.Version);
        Assert.NotNull(withMetadata.CustomMetadata);
    }

    [Fact]
    public void FromFile_DifferentExtensions_CapturesCorrectExtension()
    {
        var files = new[] { "test.png", "test.jpg", "test.ttf", "test.ogg" };

        foreach (var fileName in files)
        {
            var filePath = Path.Combine(_testFilesDir, fileName);
            File.WriteAllText(filePath, "content");

            var metadata = ResourceMetadata.FromFile(filePath);

            Assert.Equal(Path.GetExtension(fileName), metadata.Extension);
        }
    }
}
