using Microsoft.AspNetCore.Mvc;
using BlsApi.Models;
using BlsApi.Services;
using BlsApi.Utils;

namespace BlsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly ILogger<BooksController> _logger;

        public BooksController(IBookService bookService, ILogger<BooksController> logger)
        {
            _bookService = bookService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Book>>> ListBooks()
        {
            try
            {
                var books = await _bookService.ListBooksAsync();
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books");
                return StatusCode(500, new { error = "Could not retrieve books" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Book>> AddBook([FromBody] Book book)
        {
            try
            {
                var (isValid, errors) = ValidationHelper.Validate(book);
                if (!isValid)
                {
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { errors });
                }

                var createdBook = await _bookService.AddBookAsync(book);
                return CreatedAtAction(nameof(ListBooks), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding book");
                return StatusCode(500, new { error = "Could not add book" });
            }
        }

        [HttpPost("{id}/checkout")]
        public async Task<ActionResult> CheckoutBook(string id)
        {
            try
            {
                var success = await _bookService.CheckoutBookAsync(id);
                if (!success)
                {
                    return BadRequest(new { error = "Book is already checked out or does not exist" });
                }

                return Ok(new { message = "Book checked out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out book {BookId}", id);
                return StatusCode(500, new { error = "Could not checkout book" });
            }
        }

        [HttpPost("{id}/return")]
        public async Task<ActionResult> ReturnBook(string id)
        {
            try
            {
                var success = await _bookService.ReturnBookAsync(id);
                if (!success)
                {
                    return BadRequest(new { error = "Book is not checked out or does not exist" });
                }

                return Ok(new { message = "Book returned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning book {BookId}", id);
                return StatusCode(500, new { error = "Could not return book" });
            }
        }
    }
}

