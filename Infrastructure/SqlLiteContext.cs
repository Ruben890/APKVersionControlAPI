using APKVersionControlAPI.Entity;
using Microsoft.EntityFrameworkCore;

namespace APKVersionControlAPI.Infrastructure
{
    public class SqlLiteContext : DbContext
    {

        public virtual DbSet<ApkFile> ApkFiles { get; set; } = null!;

        public string DbPath { get; }

        public SqlLiteContext(DbContextOptions<SqlLiteContext> options) : base(options) {

            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "apkVersing.db");
        }


        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}
