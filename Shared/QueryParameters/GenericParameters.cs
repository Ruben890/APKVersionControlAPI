namespace APKVersionControlAPI.Shared.QueryParameters
{
    public class GenericParameters
    {
        public bool IsDownload {  get; set; } = false;
        public decimal? Version { get; set; } = null!;
    }
}
