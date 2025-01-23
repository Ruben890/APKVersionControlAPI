using APKVersionControlAPI.Interfaces;
using Hangfire;
using System.Globalization;

namespace APKVersionControlAPI.Infrastructure.Jobs
{
    public class ApkFilesJobs : IBackgroundJob
    {
        private readonly IBackgroundJobClient _backgroundJob;

        public ApkFilesJobs(IBackgroundJobClient backgroundJob)
        {
            _backgroundJob = backgroundJob;
        }


        public void DeleteFilesOlderThan2Months()
        {
            var baseFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

            if (!Directory.Exists(baseFolderPath))
            {
                throw new DirectoryNotFoundException($"The directory {baseFolderPath} does not exist.");
            }

            // Obtener todos los archivos APK en el directorio sin cargar todos a memoria
            var apkFilePaths = Directory.EnumerateFiles(baseFolderPath, "*.apk");

            // Fecha actual menos dos meses
            var twoMonthsAgo = DateTime.Now.AddMonths(-2);

            foreach (var filePath in apkFilePaths)
            {
                try
                {
                    // Extraer la fecha del nombre del archivo
                    var fileName = Path.GetFileNameWithoutExtension(filePath); // Obtiene el nombre sin extensión
                    var dateString = fileName.Split(new[] { "--" }, StringSplitOptions.None).Last(); // Extrae la fecha después de "--"
                    var fileDate = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture); // Parsea la fecha en formato "yyyyMMdd"

                    // Si la fecha del archivo es anterior a dos meses, eliminarlo
                    if (fileDate < twoMonthsAgo)
                    {
                        File.Delete(filePath);
                        Console.WriteLine($"Deleted: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar errores (por ejemplo, formato de fecha incorrecto)
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            }
        }

        public void RegisterRecurringJobs()
        {
            RecurringJob.AddOrUpdate(
                 "DeleteFilesOlderThan2Months",
                 () => DeleteFilesOlderThan2Months(),
                 Cron.Monthly); // Ejecutar mensualmente
        }
    }
}
