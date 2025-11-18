using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

public sealed class ResourceValidatorTests : IDisposable
{
    private readonly ResourceValidator _validator;
    private readonly string _testFilesDir;

    public ResourceValidatorTests()
    {
        _validator = new ResourceValidator(maxFileSizeBytes: 1024 * 1024); // 1 MB for tests
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
    public void Validate_ValidFile_ReturnsSuccess()
    {
        var filePath = Path.Combine(_testFilesDir, "test.png");
        File.WriteAllText(filePath, "fake png content");

        var result = _validator.Validate(filePath, new[] { ".png" });

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_FileNotFound_ReturnsFileNotFoundError()
    {
        var filePath = Path.Combine(_testFilesDir, "nonexistent.png");

        var result = _validator.Validate(filePath, new[] { ".png" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.FileNotFound, result.ErrorCode);
    }

    [Fact]
    public void Validate_UnsupportedExtension_ReturnsUnsupportedExtensionError()
    {
        var filePath = Path.Combine(_testFilesDir, "test.xyz");
        File.WriteAllText(filePath, "content");

        var result = _validator.Validate(filePath, new[] { ".png", ".jpg" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.UnsupportedExtension, result.ErrorCode);
        Assert.Contains(".png", result.ErrorMessage);
        Assert.Contains(".jpg", result.ErrorMessage);
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsInvalidFileDataError()
    {
        var filePath = Path.Combine(_testFilesDir, "empty.png");
        File.WriteAllText(filePath, "");

        var result = _validator.Validate(filePath, new[] { ".png" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.InvalidFileData, result.ErrorCode);
    }

    [Fact]
    public void Validate_FileTooLarge_ReturnsFileTooLargeError()
    {
        var filePath = Path.Combine(_testFilesDir, "large.png");
        var largeContent = new string('X', 2 * 1024 * 1024); // 2 MB
        File.WriteAllText(filePath, largeContent);

        var result = _validator.Validate(filePath, new[] { ".png" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.FileTooLarge, result.ErrorCode);
    }

    [Fact]
    public void Validate_NullPath_ReturnsInvalidPathError()
    {
        var result = _validator.Validate(null!, new[] { ".png" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.InvalidPath, result.ErrorCode);
    }

    [Fact]
    public void Validate_EmptyPath_ReturnsInvalidPathError()
    {
        var result = _validator.Validate("", new[] { ".png" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.InvalidPath, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhitespacePath_ReturnsInvalidPathError()
    {
        var result = _validator.Validate("   ", new[] { ".png" });

        Assert.False(result.IsValid);
        Assert.Equal(ResourceValidationError.InvalidPath, result.ErrorCode);
    }

    [Fact]
    public void Validate_EmptySupportedExtensionsList_AcceptsAnyExtension()
    {
        var filePath = Path.Combine(_testFilesDir, "test.xyz");
        File.WriteAllText(filePath, "content");

        var result = _validator.Validate(filePath, Array.Empty<string>());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CaseInsensitiveExtension_Works()
    {
        var filePath = Path.Combine(_testFilesDir, "test.PNG");
        File.WriteAllText(filePath, "content");

        var result = _validator.Validate(filePath, new[] { ".png" });

        Assert.True(result.IsValid);
    }
}
