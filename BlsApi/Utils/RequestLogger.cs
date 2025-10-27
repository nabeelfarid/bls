using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

namespace BlsApi.Utils
{
    public static class RequestLogger
    {
        public static void LogRequest(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogInformation($"Request Path: {request.Path}");
            context.Logger.LogInformation($"Request Method: {request.HttpMethod}");
            context.Logger.LogInformation($"Request Body: {request.Body ?? "null"}");
            context.Logger.LogInformation($"Path Parameters: {JsonSerializer.Serialize(request.PathParameters)}");
            context.Logger.LogInformation($"Query String Parameters: {JsonSerializer.Serialize(request.QueryStringParameters)}");
        }
    }
}
