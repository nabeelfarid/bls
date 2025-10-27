using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

namespace BlsApi.Utils
{
    public static class ApiResponse
    {
        private static readonly Dictionary<string, string> DefaultHeaders = new()
        {
            { "Content-Type", "application/json" },
            { "Access-Control-Allow-Origin", "*" }
        };

        public static APIGatewayProxyResponse Success<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = JsonSerializer.Serialize(body),
                Headers = new Dictionary<string, string>(DefaultHeaders)
            };
        }

        public static APIGatewayProxyResponse SuccessMessage(string message, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return Success(new { message }, statusCode);
        }

        public static APIGatewayProxyResponse Error(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = JsonSerializer.Serialize(new { error = errorMessage }),
                Headers = new Dictionary<string, string>(DefaultHeaders)
            };
        }

        public static APIGatewayProxyResponse ValidationError(List<string> errors)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = JsonSerializer.Serialize(new { errors }),
                Headers = new Dictionary<string, string>(DefaultHeaders)
            };
        }

        public static APIGatewayProxyResponse BadRequest(string errorMessage)
        {
            return Error(errorMessage, HttpStatusCode.BadRequest);
        }

        public static APIGatewayProxyResponse InternalServerError(string errorMessage = "Internal server error")
        {
            return Error(errorMessage, HttpStatusCode.InternalServerError);
        }

        public static APIGatewayProxyResponse NotFound(string errorMessage = "Resource not found")
        {
            return Error(errorMessage, HttpStatusCode.NotFound);
        }

        public static APIGatewayProxyResponse Created<T>(T body)
        {
            return Success(body, HttpStatusCode.Created);
        }
    }
}

