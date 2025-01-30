using APKVersionControlAPI.Entity;
using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using APKVersionControlAPI.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;


namespace APKVersionControlAPI.Infrastructure.Repository
{
    public class ApkFileRepository : IApkFileRepository
    {
        private readonly SqlLiteContext _context;


        public ApkFileRepository(SqlLiteContext context)
        {
            _context = context;
        }

        public async Task<List<ApkFileDto>> GetAllApkAsync(GenericParameters parameters)
        {
            var query = _context.ApkFiles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(parameters.Version))
            {
                query = query.Where(x => x.Version != null && x.Version == parameters.Version);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Client))
            {
                query = query.Where(x => x.Client != null && EF.Functions.Like(x.Client, $"%{parameters.Client}%"));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Name))
            {
                query = query.Where(x => x.Name != null && EF.Functions.Like(x.Name, $"%{parameters.Name}%"));
            }

            var sortedApkFiles = await query
                .OrderByDescending(x => x.Version)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new ApkFileDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Size = x.Size,
                    Version = x.Version,
                    CreatedAt = x.CreatedAt,
                    Client = x.Client
                })
                .ToListAsync();

            if (sortedApkFiles.Any())
            {
                sortedApkFiles.First().IsCurrentVersion = true;
                if (sortedApkFiles.Count > 1)
                {
                    sortedApkFiles[1].IsPreviousVersion = true;
                }
            }

            return sortedApkFiles;
        }


        public async Task<ApkFile> GetApkFileById(int Id) =>
            await _context.ApkFiles.Where(x => x.Id.Equals(Id)).FirstOrDefaultAsync();

        public async Task<List<ApkFile>> GetApkFileAll() =>
                await _context.ApkFiles.Select(x => new ApkFile()).ToListAsync();

        public void Delete(ApkFile file) => _context.ApkFiles.Remove(file);
        public async Task Insert(ApkFile file) => await _context.ApkFiles.AddAsync(file);
        public async Task SaveAsync() => await _context.SaveChangesAsync();

        public void Beggin() => _context.Database.BeginTransaction();

        public void Commit() => _context.Database.CommitTransaction();

        public void Roolback() => _context.Database.RollbackTransaction();


    }
}