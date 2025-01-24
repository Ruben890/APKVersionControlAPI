﻿using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using APKVersionControlAPI.Shared.Utils;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;


namespace APKVersionControlAPI.Infrastructure.Repository
{
    public class ApkProcessor : IApkProcessor
    {
        public async Task<List<ApkFileDto>> GetAllApkAsync(GenericParameters parameters)
        {
            var baseFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

            if (!Directory.Exists(baseFolderPath))
            {
                throw new DirectoryNotFoundException($"The directory {baseFolderPath} does not exist.");
            }

            if (!string.IsNullOrWhiteSpace(parameters.Client))
            {
                baseFolderPath = Path.Combine(baseFolderPath, parameters.Client);
            }

            // Obtener todos los archivos APK en el directorio sin cargar todos a memoria
            var apkFilePaths = Directory.EnumerateFiles(baseFolderPath, "*.apk");

            var apkFiles = await ProcessApkFilesAsync(apkFilePaths);

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

        public async Task<ApkFileDto> ExtractApkInfoAsync(string? apkFilePath, Stream? apkFileStream = null)
        {
            if (apkFilePath == null && apkFileStream == null)
            {
                throw new ArgumentException("You must provide a file path or data stream.");
            }

            if (apkFileStream != null && !apkFileStream.CanRead)
            {
                throw new ArgumentException("The data stream is invalid.", nameof(apkFileStream));
            }

            // Obtener el nombre de la carpeta
            string? folderName = null;
            if (!string.IsNullOrWhiteSpace(apkFilePath))
            {
                folderName = Path.GetFileName(Path.GetDirectoryName(apkFilePath));
            }

            var apkFileDto = new ApkFileDto
            {
                Name = apkFilePath != null ? Path.GetFileNameWithoutExtension(apkFilePath).Split('-')[0] : "ArchivoDesconocido",
                Client = string.Equals(folderName, "Files", StringComparison.OrdinalIgnoreCase) ? null : folderName,
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
                string tempApkPath = Path.Combine(tempPath, "temp.apk");

                if (apkFilePath != null)
                {
                    // Copiar el archivo APK al directorio temporal
                    File.Copy(apkFilePath, tempApkPath, true);
                }
                else if (apkFileStream != null)
                {
                    // Guardar el stream en un archivo temporal
                    await using (var fileStream = new FileStream(tempApkPath, FileMode.Create, FileAccess.Write))
                    {
                        await apkFileStream.CopyToAsync(fileStream);
                    }
                }

                // Descomprimir el archivo APK
                await Task.Run(() => APKExtractor.ExtractAPK(tempApkPath, tempPath));

                // Buscar el archivo AndroidManifest.xml en el directorio descomprimido
                string manifestPath = Path.Combine(tempPath, "AndroidManifest.xml");

                if (File.Exists(manifestPath))
                {
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

        private async Task<List<ApkFileDto>> ProcessApkFilesAsync(IEnumerable<string> apkFilePaths)
        {
            var apkFileDtos = new List<ApkFileDto>();

            await Parallel.ForEachAsync(apkFilePaths, async (apkFilePath, cancellationToken) =>
            {
                try
                {
                    var apkInfo = await ExtractApkInfoAsync(apkFilePath);
                    lock (apkFileDtos)
                    {
                        apkFileDtos.Add(apkInfo);
                    }
                }
                catch (Exception ex)
                {
                    // Log de error o manejo adecuado en caso de que falte algún archivo o haya un problema
                    Console.WriteLine($"Error procesando {apkFilePath}: {ex.Message}");
                }
            });

            return apkFileDtos;
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
    }
}