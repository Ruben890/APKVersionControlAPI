using APKVersionControlAPI.Interfaces;
using APKVersionControlAPI.Interfaces.IRepository;
using Hangfire;
using System.Globalization;

namespace APKVersionControlAPI.Infrastructure.Jobs
{
    public class ApkFilesJobs : IBackgroundJob
    {
        private readonly IBackgroundJobClient _backgroundJob;

        private readonly IApkFileRepository _repository;

        public ApkFilesJobs(IBackgroundJobClient backgroundJob, IApkFileRepository repository)
        {
            _backgroundJob = backgroundJob;
            _repository = repository;
        }

        public async Task DeleteFilesOlderThan2Months()
        {
            var baseFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

            if (!Directory.Exists(baseFolderPath))
            {
                throw new DirectoryNotFoundException($"The directory {baseFolderPath} does not exist.");
            }

            // Obtener todos los archivos APK almacenados en la base de datos
            var apkFiles = await _repository.GetApkFileAll();

            // Fecha límite (dos meses atrás)
            var twoMonthsAgo = DateTime.Now.AddMonths(-2);

            foreach (var apkFile in apkFiles)
            {
                try
                {
                    if (apkFile.FilePath == null) continue;

                    var filePath = Path.Combine(baseFolderPath, apkFile.FilePath);

                    if (!File.Exists(filePath)) continue;


                    if (apkFile.CreatedAt < twoMonthsAgo)
                    {   
                        
                        File.Delete(filePath);
                        _repository.Delete(apkFile);
                        await _repository.SaveAsync();
                        Console.WriteLine($"Deleted: {filePath}");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {apkFile.FilePath}: {ex.Message}");
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
