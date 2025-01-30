using APKVersionControlAPI.Entity;
using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using APKVersionControlAPI.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace APKVersionControlAPI.Services
{
    public class APKVersionControlServices : IAPKVersionControlServices
    {
        private readonly IApkFileRepository _repository;
        public APKVersionControlServices(IApkFileRepository repository)
        {
            _repository = repository;
        }

        public async Task<string?> UploadApkFile(IFormFile file, string? client = null)
        {
            string? filePath = null;
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

                // Extrae la version  del archivo APK
                string? version;
                using (var stream = file.OpenReadStream())
                {
                    version = await ExtractApkVersionAsync(baseFolderPath, stream);
                }

                if (string.IsNullOrWhiteSpace(version))
                {
                    throw new ArgumentException("Could not extract information from the APK.");
                }

                // Sanitiza el nombre del archivo
                string sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));

                // Nombre del archivo con versión y fecha
                string fileName = $"{sanitizedFileName}-{version}--{DateTime.Now:yyyyMMdd}.apk";
                filePath = Path.Combine(folderPath, fileName);

                // Guarda el archivo en la carpeta correspondiente
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var apkFile = new ApkFile
                {
                    Name = Path.GetFileNameWithoutExtension(file.FileName),
                    Size = Math.Round(file.Length / (1024.0 * 1024.0), 2),
                    CreatedAt = DateTime.Now,
                    FilePath = folderPath,
                    Version = version,
                    FileName = fileName,
                    Client = client ?? null,
                };

                _repository.Beggin();
                await _repository.Insert(apkFile);
                await _repository.SaveAsync();
                _repository.Commit();

                return "APK file received, processed, and saved successfully.";
            }
            catch (Exception ex)
            {
                _repository.Roolback();
                if(File.Exists(filePath)) File.Delete(filePath);
                throw new ApplicationException("An error occurred while uploading the APK file.", ex);
            }
        }

        public async Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters) =>
            await _repository.GetAllApkAsync(parameters);

        public async Task<string> FindFileForDownload(DownloadParameters parameters)
        {
            // Validar que IsDownload sea true
            if (parameters.IsDownload is not true)
            {
                throw new ArgumentException("IsDownload must be true to proceed with the download.");
            }

            // Obtener el archivo desde la base de datos
            var apkFile = await _repository.GetApkFileById(parameters.Id ?? throw new ArgumentNullException(nameof(parameters.Id), "Id must not be null."));

            if (apkFile == null)
            {
                throw new FileNotFoundException($"No APK file found with Id {parameters.Id}");
            }

            // Validar que los campos requeridos no sean null o vacíos
            if (string.IsNullOrWhiteSpace(apkFile.FilePath) || string.IsNullOrWhiteSpace(apkFile.FileName))
            {
                throw new InvalidOperationException($"FilePath or FileName is invalid for APK file with Id {parameters.Id}");
            }

            // Buscar archivos que coincidan con el patrón
            var files = Directory.GetFiles(apkFile.FilePath, apkFile.FileName, SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                throw new FileNotFoundException($"No file found in directory {apkFile.FilePath} with name {apkFile.FileName}");
            }

            // Devolver la ruta del primer archivo encontrado
            return files.First();
        }

        public async Task DeleteApkFile(int Id)
        {
            try
            {
                // Obtener el archivo desde la base de datos
                var apkFile = await _repository.GetApkFileById(Id);


                if (apkFile == null)
                {
                    throw new FileNotFoundException($"No APK file found with Id {Id}");
                }

                // Validar que los campos requeridos no sean null o vacíos
                if (string.IsNullOrWhiteSpace(apkFile.FilePath) || string.IsNullOrWhiteSpace(apkFile.FileName))
                {
                    throw new InvalidOperationException($"FilePath or FileName is invalid for APK file with Id {Id}");
                }

                // Find all files matching the pattern
                var matchingFiles = Directory.GetFiles(apkFile.FilePath, apkFile.FileName, SearchOption.TopDirectoryOnly);

                // Check if any files were found
                if (matchingFiles.Length == 0)
                {
                    throw new FileNotFoundException($"No files matching the pattern '{apkFile.FileName}' were found in {apkFile.FilePath}.");
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

        private async Task<string> ExtractApkVersionAsync(string filePath, Stream apkFileStream)
        {
            if (apkFileStream == null || !apkFileStream.CanRead)
            {
                throw new ArgumentException("The data stream is invalid.", nameof(apkFileStream));
            }

            // Crear un directorio temporal para extraer el contenido del APK
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            string? version = null;

            try
            {
                string tempApkPath = Path.Combine(tempPath, "temp.apk");

                // Guardar el stream en un archivo temporal
                await using (var fileStream = new FileStream(tempApkPath, FileMode.Create, FileAccess.Write))
                {
                    await apkFileStream.CopyToAsync(fileStream);
                }

                // Descomprimir el archivo APK
                await Task.Run(() => APKExtractor.ExtractAPK(tempApkPath, tempPath));

                // Buscar el archivo AndroidManifest.xml en el directorio descomprimido
                string manifestPath = Path.Combine(tempPath, "AndroidManifest.xml");

                if (File.Exists(manifestPath))
                {
                    version = await ExtractVersionFromManifestAsync(manifestPath, tempPath);
                }
            }
            finally
            {
                Directory.Delete(tempPath, true);
            }

            return version ??= null!;

        }

        private static async Task<string?> GetJavaVersionAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "java",
                        Arguments = "-version",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Extraer la versión de Java
                var match = Regex.Match(output, @"\d+\.\d+\.\d+");
                return match.Success ? match.Value : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task ExecuteCommandAsync(string command)
        {
            bool isWindows = OperatingSystem.IsWindows();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "cmd.exe" : "/bin/bash",
                    Arguments = isWindows ? $"/C {command}" : $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Error executing command: {error}");
            }
        }

        /// <summary>
        /// Método separado para extraer la versión del AndroidManifest.xml
        /// </summary>
        /// <param name="manifestPath"></param>
        /// <param name="tempPath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static async Task<string?> ExtractVersionFromManifestAsync(string manifestPath, string tempPath)
        {
            string? version = null;
            // Validar si Java 8 o superior está instalado
            var javaVersion = await GetJavaVersionAsync();
            if (javaVersion == null || (!javaVersion.StartsWith("1.8") && !int.TryParse(javaVersion.Split('.')[0], out var major) && major < 8))
            {
                throw new InvalidOperationException("Java 8 or higher is not installed.");
            }

            // Generar archivo decodificado usando AXMLPrinter2.jar
            string decodedManifestPath = Path.Combine(tempPath, "ManifestDecode.xml");

            // Ruta del archivo JAR en el directorio raíz del proyecto
            string jarPath = Path.Combine(Directory.GetCurrentDirectory(), "Shared", "Lib", "AXMLPrinter2.jar");

            string command = $"java -jar \"{jarPath}\" \"{manifestPath}\" > \"{decodedManifestPath}\"";
            await ExecuteCommandAsync(command);

            if (File.Exists(decodedManifestPath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(decodedManifestPath);

                XmlNode manifestNode = xmlDoc.SelectSingleNode("/manifest")!;
                if (manifestNode != null)
                {
                    version = manifestNode.Attributes?["android:versionName"]?.Value;
                }
            }

            return version;
        }

        /// <summary>
        /// Sanitiza el nombre del archivo para eliminar caracteres no válidos.
        /// </summary>
        private static string SanitizeFileName(string fileName)
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
