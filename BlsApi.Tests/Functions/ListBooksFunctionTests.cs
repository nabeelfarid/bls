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
    public class ListBooksFunctionTests
    {
        private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
        private readonly TestLambdaContext _context;

        public ListBooksFunctionTests()
        {
            _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            _context = new TestLambdaContext
            {
                FunctionName = "ListBooksFunction",
                FunctionVersion = "1"
            };
            
            System.Environment.SetEnvironmentVariable("TABLE_NAME", "Books");
        }

        [Fact]
        public async Task Handler_WithBooks_ShouldReturn200WithBookList()
        {
            // Arrange
            var scanResponse = new ScanResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Items = new List<Dictionary<string, AttributeValue>>
                {
                    new Dictionary<string, AttributeValue>
                    {
                        { "PK", new AttributeValue { S = "BOOK#1" } },
                        { "SK", new AttributeValue { S = "METADATA#1" } },
                        { "Id", new AttributeValue { S = "1" } },
                        { "Title", new AttributeValue { S = "Book 1" } },
                        { "Author", new AttributeValue { S = "Author 1" } },
                        { "ISBN", new AttributeValue { S = "123-456" } },
                        { "IsCheckedOut", new AttributeValue { BOOL = false } }
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        { "PK", new AttributeValue { S = "BOOK#2" } },
                        { "SK", new AttributeValue { S = "METADATA#2" } },
                        { "Id", new AttributeValue { S = "2" } },
                        { "Title", new AttributeValue { S = "Book 2" } },
                        { "Author", new AttributeValue { S = "Author 2" } },
                        { "ISBN", new AttributeValue { S = "789-012" } },
                        { "IsCheckedOut", new AttributeValue { BOOL = true } }
                    }
                }
            };

            _mockDynamoDb
                .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(scanResponse);

            var function = new ListBooksFunction(_mockDynamoDb.Object);
            var request = new APIGatewayProxyRequest();

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(200);
            
            var books = JsonSerializer.Deserialize<List<Book>>(response.Body);
            books.Should().NotBeNull();
            books.Should().HaveCount(2);
            
            books![0].Title.Should().Be("Book 1");
            books[0].IsCheckedOut.Should().BeFalse();
            
            books[1].Title.Should().Be("Book 2");
            books[1].IsCheckedOut.Should().BeTrue();

            _mockDynamoDb.Verify(
                x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handler_WithNoBooks_ShouldReturn200WithEmptyList()
        {
            // Arrange
            var scanResponse = new ScanResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Items = new List<Dictionary<string, AttributeValue>>()
            };

            _mockDynamoDb
                .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(scanResponse);

            var function = new ListBooksFunction(_mockDynamoDb.Object);
            var request = new APIGatewayProxyRequest();

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(200);
            
            var books = JsonSerializer.Deserialize<List<Book>>(response.Body);
            books.Should().NotBeNull();
            books.Should().BeEmpty();
        }

        [Fact]
        public async Task Handler_ShouldUseScanWithCorrectFilter()
        {
            // Arrange
            ScanRequest? capturedRequest = null;
            var scanResponse = new ScanResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Items = new List<Dictionary<string, AttributeValue>>()
            };

            _mockDynamoDb
                .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ScanRequest, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(scanResponse);

            var function = new ListBooksFunction(_mockDynamoDb.Object);
            var request = new APIGatewayProxyRequest();

            // Act
            await function.Handler(request, _context);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.TableName.Should().Be("Books");
            capturedRequest.FilterExpression.Should().Be("begins_with(SK, :metadata)");
            capturedRequest.ExpressionAttributeValues.Should().ContainKey(":metadata");
            capturedRequest.ExpressionAttributeValues[":metadata"].S.Should().Be("METADATA#");
        }

        [Fact]
        public async Task Handler_WhenDynamoDbFails_ShouldReturn500()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonDynamoDBException("Database error"));

            var function = new ListBooksFunction(_mockDynamoDb.Object);
            var request = new APIGatewayProxyRequest();

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(500);
            response.Body.Should().Contain("Could not retrieve books");
        }

        [Fact]
        public async Task Handler_ShouldIncludeCorsHeaders()
        {
            // Arrange
            var scanResponse = new ScanResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Items = new List<Dictionary<string, AttributeValue>>()
            };

            _mockDynamoDb
                .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(scanResponse);

            var function = new ListBooksFunction(_mockDynamoDb.Object);
            var request = new APIGatewayProxyRequest();

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.Headers.Should().ContainKey("Content-Type");
            response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
            response.Headers["Content-Type"].Should().Be("application/json");
            response.Headers["Access-Control-Allow-Origin"].Should().Be("*");
        }

        [Fact]
        public async Task Handler_WithMissingIsCheckedOut_ShouldDefaultToFalse()
        {
            // Arrange
            var scanResponse = new ScanResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Items = new List<Dictionary<string, AttributeValue>>
                {
                    new Dictionary<string, AttributeValue>
                    {
                        { "PK", new AttributeValue { S = "BOOK#1" } },
                        { "SK", new AttributeValue { S = "METADATA#1" } },
                        { "Id", new AttributeValue { S = "1" } },
                        { "Title", new AttributeValue { S = "Book 1" } },
                        { "Author", new AttributeValue { S = "Author 1" } },
                        { "ISBN", new AttributeValue { S = "123-456" } }
                        // IsCheckedOut is missing
                    }
                }
            };

            _mockDynamoDb
                .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(scanResponse);

            var function = new ListBooksFunction(_mockDynamoDb.Object);
            var request = new APIGatewayProxyRequest();

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(200);
            
            var books = JsonSerializer.Deserialize<List<Book>>(response.Body);
            books.Should().NotBeNull();
            books.Should().HaveCount(1);
            books![0].IsCheckedOut.Should().BeFalse(); // Should default to false
        }
    }
}

