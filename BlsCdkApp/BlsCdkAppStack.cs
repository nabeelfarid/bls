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
                TableName = "Books",
                PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "SK", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Lambda Functions
            var addBookFunction = new Function(this, "AddBookFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Handler = "BlsApi::BlsApi.Functions.AddBookFunction::Handler",
                Code = Code.FromAsset("BlsApi/bin/Release/net8.0/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "TABLE_NAME", table.TableName }
                }
            });

            var listBooksFunction = new Function(this, "ListBooksFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Handler = "BlsApi::BlsApi.Functions.ListBooksFunction::Handler",
                Code = Code.FromAsset("BlsApi/bin/Release/net8.0/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "TABLE_NAME", table.TableName }
                }
            });

            var checkoutBookFunction = new Function(this, "CheckoutBookFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Handler = "BlsApi::BlsApi.Functions.CheckoutBookFunction::Handler",
                Code = Code.FromAsset("BlsApi/bin/Release/net8.0/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "TABLE_NAME", table.TableName }
                }
            });

            var returnBookFunction = new Function(this, "ReturnBookFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Handler = "BlsApi::BlsApi.Functions.ReturnBookFunction::Handler",
                Code = Code.FromAsset("BlsApi/bin/Release/net8.0/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "TABLE_NAME", table.TableName }
                }
            });

            // Grant DynamoDB permissions to Lambda functions
            table.GrantReadWriteData(addBookFunction);
            table.GrantReadWriteData(listBooksFunction);
            table.GrantReadWriteData(checkoutBookFunction);
            table.GrantReadWriteData(returnBookFunction);

            // API Gateway
            var api = new RestApi(this, "BooksApi", new RestApiProps
            {
                RestApiName = "Books Lending Service",
                Description = "This is the Books Lending Service API"
            });

            // API Resources and Methods
            var books = api.Root.AddResource("books");
            books.AddMethod("POST", new LambdaIntegration(addBookFunction));
            books.AddMethod("GET", new LambdaIntegration(listBooksFunction));

            var book = books.AddResource("{id}");
            var checkout = book.AddResource("checkout");
            checkout.AddMethod("POST", new LambdaIntegration(checkoutBookFunction));

            var returnBook = book.AddResource("return");
            returnBook.AddMethod("POST", new LambdaIntegration(returnBookFunction));
        }
    }
}
