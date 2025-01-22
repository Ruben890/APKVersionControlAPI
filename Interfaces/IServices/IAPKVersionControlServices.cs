
namespace APKVersionControlAPI.Interfaces.IServices
{
    public interface IAPKVersionControlServices
    {
        Task<string?> UploadApkFile(IFormFile file);
    }
}
