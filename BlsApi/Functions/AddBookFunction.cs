using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using BlsApi.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BlsApi.Functions
{
    public class AddBookFunction
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");

        public AddBookFunction()
        {
            _dynamoDb = new AmazonDynamoDBClient();
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Log the incoming request
                context.Logger.LogInformation($"Request Path: {request.Path}");
                context.Logger.LogInformation($"Request Method: {request.HttpMethod}");
                context.Logger.LogInformation($"Request Body: {request.Body}");
                context.Logger.LogInformation($"Path Parameters: {JsonSerializer.Serialize(request.PathParameters)}");
                context.Logger.LogInformation($"Query String Parameters: {JsonSerializer.Serialize(request.QueryStringParameters)}");

                if (string.IsNullOrEmpty(request.Body))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = JsonSerializer.Serialize(new { error = "Request body is required" }),
                        Headers = new Dictionary<string, string>
                        {
                            { "Content-Type", "application/json" },
                            { "Access-Control-Allow-Origin", "*" }
                        }
                    };
                }

                var book = JsonSerializer.Deserialize<Book>(request.Body) ?? 
                    throw new InvalidOperationException("Failed to deserialize book from request body");
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

                context.Logger.LogInformation($"Adding book to DynamoDB... {book.Id}", book);
                await _dynamoDb.PutItemAsync(putRequest);
                context.Logger.LogInformation($"Book added to DynamoDB: {book.Id}");

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
