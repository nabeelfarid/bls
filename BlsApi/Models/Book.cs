using System;

namespace BlsApi.Models
{
    public class Book
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Author { get; set; }
        public required string ISBN { get; set; }
        public bool IsCheckedOut { get; set; } = false;
    }
}
