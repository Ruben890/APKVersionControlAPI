namespace APKVersionControlAPI.Shared.Dto
{
    public class ApkFileDto
    {
        public int? Id { get; set; } = null!;
        public string? Name { get; set; }
        public double? Size { get; set; }
        public string? Version { get; set; }
        public string? Client { get; set; } = null!;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public bool? IsCurrentVersion { get; set; } = false!;
        public bool? IsPreviousVersion { get; set; } = false!;
    }
}
