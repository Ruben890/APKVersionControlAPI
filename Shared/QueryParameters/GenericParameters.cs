namespace APKVersionControlAPI.Shared.QueryParameters
{
    public class GenericParameters
    {
        public bool? IsDownload { get; set; } = false;
        public string? Version { get; set; } = null!;
        public string? Name { get; set; } = null!;
    }
}
