using Microsoft.AspNetCore.Http;
using System;

namespace APKVersionControlAPI.Shared.Dto
{
    public class ApkFileDto
    {
        public string? Name { get; set; }
        public double? Size { get; set; }
        public string? Version { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsCurrentVersion { get; set; } = false!;
        public bool? IsPreviousVersion { get; set; } = false!;
        public string? Client { get; set; } = null!;
    }
}
