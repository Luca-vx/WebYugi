using SkiaSharp;

namespace WebYugi.Services
{
    public sealed class LocalCardImageStorage : ICardImageStorage
    {
        public const string RequestPath = "/uploads/cartas";
        private const long MaxFileBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly string _cartasFolder;

        public LocalCardImageStorage(IWebHostEnvironment environment)
        {
            _cartasFolder = Path.Combine(environment.ContentRootPath, "uploads", "cartas");
            Directory.CreateDirectory(_cartasFolder);
        }

        public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw new InvalidOperationException("Selecione uma imagem.");
            }

            if (file.Length > MaxFileBytes)
            {
                throw new InvalidOperationException("A imagem deve ter no máximo 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Formato inválido. Use JPG, PNG, WEBP ou GIF.");
            }

            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            using var codec = SKCodec.Create(buffer);
            if (codec is null)
            {
                throw new InvalidOperationException("O arquivo enviado não é uma imagem válida.");
            }

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullPath = Path.Combine(_cartasFolder, fileName);

            buffer.Position = 0;
            await using var output = File.Create(fullPath);
            await buffer.CopyToAsync(output, cancellationToken);

            return $"{RequestPath}/{fileName}";
        }

        public void Delete(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return;
            }

            var fileName = Path.GetFileName(storedPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var fullPath = Path.Combine(_cartasFolder, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
