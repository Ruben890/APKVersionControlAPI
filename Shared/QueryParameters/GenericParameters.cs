﻿namespace APKVersionControlAPI.Shared.QueryParameters
{
    public class GenericParameters
    {
        public string? Version { get; set; } = null!;
        public string? Name { get; set; } = null!;

        private string? _client = null!;
        public string? Client
        {
            get => _client;
            set => _client = value?.ToLower();
        }
    }
}
