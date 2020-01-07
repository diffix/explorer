using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Explorer.Models
{
    public class ExploreParams
    {
        [JsonPropertyName("api_key")]
        [Required]
        public string ApiKey { get; set; }

        [JsonPropertyName("table_name")]
        [Required]
        public string TableName {get; set; }

        [JsonPropertyName("column_name")]
        [Required]
        public string ColumnName {get; set; }
    }
}