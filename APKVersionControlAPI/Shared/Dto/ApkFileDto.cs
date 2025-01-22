using Microsoft.AspNetCore.Http;
using System;

namespace APKVersionControlAPI.Shared.Dto
{
    public class ApkFileDto
    {
        public IFormFile? File { get; set; }
        public string? Name { get; set; } = null!;
        public string? FileUrl { get; set; }
        public decimal? Size { get; set; }
        public decimal? Version { get; set; } = 0m!;
        public DateTime? CreatedAt { get; set; }
        public bool? IsCurrentVersion { get; set; } = null!;
        public bool? IsPreviousVersion { get; set; } = null!;
    }
}
