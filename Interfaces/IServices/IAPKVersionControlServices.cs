
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IServices
{
    public interface IAPKVersionControlServices
    {
        void DeleteApkFile(GenericParameters parameters);
        string FindFileForDownload(GenericParameters parameters);
        Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters);
        Task<string?> UploadApkFile(IFormFile file, string? client = null);
    }
}
