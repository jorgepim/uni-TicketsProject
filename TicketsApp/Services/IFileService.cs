namespace TicketsApp.Services
{
    public interface IFileService
    {
        Task<List<string>> GuardarArchivosAsync(List<IFormFile> archivos, int ticketId);
        Task<bool> EliminarArchivoAsync(string rutaArchivo);
        string ObtenerRutaCompleta(string rutaRelativa);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private const string CARPETA_ADJUNTOS = "adjuntos";
        private readonly string[] _extensionesPermitidas = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".jpeg", ".png", ".zip", ".rar" };
        private const long TAMAÑO_MAXIMO = 10 * 1024 * 1024; // 10MB

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<List<string>> GuardarArchivosAsync(List<IFormFile> archivos, int ticketId)
        {
            var rutasGuardadas = new List<string>();

            if (archivos == null || !archivos.Any())
                return rutasGuardadas;

            // Crear la ruta base dentro del proyecto
            var carpetaBase = Path.Combine(_environment.ContentRootPath, "wwwroot", CARPETA_ADJUNTOS);
            var carpetaTicket = Path.Combine(carpetaBase, $"ticket_{ticketId}");

            // Crear directorios si no existen
            Directory.CreateDirectory(carpetaTicket);

            foreach (var archivo in archivos)
            {
                try
                {
                    if (archivo.Length > 0)
                    {
                        // Validaciones
                        if (archivo.Length > TAMAÑO_MAXIMO)
                        {
                            _logger.LogWarning($"Archivo {archivo.FileName} excede el tamaño máximo permitido");
                            continue;
                        }

                        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                        if (!_extensionesPermitidas.Contains(extension))
                        {
                            _logger.LogWarning($"Extensión {extension} no permitida para archivo {archivo.FileName}");
                            continue;
                        }

                        // Generar nombre único para evitar conflictos
                        var nombreUnico = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}_{archivo.FileName}";
                        var rutaCompleta = Path.Combine(carpetaTicket, nombreUnico);

                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await archivo.CopyToAsync(stream);
                        }

                        // Guardar ruta relativa para la base de datos
                        var rutaRelativa = Path.Combine(CARPETA_ADJUNTOS, $"ticket_{ticketId}", nombreUnico);
                        rutasGuardadas.Add(rutaRelativa);

                        _logger.LogInformation($"Archivo guardado: {rutaRelativa}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al guardar archivo {archivo.FileName}");
                }
            }

            return rutasGuardadas;
        }

        public async Task<bool> EliminarArchivoAsync(string rutaRelativa)
        {
            try
            {
                var rutaCompleta = Path.Combine(_environment.ContentRootPath, "wwwroot", rutaRelativa);

                if (File.Exists(rutaCompleta))
                {
                    File.Delete(rutaCompleta);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar archivo: {rutaRelativa}");
                return false;
            }
        }

        public string ObtenerRutaCompleta(string rutaRelativa)
        {
            return Path.Combine(_environment.ContentRootPath, "wwwroot", rutaRelativa);
        }
    }
}
