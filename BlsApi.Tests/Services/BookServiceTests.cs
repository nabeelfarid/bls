using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BlsApi.Models;
using BlsApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlsApi.Tests.Services
{
    public class BookServiceTests
    {
        private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<BookService>> _mockLogger;
        private readonly BookService _bookService;

        public BookServiceTests()
        {
            _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<BookService>>();

            _mockConfiguration.Setup(x => x["DynamoDB:TableName"]).Returns("BooksTable-Test");

            _bookService = new BookService(_mockDynamoDb.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AddBookAsync_ShouldAddBookSuccessfully()
        {
            // Arrange
            var book = new Book
            {
                Title = "The Pragmatic Programmer",
                Author = "Andrew Hunt",
                ISBN = "978-0201616224"
            };

            _mockDynamoDb
                .Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutItemResponse { HttpStatusCode = HttpStatusCode.OK });

            // Act
            var result = await _bookService.AddBookAsync(book);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Title.Should().Be("The Pragmatic Programmer");
            result.Author.Should().Be("Andrew Hunt");
            result.ISBN.Should().Be("978-0201616224");
            result.IsCheckedOut.Should().BeFalse();

            _mockDynamoDb.Verify(x => x.PutItemAsync(
                It.Is<PutItemRequest>(req =>
                    req.TableName == "BooksTable-Test" &&
                    req.Item.ContainsKey("PK") &&
                    req.Item.ContainsKey("SK") &&
                    req.Item["Title"].S == "The Pragmatic Programmer"
                ),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task ListBooksAsync_ShouldReturnAllBooks()
        {
            // Arrange
            var scanResponse = new ScanResponse
            {
                Items = new List<Dictionary<string, AttributeValue>>
                {
                    new()
                    {
                        { "Id", new AttributeValue { S = "1" } },
                        { "Title", new AttributeValue { S = "Book 1" } },
                        { "Author", new AttributeValue { S = "Author 1" } },
                        { "ISBN", new AttributeValue { S = "ISBN-1" } },
                        { "IsCheckedOut", new AttributeValue { BOOL = false } }
                    },
                    new()
                    {
                        { "Id", new AttributeValue { S = "2" } },
                        { "Title", new AttributeValue { S = "Book 2" } },
                        { "Author", new AttributeValue { S = "Author 2" } },
                        { "ISBN", new AttributeValue { S = "ISBN-2" } },
                        { "IsCheckedOut", new AttributeValue { BOOL = true } }
                    }
                }
            };

            _mockDynamoDb
                .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(scanResponse);

            // Act
            var result = await _bookService.ListBooksAsync();

            // Assert
            result.Should().HaveCount(2);
            result[0].Title.Should().Be("Book 1");
            result[0].IsCheckedOut.Should().BeFalse();
            result[1].Title.Should().Be("Book 2");
            result[1].IsCheckedOut.Should().BeTrue();
        }

        [Fact]
        public async Task CheckoutBookAsync_WhenBookAvailable_ShouldReturnTrue()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateItemResponse { HttpStatusCode = HttpStatusCode.OK });

            // Act
            var result = await _bookService.CheckoutBookAsync("test-id");

            // Assert
            result.Should().BeTrue();

            _mockDynamoDb.Verify(x => x.UpdateItemAsync(
                It.Is<UpdateItemRequest>(req =>
                    req.TableName == "BooksTable-Test" &&
                    req.Key["PK"].S == "BOOK#test-id" &&
                    req.UpdateExpression == "SET IsCheckedOut = :isCheckedOut"
                ),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task CheckoutBookAsync_WhenBookAlreadyCheckedOut_ShouldReturnFalse()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConditionalCheckFailedException("Book already checked out"));

            // Act
            var result = await _bookService.CheckoutBookAsync("test-id");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ReturnBookAsync_WhenBookCheckedOut_ShouldReturnTrue()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateItemResponse { HttpStatusCode = HttpStatusCode.OK });

            // Act
            var result = await _bookService.ReturnBookAsync("test-id");

            // Assert
            result.Should().BeTrue();

            _mockDynamoDb.Verify(x => x.UpdateItemAsync(
                It.Is<UpdateItemRequest>(req =>
                    req.TableName == "BooksTable-Test" &&
                    req.Key["PK"].S == "BOOK#test-id" &&
                    req.UpdateExpression == "SET IsCheckedOut = :isCheckedOut"
                ),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task ReturnBookAsync_WhenBookNotCheckedOut_ShouldReturnFalse()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConditionalCheckFailedException("Book not checked out"));

            // Act
            var result = await _bookService.ReturnBookAsync("test-id");

            // Assert
            result.Should().BeFalse();
        }
    }
}

