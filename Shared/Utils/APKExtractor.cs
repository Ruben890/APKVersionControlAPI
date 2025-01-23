using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace APKVersionControlAPI.Shared.Utils
{
    public class APKExtractor
    {
        private const long MaxInMemorySize = 100 * 1024 * 1024; // 100 MB

        public static void ExtractAPK(string apkFilePath, string destinationFolder)
        {
            try
            {
                // Verifica si el archivo APK existe
                if (!File.Exists(apkFilePath))
                {
                    throw new FileNotFoundException($"El archivo APK no existe: {apkFilePath}");
                }

                // Verifica si la carpeta de destino existe; si no, la crea
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // Obtiene el tamaño del archivo
                long fileSize = new FileInfo(apkFilePath).Length;

                // Decide si descomprimir en memoria o en disco
                if (fileSize <= MaxInMemorySize)
                {
                    Console.WriteLine("Descomprimiendo en memoria...");
                    ExtractInMemory(apkFilePath, destinationFolder);
                }
                else
                {
                    Console.WriteLine("Descomprimiendo en disco...");
                    ExtractOnDisk(apkFilePath, destinationFolder);
                }

                Console.WriteLine($"APK descomprimido exitosamente en: {destinationFolder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al descomprimir el APK: {ex.Message}");
            }
        }

        private static void ExtractInMemory(string apkFilePath, string destinationFolder)
        {
            // Carga todo el archivo APK en memoria
            byte[] apkBytes = File.ReadAllBytes(apkFilePath);

            // Usa un MemoryStream para trabajar con los datos en memoria
            using (var memoryStream = new MemoryStream(apkBytes))
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                ExtractZipEntries(zip, destinationFolder);
            }
        }

        private static void ExtractOnDisk(string apkFilePath, string destinationFolder)
        {
            // Descomprime directamente desde el archivo en disco
            using (var zip = ZipFile.OpenRead(apkFilePath))
            {
                ExtractZipEntries(zip, destinationFolder);
            }
        }

        private static void ExtractZipEntries(ZipArchive zip, string destinationFolder)
        {
            byte[] buffer = new byte[1024 * 1024]; // Buffer de 1 MB

            Parallel.ForEach(zip.Entries, entry =>
            {
                string destinationPath = Path.Combine(destinationFolder, entry.FullName);

                // Crea la carpeta si no existe
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                // Si es un directorio, continúa
                if (entry.Length == 0)
                {
                    return;
                }

                // Extrae el archivo
                using (var entryStream = entry.Open())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    int bytesRead;
                    while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
            });
        }
    }
}