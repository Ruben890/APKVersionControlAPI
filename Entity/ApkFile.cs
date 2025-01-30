using System.ComponentModel.DataAnnotations;

namespace APKVersionControlAPI.Entity
{
    public class ApkFile
    {
        [Key]
        public int? Id { get; set; } = null!;
        public string? Name { get; set; }
        public double? Size { get; set; }
        public string? Version { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public string? Client { get; set; } = null!;
        [Required]
        public string? FilePath {  get; set; }
        public string? FileName { get; set; }
 
    }
}
