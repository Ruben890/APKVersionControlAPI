using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IRepository
{
    public interface IApkProcessor
    {
        Task<ApkFileDto> ExtractApkInfoAsync(string? apkFilePath, Stream? apkFileStream = null);
        Task<List<ApkFileDto>> GetAllApkAsync(GenericParameters parameters);
    }
}
