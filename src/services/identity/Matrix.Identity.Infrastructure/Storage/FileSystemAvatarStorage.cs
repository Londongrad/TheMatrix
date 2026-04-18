using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Errors;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Matrix.Identity.Infrastructure.Storage
{
    /// <summary>
    ///     Stores avatars in a private folder outside wwwroot and serves them only through a controlled endpoint.
    /// </summary>
    public sealed class FileSystemAvatarStorage(IHostEnvironment env) : IAvatarStorage
    {
        private const string AvatarUrlPrefix = "/avatars/";
        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
        private static readonly IReadOnlyDictionary<string, AvatarFormatDescriptor> AllowedFormats =
            new Dictionary<string, AvatarFormatDescriptor>(StringComparer.OrdinalIgnoreCase)
            {
                ["JPEG"] = new(".jpg", "image/jpeg"),
                ["PNG"] = new(".png", "image/png"),
                ["WEBP"] = new(".webp", "image/webp")
            };

        public async Task<string> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            AvatarValidatedContent validatedContent = await ValidateAsync(
                content: content,
                cancellationToken: cancellationToken);

            string avatarsRoot = GetPrivateAvatarsRoot();
            Directory.CreateDirectory(avatarsRoot);

            string finalFileName = $"{Guid.NewGuid()}{validatedContent.Extension}";
            string physicalPath = Path.Combine(
                path1: avatarsRoot,
                path2: finalFileName);

            await using var fs = new FileStream(
                path: physicalPath,
                mode: FileMode.Create,
                access: FileAccess.Write,
                share: FileShare.None);

            validatedContent.Buffer.Position = 0;
            await validatedContent.Buffer.CopyToAsync(
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

            string? privatePath = TryResolveAvatarPath(
                root: GetPrivateAvatarsRoot(),
                path: path);
            if (privatePath is not null && File.Exists(privatePath))
                File.Delete(privatePath);

            string? legacyPath = TryResolveAvatarPath(
                root: GetLegacyAvatarsRoot(),
                path: path);
            if (legacyPath is not null && File.Exists(legacyPath))
                File.Delete(legacyPath);

            return Task.CompletedTask;
        }

        private string? ResolveExistingAvatarPath(string fileName)
        {
            string? privatePath = TryResolveAvatarPath(
                root: GetPrivateAvatarsRoot(),
                path: fileName);
            if (privatePath is not null && File.Exists(privatePath))
                return privatePath;

            string? legacyPath = TryResolveAvatarPath(
                root: GetLegacyAvatarsRoot(),
                path: fileName);
            return legacyPath is not null && File.Exists(legacyPath)
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

        private static string? TryResolveAvatarPath(
            string root,
            string path)
        {
            string? relative = ExtractRelativePath(path);
            if (string.IsNullOrWhiteSpace(relative))
                return null;

            if (!string.Equals(relative, Path.GetFileName(relative), PathComparison))
                return null;

            string rootFullPath = EnsureTrailingDirectorySeparator(Path.GetFullPath(root));
            string candidateFullPath = Path.GetFullPath(Path.Combine(rootFullPath, relative));

            return candidateFullPath.StartsWith(rootFullPath, PathComparison)
                ? candidateFullPath
                : null;
        }

        private static string? ExtractRelativePath(string path)
        {
            string normalized = path.Trim()
               .Replace('\\', '/');

            if (normalized.StartsWith(AvatarUrlPrefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized[AvatarUrlPrefix.Length..];
            else
                normalized = normalized.TrimStart('/', '\\');

            return string.IsNullOrWhiteSpace(normalized)
                ? null
                : normalized;
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static async Task<AvatarValidatedContent> ValidateAsync(
            Stream content,
            CancellationToken cancellationToken)
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(
                destination: buffer,
                cancellationToken: cancellationToken);

            if (buffer.Length == 0)
            {
                await buffer.DisposeAsync();
                throw ApplicationErrorsFactory.AvatarContentInvalid();
            }

            buffer.Position = 0;

            try
            {
                using Image image = await Image.LoadAsync(
                    stream: buffer,
                    cancellationToken: cancellationToken);

                IImageFormat? format = image.Metadata.DecodedImageFormat;
                if (format is null ||
                    !AllowedFormats.TryGetValue(format.Name, out AvatarFormatDescriptor? descriptor))
                {
                    await buffer.DisposeAsync();
                    throw ApplicationErrorsFactory.AvatarFormatNotSupported();
                }

                buffer.Position = 0;
                return new AvatarValidatedContent(
                    Buffer: buffer,
                    Extension: descriptor.Extension);
            }
            catch (Matrix.BuildingBlocks.Application.Exceptions.MatrixApplicationException)
            {
                throw;
            }
            catch (UnknownImageFormatException)
            {
                await buffer.DisposeAsync();
                throw ApplicationErrorsFactory.AvatarFormatNotSupported();
            }
            catch (InvalidImageContentException)
            {
                await buffer.DisposeAsync();
                throw ApplicationErrorsFactory.AvatarContentInvalid();
            }
        }

        private sealed record AvatarValidatedContent(
            MemoryStream Buffer,
            string Extension);

        private sealed record AvatarFormatDescriptor(string Extension, string ContentType);
    }
}
