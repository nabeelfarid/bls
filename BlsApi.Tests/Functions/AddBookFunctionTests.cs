using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using BlsApi.Functions;
using BlsApi.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace BlsApi.Tests.Functions
{
    public class AddBookFunctionTests
    {
        private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
        private readonly TestLambdaContext _context;

        public AddBookFunctionTests()
        {
            _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            _context = new TestLambdaContext
            {
                FunctionName = "AddBookFunction",
                FunctionVersion = "1"
            };
            
            // Set environment variable for table name
            System.Environment.SetEnvironmentVariable("TABLE_NAME", "Books");
        }

        [Fact]
        public async Task Handler_WithValidBook_ShouldReturn201()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutItemResponse { HttpStatusCode = HttpStatusCode.OK });

            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = JsonSerializer.Serialize(new
                {
                    title = "The Pragmatic Programmer",
                    author = "David Thomas, Andrew Hunt",
                    isbn = "978-0135957059"
                })
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(201);
            
            var book = JsonSerializer.Deserialize<Book>(response.Body);
            book.Should().NotBeNull();
            book!.Title.Should().Be("The Pragmatic Programmer");
            book.Author.Should().Be("David Thomas, Andrew Hunt");
            book.ISBN.Should().Be("978-0135957059");
            book.Id.Should().NotBeNullOrEmpty();
            book.IsCheckedOut.Should().BeFalse();

            _mockDynamoDb.Verify(
                x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handler_WithMissingTitle_ShouldReturn400()
        {
            // Arrange
            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = JsonSerializer.Serialize(new
                {
                    title = "",
                    author = "David Thomas",
                    isbn = "978-0135957059"
                })
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("Title is required");

            _mockDynamoDb.Verify(
                x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handler_WithMissingAuthor_ShouldReturn400()
        {
            // Arrange
            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = JsonSerializer.Serialize(new
                {
                    title = "Test Book",
                    author = "",
                    isbn = "978-0135957059"
                })
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("Author is required");
        }

        [Fact]
        public async Task Handler_WithMissingISBN_ShouldReturn400()
        {
            // Arrange
            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = JsonSerializer.Serialize(new
                {
                    title = "Test Book",
                    author = "Test Author",
                    isbn = ""
                })
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("ISBN is required");
        }

        [Fact]
        public async Task Handler_WithInvalidJson_ShouldReturn400()
        {
            // Arrange
            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = "{invalid json}"
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("Invalid JSON format!");
        }

        [Fact]
        public async Task Handler_WithEmptyBody_ShouldReturn400()
        {
            // Arrange
            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = ""
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("Invalid JSON format");
        }

        [Fact]
        public async Task Handler_WithNullBody_ShouldReturn400()
        {
            // Arrange
            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = null
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("Invalid JSON format");
        }

        [Fact]
        public async Task Handler_WhenDynamoDbFails_ShouldReturn500()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonDynamoDBException("Database error"));

            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = JsonSerializer.Serialize(new
                {
                    title = "Test Book",
                    author = "Test Author",
                    isbn = "978-0135957059"
                })
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(500);
            response.Body.Should().Contain("Could not add book");
        }

        [Fact]
        public async Task Handler_ShouldSetCorrectDynamoDbKeys()
        {
            // Arrange
            PutItemRequest? capturedRequest = null;
            _mockDynamoDb
                .Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutItemRequest, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new PutItemResponse { HttpStatusCode = HttpStatusCode.OK });

            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = JsonSerializer.Serialize(new
                {
                    title = "Test Book",
                    author = "Test Author",
                    isbn = "978-0135957059"
                })
            };

            // Act
            await function.Handler(request, _context);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.TableName.Should().Be("Books");
            capturedRequest.Item.Should().ContainKey("PK");
            capturedRequest.Item.Should().ContainKey("SK");
            capturedRequest.Item["PK"].S.Should().StartWith("BOOK#");
            capturedRequest.Item["SK"].S.Should().StartWith("METADATA#");
        }

        [Fact]
        public async Task Handler_ShouldIncludeCorsHeaders()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutItemResponse { HttpStatusCode = HttpStatusCode.OK });

            var function = new AddBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                Body = JsonSerializer.Serialize(new
                {
                    title = "Test Book",
                    author = "Test Author",
                    isbn = "978-0135957059"
                })
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.Headers.Should().ContainKey("Content-Type");
            response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
            response.Headers["Content-Type"].Should().Be("application/json");
            response.Headers["Access-Control-Allow-Origin"].Should().Be("*");
        }
    }
}

