using System;
using System.Text.Json.Serialization;

namespace BlsApi.Models
{
    public class Book
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("title")]
        public required string Title { get; set; }
        
        [JsonPropertyName("author")]
        public required string Author { get; set; }
        
        [JsonPropertyName("isbn")]
        public required string ISBN { get; set; }
        
        [JsonPropertyName("isCheckedOut")]
        public bool IsCheckedOut { get; set; } = false;
    }
}
