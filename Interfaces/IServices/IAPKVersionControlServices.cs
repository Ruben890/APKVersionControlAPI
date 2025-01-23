
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IServices
{
    public interface IAPKVersionControlServices
    {
        Task<IEnumerable<ApkFileDto>> GetApkFiles(GenericParameters parameters);
        Task<string?> UploadApkFile(IFormFile file);
    }
}
