namespace Explorer.Api.Models
{
    using System.ComponentModel.DataAnnotations;

    public class ExploreParams
    {
        private string apiUrl = string.Empty;

        [Required]
        public string ApiUrl
        {
            get => apiUrl;
            set
            {
                apiUrl = value.EndsWith("/") ? value : $"{value}/";
            }
        }

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string TableName { get; set; } = string.Empty;

        [Required]
        public string ColumnName { get; set; } = string.Empty;

        [Required]
        public string DataSourceName { get; set; } = string.Empty;
    }
}