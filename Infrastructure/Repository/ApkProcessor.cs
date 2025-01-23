
using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace APKVersionControlAPI.Infrastructure.Repository
{
    public class ApkProcessor : IApkProcessor
    {

        public List<ApkFileDto> GetAllApk(GenericParameters parameters)
        {
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");
            }

            // Obtener todos los archivos APK en el directorio sin cargar todos a memoria
            var apkFilePaths = Directory.EnumerateFiles(directoryPath, "*.apk");

            var apkFiles = ProcessApkFiles(apkFilePaths);

            // Filtrar por versión solo si se ha proporcionado
            if (!string.IsNullOrWhiteSpace(parameters.Version))
            {
                apkFiles = apkFiles.Where(x => x.Version!.Equals(parameters.Version)).ToList();
            }

            // Filtrar por nombre solo si se ha proporcionado
            if (!string.IsNullOrWhiteSpace(parameters.Name))
            {
                apkFiles = apkFiles.Where(x => x.Name!.Contains(parameters.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Ordenar por versión y fecha de creación (descendente)
            var sortedApkFiles = apkFiles
                .OrderByDescending(x => new Version(x.Version ?? "0.0.0"))  // Comparar las versiones
                .ThenByDescending(x => x.CreatedAt)  // Ordenar por fecha de creación
                .ToList();

            // Marcar la versión actual y la anterior
            if (sortedApkFiles.Count > 0)
            {
                sortedApkFiles[0].IsCurrentVersion = true;
                if (sortedApkFiles.Count > 1)
                {
                    sortedApkFiles[1].IsPreviousVersion = true;
                }
            }

            return sortedApkFiles;
        }

        private List<ApkFileDto> ProcessApkFiles(IEnumerable<string> apkFilePaths)
        {
            var apkFileDtos = new List<ApkFileDto>();

            foreach (var apkFilePath in apkFilePaths)
            {
                try
                {
                    var apkInfo = ExtractApkInfo(apkFilePath);
                    apkFileDtos.Add(apkInfo);
                }
                catch (Exception ex)
                {
                    // Log de error o manejo adecuado en caso de que falte algún archivo o haya un problema
                    Console.WriteLine($"Error procesando {apkFilePath}: {ex.Message}");
                }
            }

            return apkFileDtos;
        }

        public ApkFileDto ExtractApkInfo(string? apkFilePath, Stream? apkFileStream = null)
        {
            if (apkFilePath == null && apkFileStream == null)
                throw new ArgumentException("You must provide a file path or data stream.");

            if (apkFileStream != null && !apkFileStream.CanRead)
                throw new ArgumentException("The data stream is invalid.", nameof(apkFileStream));

            var apkFileDto = new ApkFileDto
            {
                Name = apkFilePath != null ? Path.GetFileNameWithoutExtension(apkFilePath).Split('-')[0] : "ArchivoDesconocido",
                Size = apkFilePath != null
                    ? Math.Round(new FileInfo(apkFilePath).Length / (1024.0 * 1024.0), 2)
                    : Math.Round(apkFileStream!.Length / (1024.0 * 1024.0), 2),
                CreatedAt = apkFilePath != null ? File.GetCreationTime(apkFilePath) : DateTime.Now
            };

            // Crear un directorio temporal para extraer el contenido del APK
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                if (apkFilePath != null)
                {
                    ZipFile.ExtractToDirectory(apkFilePath, tempPath);
                }
                else if (apkFileStream != null)
                {
                    string tempApkPath = Path.Combine(tempPath, "temp.apk");
                    using (var fileStream = new FileStream(tempApkPath, FileMode.Create, FileAccess.Write))
                    {
                        apkFileStream.CopyTo(fileStream);
                    }
                    ZipFile.ExtractToDirectory(tempApkPath, tempPath);
                }

                string manifestPath = Path.Combine(tempPath, "AndroidManifest.xml");

                if (File.Exists(manifestPath))
                {
                    // Validar si Java 8 o superior está instalado
                    var javaVersion = GetJavaVersion();
                    if (javaVersion == null || !javaVersion.StartsWith("1.8") && !int.TryParse(javaVersion.Split('.')[0], out var major) && major < 8)
                    {
                        throw new InvalidOperationException("Java 8 or higher is not installed.");
                    }

                    // Generar archivo decodificado usando AXMLPrinter2.jar
                    string decodedManifestPath = Path.Combine(tempPath, "ManifestDecode.xml");

                    // Ruta del archivo JAR en el directorio raíz del proyecto
                    string jarPath = Path.Combine(Directory.GetCurrentDirectory(), "Lib", "AXMLPrinter2.jar");

                    string command = $"java -jar \"{jarPath}\" \"{manifestPath}\" > \"{decodedManifestPath}\"";
                    ExecuteCommand(command);

                    if (File.Exists(decodedManifestPath))
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(decodedManifestPath);

                        XmlNode manifestNode = xmlDoc.SelectSingleNode("/manifest")!;
                        if (manifestNode != null)
                        {
                            apkFileDto.Version = manifestNode.Attributes?["android:versionName"]?.Value;
                        }
                    }
                }
            }
            finally
            {
                // Limpiar el directorio temporal
                Directory.Delete(tempPath, true);
            }

            return apkFileDto;
        }

        private static string? GetJavaVersion()
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
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Extraer la versión de Java
                var match = Regex.Match(output, @"\d+\.\d+\.\d+");
                return match.Success ? match.Value : null;
            }
            catch
            {
                return null;
            }
        }

        private static void ExecuteCommand(string command)
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
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Error executing command: {error}");
            }
        }

    }
}
