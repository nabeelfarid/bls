using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlsApi.Models
{
    public class Book
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("title")]
        [Required(ErrorMessage = "Title is required")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 500 characters")]
        public required string Title { get; set; }
        
        [JsonPropertyName("author")]
        [Required(ErrorMessage = "Author is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Author must be between 1 and 200 characters")]
        public required string Author { get; set; }
        
        [JsonPropertyName("isbn")]
        [Required(ErrorMessage = "ISBN is required")]
        public required string ISBN { get; set; }
        
        [JsonPropertyName("isCheckedOut")]
        public bool IsCheckedOut { get; set; } = false;
    }
}
