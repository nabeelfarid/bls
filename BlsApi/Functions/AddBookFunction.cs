using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using BlsApi.Models;
using BlsApi.Utils;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BlsApi.Functions
{
    public class AddBookFunction
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName;

        public AddBookFunction() : this(new AmazonDynamoDBClient())
        {
        }

        public AddBookFunction(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
            _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                RequestLogger.LogRequest(request, context);

                var book = JsonSerializer.Deserialize<Book>(request.Body ?? string.Empty);
                
                // Validate the book object
                var (isValid, errors) = ValidationHelper.Validate(book);
                if (!isValid)
                {
                    context.Logger.LogWarning($"Validation failed: {string.Join(", ", errors)}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = JsonSerializer.Serialize(new { errors }),
                        Headers = new Dictionary<string, string>
                        {
                            { "Content-Type", "application/json" },
                            { "Access-Control-Allow-Origin", "*" }
                        }
                    };
                }

                book.Id = Guid.NewGuid().ToString();
                book.IsCheckedOut = false;

                var putRequest = new PutItemRequest
                {
                    TableName = _tableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "PK", new AttributeValue { S = $"BOOK#{book.Id}" } },
                        { "SK", new AttributeValue { S = $"METADATA#{book.Id}" } },
                        { "Id", new AttributeValue { S = book.Id } },
                        { "Title", new AttributeValue { S = book.Title } },
                        { "Author", new AttributeValue { S = book.Author } },
                        { "ISBN", new AttributeValue { S = book.ISBN } },
                        { "IsCheckedOut", new AttributeValue { BOOL = book.IsCheckedOut } }
                    }
                };

                await _dynamoDb.PutItemAsync(putRequest);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 201,
                    Body = JsonSerializer.Serialize(book),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }
            catch (JsonException ex)
            {
                context.Logger.LogWarning($"Invalid JSON format: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { error = "Invalid JSON format", details = ex.Message }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = JsonSerializer.Serialize(new { error = "Could not add book" }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }
        }
    }
}
