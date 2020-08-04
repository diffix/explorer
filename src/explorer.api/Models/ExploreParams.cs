namespace Explorer.Api.Models
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;

    public class ExploreParams
    {
        private static readonly ImmutableArray<string> EmptyColumns = ImmutableArray.Create<string>();

        private string apiUrl = string.Empty;

        [Required]
        public string ApiUrl
        {
            get => apiUrl;
            set => apiUrl = value.EndsWith("/", ignoreCase: false, CultureInfo.InvariantCulture) ? value : $"{value}/";
        }

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string DataSource { get; set; } = string.Empty;

        [Required]
        public string Table { get; set; } = string.Empty;

        [Required]
        public ImmutableArray<string> Columns { get; set; } = EmptyColumns;
    }
}