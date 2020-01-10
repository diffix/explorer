using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Explorer.Api.Models
{
    public class ExploreParams
    {
        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string TableName {get; set; }

        [Required]
        public string ColumnName {get; set; }
    }
}