namespace Explorer.Api.Models
{
    using System.ComponentModel.DataAnnotations;

    public class ExploreParams
    {
        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string TableName { get; set; }

        [Required]
        public string ColumnName { get; set; }

        [Required]
        public string DataSourceName { get; set; }
    }
}