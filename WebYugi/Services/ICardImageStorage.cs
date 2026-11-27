namespace WebYugi.Services
{
    public interface ICardImageStorage
    {
        Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
        void Delete(string? storedPath);
    }
}
