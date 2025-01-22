using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.Dto;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
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
                // Verificar que el archivo tenga contenido
                if (file == null || file.Length <= 0)
                {
                    throw new ArgumentException("El archivo está vacío o no se ha recibido.");
                }

                // Validar que el archivo sea un APK
                if (!string.Equals(Path.GetExtension(file.FileName), ".apk", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("El archivo no es un APK válido.");
                }

                // Ruta para guardar el archivo dentro de wwwroot/Files
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

                // Crear la carpeta 'Files' si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Extraer información del archivo APK (usando un servicio/apkProcessor personalizado)
                ApkFileDto? apkInfo = null;
                using (var stream = file.OpenReadStream())
                {
                    // Método personalizado para extraer información del APK
                    apkInfo = _apkProcessor.ExtractApkInfo(null, stream);
                }

                if (apkInfo == null)
                {
                    throw new ArgumentException("No se pudo extraer la información del APK.");
                }

                // Ruta completa para el archivo
                string sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName); // Nombre sin extensión
                sanitizedFileName = sanitizedFileName.Replace(" ", "_");
                string fileName = $"{sanitizedFileName}-{apkInfo.Version}/{apkInfo.CreatedAt}.apk";
                string filePath = Path.Combine(folderPath, fileName);

                // Guardar el archivo en la carpeta 'Files'
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"Archivo APK recibido y guardado correctamente en: {filePath}";
            }
            catch (Exception ex)
            {
                // Manejar cualquier error
                return $"Error: {ex.Message}";
            }
        }




    }
}
