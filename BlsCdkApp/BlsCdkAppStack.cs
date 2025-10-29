using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.APIGateway;
using Constructs;

namespace BlsCdkApp
{
    public class BlsCdkAppStack : Stack
    {
        internal BlsCdkAppStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // DynamoDB Table
            var table = new Table(this, "BooksTable", new TableProps
            {
                TableName = "BooksTable",
                PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "SK", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Single Lambda function for the entire ASP.NET Core Web API
            var apiFunction = new Function(this, "BooksApiFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Handler = "BlsApi",  // AspNetCoreServer handles all routes
                Code = Code.FromAsset("BlsApi/bin/Release/net8.0/publish"),
                Timeout = Duration.Seconds(30),
                MemorySize = 1024,
                Environment = new Dictionary<string, string>
                {
                    { "DynamoDB__TableName", table.TableName },
                    { "ASPNETCORE_ENVIRONMENT", "Production" }
                }
            });

            // Grant DynamoDB permissions
            table.GrantReadWriteData(apiFunction);

            // API Gateway REST API with Lambda Proxy Integration
            var api = new RestApi(this, "BooksApi", new RestApiProps
            {
                RestApiName = "Books Lending Service",
                Description = "Books Lending Service Web API"
            });

            // Proxy all requests to the Lambda function
            var integration = new LambdaIntegration(apiFunction, new LambdaIntegrationOptions
            {
                Proxy = true
            });

            api.Root.AddProxy(new ProxyResourceOptions
            {
                DefaultIntegration = integration,
                AnyMethod = true
            });

            // Output the API URL
            new CfnOutput(this, "ApiUrl", new CfnOutputProps
            {
                Value = api.Url,
                Description = "API Gateway URL"
            });
        }
    }
}
