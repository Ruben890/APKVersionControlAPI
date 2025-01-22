
using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
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
                throw new DirectoryNotFoundException($"El directorio {directoryPath} no existe.");
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
                apkFiles = apkFiles.Where(x => x.Name!.Equals(parameters.Name, StringComparison.OrdinalIgnoreCase)).ToList();
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
                throw new ArgumentException("Debes proporcionar una ruta de archivo o un flujo de datos.");

            if (apkFileStream != null && !apkFileStream.CanRead)
                throw new ArgumentException("El flujo de datos no es válido.", nameof(apkFileStream));

            var apkFileDto = new ApkFileDto
            {
                Name = apkFilePath != null ? Path.GetFileNameWithoutExtension(apkFilePath): "ArchivoDesconocido",
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
                    // Descomprimir el archivo APK desde la ruta
                    ZipFile.ExtractToDirectory(apkFilePath, tempPath);
                }
                else if (apkFileStream != null)
                {
                    // Crear un archivo temporal a partir del flujo de datos
                    string tempApkPath = Path.Combine(tempPath, "temp.apk");
                    using (var fileStream = new FileStream(tempApkPath, FileMode.Create, FileAccess.Write))
                    {
                        apkFileStream.CopyTo(fileStream);
                    }

                    // Descomprimir el archivo APK temporal
                    ZipFile.ExtractToDirectory(tempApkPath, tempPath);
                }

                // Ruta al archivo AndroidManifest.xml extraído
                string manifestPath = Path.Combine(tempPath, "AndroidManifest.xml");

                if (File.Exists(manifestPath))
                {
                    // Leer y analizar el archivo AndroidManifest.xml
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(manifestPath);

                    // Extraer la versión de la aplicación
                    XmlNode manifestNode = xmlDoc.SelectSingleNode("/manifest")!;
                    if (manifestNode != null)
                    {
                        apkFileDto.Version = manifestNode.Attributes!["android:versionName"]?.Value;
                    }
                }
            }
            finally
            {
                // Eliminar el directorio temporal y su contenido
                Directory.Delete(tempPath, true);
            }

            return apkFileDto;
        }

    }
}
