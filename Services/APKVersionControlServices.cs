using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace APKVersionControlAPI.Services
{
    public class APKVersionControlServices : IAPKVersionControlServices
    {
        private readonly IApkProcessor _apkProcessor;
        public APKVersionControlServices(IApkProcessor processor)
        {
            _apkProcessor = processor;
        }

        public async Task<string?> UploadApkFile(IFormFile file)
        {
            try
            {
                // Verifica si el archivo tiene contenido
                if (file == null || file.Length <= 0)
                {
                    throw new ArgumentException("The file is empty or has not been received.");
                }

                // Valida que el archivo sea un APK
                if (!string.Equals(Path.GetExtension(file.FileName), ".apk", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("The file is not a valid APK.");
                }

                // Ruta para guardar el archivo dentro de wwwroot/Files
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

                // Crea la carpeta 'Files' si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Extrae información del archivo APK (usando un servicio personalizado/apkProcessor)
                ApkFileDto? apkInfo = null;
                using (var stream = file.OpenReadStream())
                {
                    // Método personalizado para extraer información del APK
                    apkInfo = await _apkProcessor.ExtractApkInfoAsync(null, stream);
                }

                if (apkInfo == null)
                {
                    throw new ArgumentException("Could not extract information from the APK.");
                }

                // Nombre del archivo sanitizado
                string sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "_")
                    .Replace(".", "_")
                    .ToLower();

                // Nombre del archivo con versión y fecha
                string fileName = $"{sanitizedFileName}-{apkInfo.Version}--{apkInfo.CreatedAt?.ToString("yyyyMMdd")}.apk";
                string filePath = Path.Combine(folderPath, fileName);

                // Guarda el archivo en la carpeta 'Files'
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return "APK file received, compressed, and saved successfully.";
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters) =>
            await _apkProcessor.GetAllApkAsync(parameters);

        public string FindFileForDownload(GenericParameters parameters)
        {
            // Validar que IsDownload sea true
            if (!parameters.IsDownload.HasValue || !parameters.IsDownload.Value)
            {
                throw new ArgumentException("IsDownload must be true to proceed with the download.");
            }

            // Validar que Name y Version no sean nulos o vacíos
            if (string.IsNullOrEmpty(parameters.Name) || string.IsNullOrEmpty(parameters.Version))
            {
                throw new ArgumentException("Name and Version must be provided.");
            }

            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");
            }

            // Construir el patrón de búsqueda
            string searchPattern = $"{parameters.Name}-{parameters.Version}--*.apk";

            // Buscar archivos que coincidan con el patrón
            var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                throw new FileNotFoundException($"No file found with name {parameters.Name} and version {parameters.Version}.");
            }

            // Devolver la ruta del primer archivo encontrado
            return files.FirstOrDefault()!;
        }

    }
}
