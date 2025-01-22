using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IRepository
{
    public interface IApkProcessor
    {
        ApkFileDto ExtractApkInfo(string? apkFilePath, Stream? apkFileStream = null);
        List<ApkFileDto> GetAllApk(GenericParameters parameters);
    }
}
