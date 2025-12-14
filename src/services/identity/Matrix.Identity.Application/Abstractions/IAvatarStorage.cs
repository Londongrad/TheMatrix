namespace Matrix.Identity.Application.Abstractions
{
    public interface IAvatarStorage
    {
        /// <summary>
        ///     Сохраняет файл аватарки и возвращает относительный путь или URL.
        /// </summary>
        Task<string> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Удаляет ранее сохранённый файл (если существует).
        /// </summary>
        Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default);
    }
}
