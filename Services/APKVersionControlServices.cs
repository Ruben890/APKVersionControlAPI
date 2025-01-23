using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
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
                // Check if the file has content
                if (file == null || file.Length <= 0)
                {
                    throw new ArgumentException("The file is empty or has not been received.");
                }

                // Validate that the file is an APK
                if (!string.Equals(Path.GetExtension(file.FileName), ".apk", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("The file is not a valid APK.");
                }

                // Path to save the file within wwwroot/Files
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");

                // Create the 'Files' folder if it does not exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Extract information from the APK file (using a custom service/apkProcessor)
                ApkFileDto? apkInfo = null;
                using (var stream = file.OpenReadStream())
                {
                    // Custom method to extract information from the APK
                    apkInfo = await _apkProcessor.ExtractApkInfoAsync(null, stream);
                }

                if (apkInfo == null)
                {
                    throw new ArgumentException("Could not extract information from the APK.");
                }

                // Full path for the file
                string sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_").Replace(".", "_").ToLower();

                string fileName = $"{sanitizedFileName}-{apkInfo.Version}--{apkInfo.CreatedAt?.ToString("yyyyMMdd")}.apk";
                string filePath = Path.Combine(folderPath, fileName);

                // Save the file to the 'Files' folder
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Compress the APK file to a ZIP file
                string zipFileName = Path.ChangeExtension(fileName, ".zip");
                string zipFilePath = Path.Combine(folderPath, zipFileName);

                using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    zipArchive.CreateEntryFromFile(filePath, fileName);
                }

                // Optionally, delete the original APK file after compression
                File.Delete(filePath);

                return "APK file received, compressed, and saved successfully.";
            }
            catch (Exception ex)
            {
                // Handle any errors
                return $"Error: {ex.Message}";
            }
        }

        public async Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters) =>
            await _apkProcessor.GetAllApkAsync(parameters);


    }
}
