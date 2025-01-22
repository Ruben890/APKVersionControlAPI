using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IRepository
{
    public interface IFIleRepository
    {
        ApkFileDto ExtractApkInfo(string? apkFilePath = null, Stream? apkFileStream = null);
        List<ApkFileDto> GetAllApk(GenericParameters parameters);
    }
}
