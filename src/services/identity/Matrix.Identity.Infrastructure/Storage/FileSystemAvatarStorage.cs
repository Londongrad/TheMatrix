using Matrix.Identity.Application.Abstractions.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;

namespace Matrix.Identity.Infrastructure.Storage
{
    /// <summary>
    ///     Stores avatars in a private folder outside wwwroot and serves them only through a controlled endpoint.
    /// </summary>
    public sealed class FileSystemAvatarStorage(IHostEnvironment env) : IAvatarStorage
    {
        private const string AvatarUrlPrefix = "/avatars/";
        private const string DefaultExtension = ".png";

        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        public async Task<string> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            string ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = DefaultExtension;

            string avatarsRoot = GetPrivateAvatarsRoot();
            Directory.CreateDirectory(avatarsRoot);

            string finalFileName = $"{Guid.NewGuid()}{ext}";
            string physicalPath = Path.Combine(
                path1: avatarsRoot,
                path2: finalFileName);

            await using var fs = new FileStream(
                path: physicalPath,
                mode: FileMode.Create,
                access: FileAccess.Write,
                share: FileShare.None);

            await content.CopyToAsync(
                destination: fs,
                cancellationToken: cancellationToken);

            return AvatarUrlPrefix + finalFileName;
        }

        public Task<AvatarFileReadResult?> OpenReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult<AvatarFileReadResult?>(null);

            string fileName = ExtractFileName(path);
            if (string.IsNullOrWhiteSpace(fileName))
                return Task.FromResult<AvatarFileReadResult?>(null);

            string? physicalPath = ResolveExistingAvatarPath(fileName);
            if (physicalPath is null)
                return Task.FromResult<AvatarFileReadResult?>(null);

            string contentType = ResolveContentType(fileName);
            Stream stream = new FileStream(
                path: physicalPath,
                mode: FileMode.Open,
                access: FileAccess.Read,
                share: FileShare.Read);

            return Task.FromResult<AvatarFileReadResult?>(
                new AvatarFileReadResult(
                    Content: stream,
                    ContentType: contentType));
        }

        public Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.CompletedTask;

            string fileName = ExtractFileName(path);
            if (string.IsNullOrWhiteSpace(fileName))
                return Task.CompletedTask;

            string privatePath = Path.Combine(
                path1: GetPrivateAvatarsRoot(),
                path2: fileName);
            if (File.Exists(privatePath))
                File.Delete(privatePath);

            string legacyPath = Path.Combine(
                path1: GetLegacyAvatarsRoot(),
                path2: fileName);
            if (File.Exists(legacyPath))
                File.Delete(legacyPath);

            return Task.CompletedTask;
        }

        private string? ResolveExistingAvatarPath(string fileName)
        {
            string privatePath = Path.Combine(
                path1: GetPrivateAvatarsRoot(),
                path2: fileName);
            if (File.Exists(privatePath))
                return privatePath;

            string legacyPath = Path.Combine(
                path1: GetLegacyAvatarsRoot(),
                path2: fileName);
            return File.Exists(legacyPath)
                ? legacyPath
                : null;
        }

        private string GetPrivateAvatarsRoot()
        {
            return Path.Combine(
                path1: env.ContentRootPath,
                path2: "App_Data",
                path3: "avatars");
        }

        private string GetLegacyAvatarsRoot()
        {
            return Path.Combine(
                path1: env.ContentRootPath,
                path2: "wwwroot",
                path3: "avatars");
        }

        private static string ExtractFileName(string path)
        {
            string normalized = path.Trim()
               .Replace('\\', '/');

            if (normalized.StartsWith(AvatarUrlPrefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized[AvatarUrlPrefix.Length..];

            return Path.GetFileName(normalized);
        }

        private static string ResolveContentType(string fileName)
        {
            return ContentTypeProvider.TryGetContentType(fileName, out string? contentType)
                ? contentType
                : "application/octet-stream";
        }
    }
}
