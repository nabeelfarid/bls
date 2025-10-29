using BlsApi.Models;

namespace BlsApi.Services
{
    public interface IBookService
    {
        Task<Book> AddBookAsync(Book book);
        Task<List<Book>> ListBooksAsync();
        Task<bool> CheckoutBookAsync(string bookId);
        Task<bool> ReturnBookAsync(string bookId);
    }
}

