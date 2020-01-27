namespace Explorer.Api.Models
{
    public class NotImplementedError
    {
        public string Status { get; set; } = "error";

        public string Description { get; set; } = "Not implemented";

        public object Data { get; set; } = string.Empty;
    }
}