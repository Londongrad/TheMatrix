using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Matrix.Identity.Infrastructure.Tests.Storage
{
    public sealed class FileSystemAvatarStorageTests
    {
        [Fact]
        public async Task SaveAsync_StoresPngInPrivateFolder_AndOpenReadAsyncReturnsStoredContent()
        {
            await using var fixture = new AvatarStorageFixture();
            var storage = new FileSystemAvatarStorage(fixture.Environment);
            await using MemoryStream content = await CreateImageAsync(saveAsBmp: false);

            string path = await storage.SaveAsync(
                content: content,
                fileName: "avatar.png",
                contentType: "image/png",
                cancellationToken: CancellationToken.None);
            AvatarFileReadResult? readResult = await storage.OpenReadAsync(
                path: path,
                cancellationToken: CancellationToken.None);

            Assert.StartsWith(
                expectedStartString: "/avatars/",
                actualString: path,
                comparisonType: StringComparison.Ordinal);
            Assert.EndsWith(
                expectedEndString: ".png",
                actualString: path,
                comparisonType: StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(readResult);
            Assert.Equal(
                expected: "image/png",
                actual: readResult.ContentType);

            await using Stream readStream = readResult.Content;
            await using var buffer = new MemoryStream();
            await readStream.CopyToAsync(
                destination: buffer,
                cancellationToken: CancellationToken.None);
            Assert.NotEmpty(buffer.ToArray());
            Assert.Single(Directory.GetFiles(fixture.PrivateAvatarsRoot));
        }

        [Fact]
        public async Task SaveAsync_RejectsUnsupportedImageFormat()
        {
            await using var fixture = new AvatarStorageFixture();
            var storage = new FileSystemAvatarStorage(fixture.Environment);
            await using MemoryStream content = await CreateImageAsync(saveAsBmp: true);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => storage.SaveAsync(
                    content: content,
                    fileName: "avatar.bmp",
                    contentType: "image/bmp",
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Avatar.UnsupportedFormat",
                actual: exception.Code);
        }

        [Fact]
        public async Task DeleteAsync_RemovesPrivateAndLegacyFiles_AndIgnoresPathTraversal()
        {
            await using var fixture = new AvatarStorageFixture();
            var storage = new FileSystemAvatarStorage(fixture.Environment);
            string privatePath = Path.Combine(
                path1: fixture.PrivateAvatarsRoot,
                path2: "private.png");
            string legacyPath = Path.Combine(
                path1: fixture.LegacyAvatarsRoot,
                path2: "legacy.png");
            string outsidePath = Path.Combine(
                path1: fixture.RootPath,
                path2: "outside.txt");

            Directory.CreateDirectory(fixture.PrivateAvatarsRoot);
            Directory.CreateDirectory(fixture.LegacyAvatarsRoot);
            await File.WriteAllTextAsync(
                path: privatePath,
                contents: "private");
            await File.WriteAllTextAsync(
                path: legacyPath,
                contents: "legacy");
            await File.WriteAllTextAsync(
                path: outsidePath,
                contents: "outside");

            await storage.DeleteAsync(
                path: "/avatars/private.png",
                cancellationToken: CancellationToken.None);
            await storage.DeleteAsync(
                path: "legacy.png",
                cancellationToken: CancellationToken.None);
            await storage.DeleteAsync(
                path: "../outside.txt",
                cancellationToken: CancellationToken.None);

            Assert.False(File.Exists(privatePath));
            Assert.False(File.Exists(legacyPath));
            Assert.True(File.Exists(outsidePath));
        }

        private static async Task<MemoryStream> CreateImageAsync(bool saveAsBmp)
        {
            var image = new Image<Rgba32>(
                width: 1,
                height: 1,
                backgroundColor: Color.HotPink);
            var buffer = new MemoryStream();

            if (saveAsBmp)
                await image.SaveAsync(
                    stream: buffer,
                    encoder: new BmpEncoder());
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
                RootPath = Path.Combine(
                    path1: Path.GetTempPath(),
                    path2: "matrix-avatar-tests",
                    path3: Guid.NewGuid()
                       .ToString("N"));
                Environment = new TestHostEnvironment(RootPath);
            }

            public string RootPath { get; }

            public string PrivateAvatarsRoot => Path.Combine(
                path1: RootPath,
                path2: "App_Data",
                path3: "avatars");

            public string LegacyAvatarsRoot => Path.Combine(
                path1: RootPath,
                path2: "wwwroot",
                path3: "avatars");

            public IHostEnvironment Environment { get; }

            public ValueTask DisposeAsync()
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(
                        path: RootPath,
                        recursive: true);

                return ValueTask.CompletedTask;
            }
        }

        private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Tests";
            public string ApplicationName { get; set; } = "Matrix.Identity.Infrastructure.Tests";
            public string ContentRootPath { get; set; } = contentRootPath;
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }
    }
}
