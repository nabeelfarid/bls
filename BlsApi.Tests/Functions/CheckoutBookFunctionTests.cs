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
using FluentAssertions;
using Moq;
using Xunit;

namespace BlsApi.Tests.Functions
{
    public class CheckoutBookFunctionTests
    {
        private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
        private readonly TestLambdaContext _context;

        public CheckoutBookFunctionTests()
        {
            _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            _context = new TestLambdaContext
            {
                FunctionName = "CheckoutBookFunction",
                FunctionVersion = "1"
            };
            
            System.Environment.SetEnvironmentVariable("TABLE_NAME", "Books");
        }

        [Fact]
        public async Task Handler_WithAvailableBook_ShouldReturn200()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateItemResponse { HttpStatusCode = HttpStatusCode.OK });

            var function = new CheckoutBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "id", "test-book-id" }
                }
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(200);
            response.Body.Should().Contain("Book checked out successfully");

            _mockDynamoDb.Verify(
                x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handler_WithAlreadyCheckedOutBook_ShouldReturn400()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConditionalCheckFailedException("Condition not met"));

            var function = new CheckoutBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "id", "test-book-id" }
                }
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("Book is already checked out or does not exist");
        }

        [Fact]
        public async Task Handler_WithNonExistentBook_ShouldReturn400()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConditionalCheckFailedException("Item not found"));

            var function = new CheckoutBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "id", "non-existent-id" }
                }
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(400);
            response.Body.Should().Contain("does not exist");
        }

        [Fact]
        public async Task Handler_ShouldSetCorrectDynamoDbKeys()
        {
            // Arrange
            UpdateItemRequest? capturedRequest = null;
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .Callback<UpdateItemRequest, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new UpdateItemResponse { HttpStatusCode = HttpStatusCode.OK });

            var function = new CheckoutBookFunction(_mockDynamoDb.Object);

            var bookId = "test-book-123";
            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "id", bookId }
                }
            };

            // Act
            await function.Handler(request, _context);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.TableName.Should().Be("Books");
            capturedRequest.Key.Should().ContainKey("PK");
            capturedRequest.Key.Should().ContainKey("SK");
            capturedRequest.Key["PK"].S.Should().Be($"BOOK#{bookId}");
            capturedRequest.Key["SK"].S.Should().Be($"METADATA#{bookId}");
        }

        [Fact]
        public async Task Handler_ShouldUseCorrectUpdateExpression()
        {
            // Arrange
            UpdateItemRequest? capturedRequest = null;
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .Callback<UpdateItemRequest, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new UpdateItemResponse { HttpStatusCode = HttpStatusCode.OK });

            var function = new CheckoutBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "id", "test-book-id" }
                }
            };

            // Act
            await function.Handler(request, _context);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.UpdateExpression.Should().Be("SET IsCheckedOut = :isCheckedOut");
            capturedRequest.ConditionExpression.Should().Be("attribute_exists(PK) AND IsCheckedOut = :notCheckedOut");
            capturedRequest.ExpressionAttributeValues.Should().ContainKey(":isCheckedOut");
            capturedRequest.ExpressionAttributeValues.Should().ContainKey(":notCheckedOut");
            capturedRequest.ExpressionAttributeValues[":isCheckedOut"].BOOL.Should().BeTrue();
            capturedRequest.ExpressionAttributeValues[":notCheckedOut"].BOOL.Should().BeFalse();
        }

        [Fact]
        public async Task Handler_WhenDynamoDbFails_ShouldReturn500()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonDynamoDBException("Database error"));

            var function = new CheckoutBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "id", "test-book-id" }
                }
            };

            // Act
            var response = await function.Handler(request, _context);

            // Assert
            response.StatusCode.Should().Be(500);
            response.Body.Should().Contain("Could not checkout book");
        }

        [Fact]
        public async Task Handler_ShouldIncludeCorsHeaders()
        {
            // Arrange
            _mockDynamoDb
                .Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateItemResponse { HttpStatusCode = HttpStatusCode.OK });

            var function = new CheckoutBookFunction(_mockDynamoDb.Object);

            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "id", "test-book-id" }
                }
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

