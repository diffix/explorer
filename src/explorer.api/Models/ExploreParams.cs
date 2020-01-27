namespace Explorer.Api.Models
{
    using System.ComponentModel.DataAnnotations;

    public class ExploreParams
    {
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