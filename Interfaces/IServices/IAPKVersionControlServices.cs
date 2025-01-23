
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IServices
{
    public interface IAPKVersionControlServices
    {
        string FindFileForDownload(GenericParameters parameters);
        Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters);
        Task<string?> UploadApkFile(IFormFile file, string Client);
    }
}
