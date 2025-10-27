using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

namespace BlsApi.Functions
{
    public class ReturnBookFunction
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");

        public ReturnBookFunction()
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
                context.Logger.LogInformation($"Path Parameters: {JsonSerializer.Serialize(request.PathParameters)}");
                context.Logger.LogInformation($"Query String Parameters: {JsonSerializer.Serialize(request.QueryStringParameters)}");

                var bookId = request.PathParameters["id"];

                var updateRequest = new UpdateItemRequest
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "PK", new AttributeValue { S = $"BOOK#{bookId}" } },
                        { "SK", new AttributeValue { S = $"METADATA#{bookId}" } }
                    },
                    UpdateExpression = "SET IsCheckedOut = :isCheckedOut",
                    ConditionExpression = "attribute_exists(PK) AND IsCheckedOut = :checkedOut",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":isCheckedOut", new AttributeValue { BOOL = false } },
                        { ":checkedOut", new AttributeValue { BOOL = true } }
                    }
                };

                await _dynamoDb.UpdateItemAsync(updateRequest);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = JsonSerializer.Serialize(new { message = "Book returned successfully" }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }
            catch (ConditionalCheckFailedException)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { error = "Book is not checked out or does not exist" }),
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
                    Body = JsonSerializer.Serialize(new { error = "Could not return book" }),
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
