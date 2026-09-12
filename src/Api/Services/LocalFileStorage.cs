using Application.Interfaces;

namespace Api.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _environment;

        public LocalFileStorage(IWebHostEnvironment envinronment)
        {
            _environment = envinronment;
        }

        public Task EliminarAsync(string ruta, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return Task.CompletedTask;

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            var rutaRelativa = ruta
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var rutaFisica = Path.Combine(webRoot, rutaRelativa);

            if (File.Exists(rutaFisica))
            {
                File.Delete(rutaFisica);
            }

            return Task.CompletedTask;
        }

        public async Task<string> GuardarAsync(int productoId, Stream stream, string nombreArchivo, string contentType, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();

            var nombreGenerado = $"{Guid.NewGuid():N}{extension}";

            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var directorio = Path.Combine(
                webRoot,
                "uploads",
                "productos",
                productoId.ToString());

            Directory.CreateDirectory(directorio);

            var rutaFisica = Path.Combine(directorio, nombreGenerado);

            await using var fileStream = new FileStream(
                rutaFisica,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true
                );

            await stream.CopyToAsync(fileStream, cancellationToken);

            return $"/uploads/productos/{productoId}/{nombreGenerado}";
        }
    }
}
