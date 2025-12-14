using Matrix.Identity.Application.Abstractions.Services;
using Microsoft.Extensions.Hosting;

namespace Matrix.Identity.Infrastructure.Storage
{
    /// <summary>
    ///     Хранит аватарки пользователей в файловой системе:
    ///     {ContentRoot}/wwwroot/avatars/{guid}.ext
    /// </summary>
    public sealed class FileSystemAvatarStorage(IHostEnvironment env) : IAvatarStorage
    {
        public async Task<string> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            // расширение файла (если нет, по умолчанию .png)
            string ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".png";

            // {ContentRoot}/wwwroot/avatars
            string wwwrootPath = Path.Combine(
                path1: env.ContentRootPath,
                path2: "wwwroot");
            string avatarsRoot = Path.Combine(
                path1: wwwrootPath,
                path2: "avatars");

            Directory.CreateDirectory(avatarsRoot);

            // имя файла: новый Guid + исходное расширение
            string finalFileName = $"{Guid.NewGuid()}{ext}";
            string physicalPath = Path.Combine(
                path1: avatarsRoot,
                path2: finalFileName);

            await using (var fs = new FileStream(
                             path: physicalPath,
                             mode: FileMode.Create,
                             access: FileAccess.Write,
                             share: FileShare.None))
                await content.CopyToAsync(
                    destination: fs,
                    cancellationToken: cancellationToken);

            // относительный путь, который пойдёт в AvatarUrl и в <img src="...">
            // /avatars/xxx.png
            string relativePath = Path.Combine(
                    path1: "avatars",
                    path2: finalFileName)
               .Replace(
                    oldValue: "\\",
                    newValue: "/");

            return "/" + relativePath;
        }

        public Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.CompletedTask;

            // убираем ведущий слэш, если он есть
            string relative = path.TrimStart(
                '/',
                '\\');

            string wwwrootPath = Path.Combine(
                path1: env.ContentRootPath,
                path2: "wwwroot");
            string physicalPath = Path.Combine(
                path1: wwwrootPath,
                path2: relative);

            if (File.Exists(physicalPath))
                File.Delete(physicalPath);

            return Task.CompletedTask;
        }
    }
}
