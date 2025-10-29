using System.Collections.Generic;
using System.Threading.Tasks;
using BlsApi.Controllers;
using BlsApi.Models;
using BlsApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlsApi.Tests.Controllers
{
    public class BooksControllerTests
    {
        private readonly Mock<IBookService> _mockBookService;
        private readonly Mock<ILogger<BooksController>> _mockLogger;
        private readonly BooksController _controller;

        public BooksControllerTests()
        {
            _mockBookService = new Mock<IBookService>();
            _mockLogger = new Mock<ILogger<BooksController>>();
            _controller = new BooksController(_mockBookService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ListBooks_ShouldReturnOkWithBooks()
        {
            // Arrange
            var books = new List<Book>
            {
                new() { Id = "1", Title = "Book 1", Author = "Author 1", ISBN = "ISBN-1", IsCheckedOut = false },
                new() { Id = "2", Title = "Book 2", Author = "Author 2", ISBN = "ISBN-2", IsCheckedOut = true }
            };

            _mockBookService.Setup(x => x.ListBooksAsync()).ReturnsAsync(books);

            // Act
            var result = await _controller.ListBooks();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedBooks = okResult.Value.Should().BeAssignableTo<List<Book>>().Subject;
            returnedBooks.Should().HaveCount(2);
        }

        [Fact]
        public async Task AddBook_WithValidBook_ShouldReturnCreated()
        {
            // Arrange
            var book = new Book
            {
                Title = "The Pragmatic Programmer",
                Author = "Andrew Hunt",
                ISBN = "978-0201616224"
            };

            var createdBook = new Book
            {
                Id = "test-id",
                Title = "The Pragmatic Programmer",
                Author = "Andrew Hunt",
                ISBN = "978-0201616224",
                IsCheckedOut = false
            };

            _mockBookService.Setup(x => x.AddBookAsync(It.IsAny<Book>())).ReturnsAsync(createdBook);

            // Act
            var result = await _controller.AddBook(book);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedBook = createdResult.Value.Should().BeAssignableTo<Book>().Subject;
            returnedBook.Id.Should().Be("test-id");
        }

        [Fact]
        public async Task AddBook_WithInvalidBook_ShouldReturnBadRequest()
        {
            // Arrange
            var book = new Book
            {
                Title = "", // Invalid: empty title
                Author = "Andrew Hunt",
                ISBN = "978-0201616224"
            };

            // Act
            var result = await _controller.AddBook(book);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckoutBook_WhenSuccessful_ShouldReturnOk()
        {
            // Arrange
            _mockBookService.Setup(x => x.CheckoutBookAsync("test-id")).ReturnsAsync(true);

            // Act
            var result = await _controller.CheckoutBook("test-id");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CheckoutBook_WhenBookNotAvailable_ShouldReturnBadRequest()
        {
            // Arrange
            _mockBookService.Setup(x => x.CheckoutBookAsync("test-id")).ReturnsAsync(false);

            // Act
            var result = await _controller.CheckoutBook("test-id");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ReturnBook_WhenSuccessful_ShouldReturnOk()
        {
            // Arrange
            _mockBookService.Setup(x => x.ReturnBookAsync("test-id")).ReturnsAsync(true);

            // Act
            var result = await _controller.ReturnBook("test-id");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ReturnBook_WhenBookNotCheckedOut_ShouldReturnBadRequest()
        {
            // Arrange
            _mockBookService.Setup(x => x.ReturnBookAsync("test-id")).ReturnsAsync(false);

            // Act
            var result = await _controller.ReturnBook("test-id");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}

