using APKVersionControlAPI.Entity;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;

namespace APKVersionControlAPI.Interfaces.IRepository
{
    public interface IApkFileRepository
    {
       
        Task<List<ApkFileDto>> GetAllApkAsync(GenericParameters parameters);
        Task<ApkFile> GetApkFileById(int Id);
        Task<List<ApkFile>> GetApkFileAll();
        Task Insert(ApkFile file);
        void Delete(ApkFile file);

        Task SaveAsync();
        void Beggin();
        void Commit();
        void Roolback();
      
    }
}
