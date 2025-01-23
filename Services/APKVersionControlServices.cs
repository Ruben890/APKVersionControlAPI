using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using APKVersionControlAPI.Ultils;
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

                // Comprime el archivo APK a un archivo ZIP
                string zipFileName = Path.ChangeExtension(fileName, ".zip");
                string zipFilePath = Path.Combine(folderPath, zipFileName);

                // Comprime el archivo de manera asíncrona
                await Task.Run(() => FileCompressor.CompressFile(filePath, zipFilePath));

                // Elimina el archivo APK original solo si la compresión fue exitosa
                if (File.Exists(zipFilePath))
                {
                    File.Delete(filePath);
                }
                else
                {
                    throw new InvalidOperationException("Failed to compress the APK file.");
                }

                return "APK file received, compressed, and saved successfully.";
            }
            catch (Exception ex)
            {
                // Maneja cualquier error y proporciona más detalles
                return $"Error: {ex.Message}. StackTrace: {ex.StackTrace}";
            }
        }

        public async Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters) =>
            await _apkProcessor.GetAllApkAsync(parameters);


    }
}
