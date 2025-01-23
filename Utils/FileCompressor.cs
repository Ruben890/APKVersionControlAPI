using System;
using System.IO;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams;

namespace APKVersionControlAPI.Ultils
{
    public static class FileCompressor
    {
        // Tamaño máximo para cargar en memoria (100 MB)
        private const long MaxMemorySize = 100 * 1024 * 1024; // 100 MB

        /// <summary>
        /// Comprime un archivo usando LZ4.
        /// </summary>
        /// <param name="sourceFilePath">Ruta del archivo a comprimir.</param>
        /// <param name="destinationFilePath">Ruta del archivo comprimido de salida.</param>
        /// <returns>True si la compresión fue exitosa, False en caso contrario.</returns>
        public static bool CompressFile(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                // Verifica si el archivo fuente existe
                if (!File.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException("El archivo fuente no existe.");
                }

                // Obtiene el tamaño del archivo
                long fileSize = new FileInfo(sourceFilePath).Length;

                // Decide si cargar en memoria o usar el método tradicional
                if (fileSize <= MaxMemorySize)
                {
                    // Método en memoria (rápido para archivos pequeños)
                    CompressInMemory(sourceFilePath, destinationFilePath);
                }
                else
                {
                    // Método tradicional (para archivos grandes)
                    CompressUsingFileStream(sourceFilePath, destinationFilePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine($"Error al comprimir el archivo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Comprime un archivo en memoria usando LZ4 y lo guarda en disco.
        /// </summary>
        private static void CompressInMemory(string sourceFilePath, string destinationFilePath)
        {
            // Carga el archivo en memoria
            byte[] fileBytes = File.ReadAllBytes(sourceFilePath);

            // Comprime el archivo en memoria usando LZ4
            using (var memoryStream = new MemoryStream())
            {
                using (var lz4Stream = LZ4Stream.Encode(memoryStream))
                {
                    lz4Stream.Write(fileBytes, 0, fileBytes.Length);
                }

                // Escribe el archivo comprimido en disco
                File.WriteAllBytes(destinationFilePath, memoryStream.ToArray());
            }
        }

        /// <summary>
        /// Comprime un archivo usando FileStream y LZ4 (para archivos grandes).
        /// </summary>
        private static void CompressUsingFileStream(string sourceFilePath, string destinationFilePath)
        {
            // Abre el archivo fuente
            using (var inputStream = File.OpenRead(sourceFilePath))
            // Crea el archivo comprimido
            using (var outputStream = File.Create(destinationFilePath))
            // Aplica compresión LZ4
            using (var lz4Stream = LZ4Stream.Encode(outputStream))
            {
                inputStream.CopyTo(lz4Stream);
            }
        }

        /// <summary>
        /// Descomprime un archivo comprimido con LZ4.
        /// </summary>
        /// <param name="sourceFilePath">Ruta del archivo comprimido.</param>
        /// <param name="destinationFilePath">Ruta del archivo descomprimido de salida.</param>
        /// <returns>True si la descompresión fue exitosa, False en caso contrario.</returns>
        public static bool DecompressFile(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                // Verifica si el archivo comprimido existe
                if (!File.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException("El archivo comprimido no existe.");
                }

                // Obtiene el tamaño del archivo comprimido
                long compressedSize = new FileInfo(sourceFilePath).Length;

                // Decide si cargar en memoria o usar el método tradicional
                if (compressedSize <= MaxMemorySize)
                {
                    // Método en memoria (rápido para archivos pequeños)
                    DecompressInMemory(sourceFilePath, destinationFilePath);
                }
                else
                {
                    // Método tradicional (para archivos grandes)
                    DecompressUsingFileStream(sourceFilePath, destinationFilePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine($"Error al descomprimir el archivo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Descomprime un archivo en memoria usando LZ4.
        /// </summary>
        private static void DecompressInMemory(string sourceFilePath, string destinationFilePath)
        {
            // Carga el archivo comprimido en memoria
            byte[] compressedBytes = File.ReadAllBytes(sourceFilePath);

            // Descomprime el archivo en memoria usando LZ4
            using (var memoryStream = new MemoryStream(compressedBytes))
            using (var lz4Stream = LZ4Stream.Decode(memoryStream))
            using (var outputStream = new MemoryStream())
            {
                lz4Stream.CopyTo(outputStream);

                // Escribe el archivo descomprimido en disco
                File.WriteAllBytes(destinationFilePath, outputStream.ToArray());
            }
        }

        /// <summary>
        /// Descomprime un archivo usando FileStream y LZ4 (para archivos grandes).
        /// </summary>
        private static void DecompressUsingFileStream(string sourceFilePath, string destinationFilePath)
        {
            // Abre el archivo comprimido
            using (var inputStream = File.OpenRead(sourceFilePath))
            // Crea el archivo descomprimido
            using (var outputStream = File.Create(destinationFilePath))
            // Aplica descompresión LZ4
            using (var lz4Stream = LZ4Stream.Decode(inputStream))
            {
                lz4Stream.CopyTo(outputStream);
            }
        }
    }
}