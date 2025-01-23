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

        public async Task<string?> UploadApkFile(IFormFile file, string? client = null)
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

                // Ruta base para guardar el archivo dentro de wwwroot/Files
                string baseFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

                // Crea la carpeta base 'Files' si no existe
                if (!Directory.Exists(baseFolderPath))
                {
                    Directory.CreateDirectory(baseFolderPath);
                }

                // Si el cliente no es nulo o vacío, crear una subcarpeta para el cliente
                string folderPath = baseFolderPath;
                if (!string.IsNullOrWhiteSpace(client))
                {
                    folderPath = Path.Combine(baseFolderPath, client.ToLower()); // Convertir a minúsculas
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                }

                // Extrae información del archivo APK
                ApkFileDto? apkInfo;
                using (var stream = file.OpenReadStream())
                {
                    apkInfo = await _apkProcessor.ExtractApkInfoAsync(null, stream);
                }

                if (apkInfo == null)
                {
                    throw new ArgumentException("Could not extract information from the APK.");
                }

                // Sanitiza el nombre del archivo
                string sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));

                // Nombre del archivo con versión y fecha
                string fileName = $"{sanitizedFileName}-{apkInfo.Version}--{apkInfo.CreatedAt:yyyyMMdd}.apk";
                string filePath = Path.Combine(folderPath, fileName);

                // Guarda el archivo en la carpeta correspondiente
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return "APK file received, processed, and saved successfully.";
            }
            catch (Exception ex)
            {
                // Registrar el error y lanzar una excepción personalizada
                throw new ApplicationException("An error occurred while uploading the APK file.", ex);
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

            var baseDirectory = ValidateAndGetBaseDirectory(parameters);

            // Construir el patrón de búsqueda
            string searchPattern = $"{parameters.Name}-{parameters.Version}--*.apk";

            // Buscar archivos que coincidan con el patrón
            var files = Directory.GetFiles(baseDirectory, searchPattern, SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                throw new FileNotFoundException($"No file found with name {parameters.Name} and version {parameters.Version}.");
            }

            // Devolver la ruta del primer archivo encontrado
            return files.First();
        }

        public void DeleteApkFile(GenericParameters parameters)
        {
            try
            {
                // Get the validated base directory
                var baseDirectory = ValidateAndGetBaseDirectory(parameters);

                // Build the search pattern
                string searchPattern = $"{parameters.Name}-{parameters.Version}--*.apk";

                // Find all files matching the pattern
                var matchingFiles = Directory.GetFiles(baseDirectory, searchPattern, SearchOption.TopDirectoryOnly);

                // Check if any files were found
                if (matchingFiles.Length == 0)
                {
                    throw new FileNotFoundException($"No files matching the pattern '{searchPattern}' were found in {baseDirectory}.");
                }

                // Delete all matching files
                foreach (var filePath in matchingFiles)
                {
                    File.Delete(filePath);
                    Console.WriteLine($"The file {filePath} has been successfully deleted.");
                }

                Console.WriteLine($"{matchingFiles.Length} file(s) were deleted successfully.");
            }
            catch (Exception ex)
            {
                // Rethrow the exception with a custom message or handle it as needed
                throw new Exception($"Error deleting the APK file(s): {ex.Message}", ex);
            }
        }

        private string ValidateAndGetBaseDirectory(GenericParameters parameters)
        {
            
            // Validar que Name y Version no sean nulos o vacíos
            if (string.IsNullOrEmpty(parameters.Name) || string.IsNullOrEmpty(parameters.Version))
            {
                throw new ArgumentException("Name and Version must be provided.");
            }

            // Determinar el directorio base
            string baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

            // Si el cliente no es nulo o vacío, agregarlo al path
            if (!string.IsNullOrWhiteSpace(parameters.Client))
            {
                baseDirectory = Path.Combine(baseDirectory, parameters.Client);
            }

            // Verificar si el directorio existe
            if (!Directory.Exists(baseDirectory))
            {
                throw new DirectoryNotFoundException($"The directory {baseDirectory} does not exist.");
            }

            return baseDirectory;
        }

        /// <summary>
        /// Sanitiza el nombre del archivo para eliminar caracteres no válidos.
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.");
            }

            // Reemplaza caracteres no válidos
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Replace(" ", "_")
                .Replace(".", "_")
                .ToLower()
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            return sanitized;
        }
    }
}
