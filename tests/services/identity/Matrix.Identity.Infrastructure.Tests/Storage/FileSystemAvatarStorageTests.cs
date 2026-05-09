using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Storage;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Matrix.Identity.Infrastructure.Tests.Storage;

public sealed class FileSystemAvatarStorageTests
{
    [Fact]
    public async Task SaveAsync_StoresPngInPrivateFolder_AndOpenReadAsyncReturnsStoredContent()
    {
        await using var fixture = new AvatarStorageFixture();
        var storage = new FileSystemAvatarStorage(fixture.Environment);
        await using MemoryStream content = await CreateImageAsync(saveAsBmp: false);

        string path = await storage.SaveAsync(content, "avatar.png", "image/png", CancellationToken.None);
        AvatarFileReadResult? readResult = await storage.OpenReadAsync(path, CancellationToken.None);

        Assert.StartsWith("/avatars/", path, StringComparison.Ordinal);
        Assert.EndsWith(".png", path, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(readResult);
        Assert.Equal("image/png", readResult.ContentType);

        await using Stream readStream = readResult.Content;
        await using var buffer = new MemoryStream();
        await readStream.CopyToAsync(buffer, CancellationToken.None);
        Assert.NotEmpty(buffer.ToArray());
        Assert.Single(Directory.GetFiles(fixture.PrivateAvatarsRoot));
    }

    [Fact]
    public async Task SaveAsync_RejectsUnsupportedImageFormat()
    {
        await using var fixture = new AvatarStorageFixture();
        var storage = new FileSystemAvatarStorage(fixture.Environment);
        await using MemoryStream content = await CreateImageAsync(saveAsBmp: true);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            () => storage.SaveAsync(content, "avatar.bmp", "image/bmp", CancellationToken.None));

        Assert.Equal("Identity.Avatar.UnsupportedFormat", exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPrivateAndLegacyFiles_AndIgnoresPathTraversal()
    {
        await using var fixture = new AvatarStorageFixture();
        var storage = new FileSystemAvatarStorage(fixture.Environment);
        string privatePath = Path.Combine(fixture.PrivateAvatarsRoot, "private.png");
        string legacyPath = Path.Combine(fixture.LegacyAvatarsRoot, "legacy.png");
        string outsidePath = Path.Combine(fixture.RootPath, "outside.txt");

        Directory.CreateDirectory(fixture.PrivateAvatarsRoot);
        Directory.CreateDirectory(fixture.LegacyAvatarsRoot);
        await File.WriteAllTextAsync(privatePath, "private");
        await File.WriteAllTextAsync(legacyPath, "legacy");
        await File.WriteAllTextAsync(outsidePath, "outside");

        await storage.DeleteAsync("/avatars/private.png", CancellationToken.None);
        await storage.DeleteAsync("legacy.png", CancellationToken.None);
        await storage.DeleteAsync("../outside.txt", CancellationToken.None);

        Assert.False(File.Exists(privatePath));
        Assert.False(File.Exists(legacyPath));
        Assert.True(File.Exists(outsidePath));
    }

    private static async Task<MemoryStream> CreateImageAsync(bool saveAsBmp)
    {
        var image = new Image<Rgba32>(1, 1, Color.HotPink);
        var buffer = new MemoryStream();

        if (saveAsBmp)
            await image.SaveAsync(buffer, new BmpEncoder());
        else
            await image.SaveAsPngAsync(buffer);

        buffer.Position = 0;
        image.Dispose();
        return buffer;
    }

    private sealed class AvatarStorageFixture : IAsyncDisposable
    {
        public AvatarStorageFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "matrix-avatar-tests", Guid.NewGuid().ToString("N"));
            Environment = new TestHostEnvironment(RootPath);
        }

        public string RootPath { get; }
        public string PrivateAvatarsRoot => Path.Combine(RootPath, "App_Data", "avatars");
        public string LegacyAvatarsRoot => Path.Combine(RootPath, "wwwroot", "avatars");
        public IHostEnvironment Environment { get; }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);

            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Tests";
        public string ApplicationName { get; set; } = "Matrix.Identity.Infrastructure.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
