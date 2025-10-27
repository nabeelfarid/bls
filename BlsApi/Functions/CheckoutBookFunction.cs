using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

namespace BlsApi.Functions
{
    public class CheckoutBookFunction
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");

        public CheckoutBookFunction()
        {
            _dynamoDb = new AmazonDynamoDBClient();
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
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
                    ConditionExpression = "attribute_exists(PK) AND IsCheckedOut = :notCheckedOut",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":isCheckedOut", new AttributeValue { BOOL = true } },
                        { ":notCheckedOut", new AttributeValue { BOOL = false } }
                    }
                };

                await _dynamoDb.UpdateItemAsync(updateRequest);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = JsonSerializer.Serialize(new { message = "Book checked out successfully" }),
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
                    Body = JsonSerializer.Serialize(new { error = "Book is already checked out or does not exist" }),
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
                    Body = JsonSerializer.Serialize(new { error = "Could not checkout book" }),
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
