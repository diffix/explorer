using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Explorer.Models
{
    public class ExploreParams
    {
        [Required]
        [Display(Name = "api_key")]
        [JsonPropertyName("api_key")]
        public string ApiKey { get; set; }

        [Required]
        [Display(Name = "table_name")]
        [JsonPropertyName("table_name")]
        public string TableName {get; set; }

        [Required]
        [Display(Name = "column_name")]
        [JsonPropertyName("column_name")]
        public string ColumnName {get; set; }
    }
}