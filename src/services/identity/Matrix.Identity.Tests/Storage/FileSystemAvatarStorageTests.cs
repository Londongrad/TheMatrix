using FluentAssertions;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Matrix.Identity.Tests.Storage;

public sealed class FileSystemAvatarStorageTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly FileSystemAvatarStorage _storage;

    public FileSystemAvatarStorageTests()
    {
        _contentRoot = Path.Combine(
            Path.GetTempPath(),
            "matrix-avatar-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_contentRoot);

        _storage = new FileSystemAvatarStorage(new TestHostEnvironment(_contentRoot));
    }

    [Fact]
    public async Task SaveAsync_ShouldRejectHtmlPayloadDisguisedAsPng()
    {
        await using var content = new MemoryStream("<html><body>not-an-image</body></html>"u8.ToArray());

        Func<Task> act = () => _storage.SaveAsync(
            content: content,
            fileName: "avatar.png",
            contentType: "image/png");

        MatrixApplicationException ex = (await act.Should().ThrowAsync<MatrixApplicationException>()).Which;
        ex.Code.Should().Be("Identity.Avatar.UnsupportedFormat");
    }

    [Fact]
    public async Task SaveAsync_ShouldRejectSvgPayloadDisguisedAsPng()
    {
        await using var content = new MemoryStream("""
                                                  <svg xmlns="http://www.w3.org/2000/svg" width="10" height="10">
                                                    <rect width="10" height="10" fill="red" />
                                                  </svg>
                                                  """u8.ToArray());

        Func<Task> act = () => _storage.SaveAsync(
            content: content,
            fileName: "avatar.png",
            contentType: "image/png");

        MatrixApplicationException ex = (await act.Should().ThrowAsync<MatrixApplicationException>()).Which;
        ex.Code.Should().Be("Identity.Avatar.UnsupportedFormat");
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreValidatedAvatarOutsideLegacyWebRoot()
    {
        await using MemoryStream content = await CreateValidPngAsync();

        string path = await _storage.SaveAsync(
            content: content,
            fileName: "avatar.png",
            contentType: "image/png");

        string savedFileName = Path.GetFileName(path);
        string privateFile = Path.Combine(_contentRoot, "App_Data", "avatars", savedFileName);
        string legacyFile = Path.Combine(_contentRoot, "wwwroot", "avatars", savedFileName);

        File.Exists(privateFile).Should().BeTrue();
        File.Exists(legacyFile).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldIgnoreTraversalPathsOutsideAvatarRoot()
    {
        string outsideFile = Path.Combine(_contentRoot, "outside.txt");
        await File.WriteAllTextAsync(outsideFile, "keep-me");

        await _storage.DeleteAsync("/avatars/../outside.txt");

        File.Exists(outsideFile).Should().BeTrue();
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnNullForTraversalPath()
    {
        AvatarFileReadResult? result = await _storage.OpenReadAsync("/avatars/../outside.txt");

        result.Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
            Directory.Delete(_contentRoot, recursive: true);
    }

    private static async Task<MemoryStream> CreateValidPngAsync()
    {
        using var image = new Image<Rgba32>(1, 1);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Matrix.Identity.Tests";

        public string ContentRootPath { get; set; } = contentRootPath;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
