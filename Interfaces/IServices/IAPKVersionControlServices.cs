
using APKVersionControlAPI.Entity;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IServices
{
    public interface IAPKVersionControlServices
    {
        Task DeleteApkFile(int Id);
        Task<string> FindFileForDownload(DownloadParameters parameters);
        Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters);
        Task<string?> UploadApkFile(IFormFile file, string? client = null);
    }
}
