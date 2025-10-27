using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using BlsApi.Models;
using BlsApi.Utils;

namespace BlsApi.Functions
{
    public class ListBooksFunction
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName;

        public ListBooksFunction() : this(new AmazonDynamoDBClient())
        {
        }

        public ListBooksFunction(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
            _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                RequestLogger.LogRequest(request, context);

                var scanRequest = new ScanRequest
                {
                    TableName = _tableName,
                    FilterExpression = "begins_with(SK, :metadata)",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":metadata", new AttributeValue { S = "METADATA#" } }
                    }
                };

                var response = await _dynamoDb.ScanAsync(scanRequest);
                var books = new List<Book>();

                foreach (var item in response.Items)
                {
                    books.Add(new Book
                    {
                        Id = item["Id"].S,
                        Title = item["Title"].S,
                        Author = item["Author"].S,
                        ISBN = item["ISBN"].S,
                        IsCheckedOut = item.ContainsKey("IsCheckedOut") && (item["IsCheckedOut"].BOOL ?? false)
                    });
                }

                return ApiResponse.Success(books);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex.Message}");
                return ApiResponse.InternalServerError("Could not retrieve books");
            }
        }
    }
}
