using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using BlsApi.Utils;

namespace BlsApi.Functions
{
    public class ReturnBookFunction
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName;

        public ReturnBookFunction() : this(new AmazonDynamoDBClient())
        {
        }

        public ReturnBookFunction(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
            _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                RequestLogger.LogRequest(request, context);

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

                return ApiResponse.SuccessMessage("Book returned successfully");
            }
            catch (ConditionalCheckFailedException)
            {
                return ApiResponse.BadRequest("Book is not checked out or does not exist");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex.Message}");
                return ApiResponse.InternalServerError("Could not return book");
            }
        }
    }
}
