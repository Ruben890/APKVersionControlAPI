using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace APKVersionControlAPI.Services
{
    public class APKVersionControlServices : IAPKVersionControlServices
    {
        public async Task<string?> UploadApkFile(IFormFile file)
        {
            try
            {
                // Check if the file has content
                if (file.Length <= 0)
                {
                    throw new ArgumentException("The file is empty.");
                }

                // Validate that the file is an APK
                if (!string.Equals(Path.GetExtension(file.FileName), ".apk", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("The file is not a valid APK.");
                }

                // Check that only one file has been sent
                if (file == null)
                {
                    throw new ArgumentException("No file has been received.");
                }

                // Optional: You could validate the file name or size if needed.
                // If you want to save the file, you can do it in the following step:
                // var path = Path.Combine("destination_path", file.FileName);
                // using (var stream = new FileStream(path, FileMode.Create))
                // {
                //     await file.CopyToAsync(stream);
                // }

                return "APK file received successfully.";
            }

            catch (Exception)
            {
                throw;
            }
        }

    }
}
