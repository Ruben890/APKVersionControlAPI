using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IRepository
{
    public interface IFIleRepository
    {
        List<ApkFileDto> GetAllApk(GenericParameters parameters);
    }
}
