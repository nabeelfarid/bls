using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BlsApi.Models;

namespace BlsApi.Services
{
    public class BookService : IBookService
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName;
        private readonly ILogger<BookService> _logger;

        public BookService(IAmazonDynamoDB dynamoDb, IConfiguration configuration, ILogger<BookService> logger)
        {
            _dynamoDb = dynamoDb;
            _tableName = configuration["DynamoDB:TableName"] 
                ?? Environment.GetEnvironmentVariable("TABLE_NAME") 
                ?? throw new InvalidOperationException("DynamoDB table name is not configured");
            _logger = logger;
        }

        public async Task<Book> AddBookAsync(Book book)
        {
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
            _logger.LogInformation("Book added successfully: {BookId}", book.Id);

            return book;
        }

        public async Task<List<Book>> ListBooksAsync()
        {
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

            _logger.LogInformation("Retrieved {Count} books", books.Count);
            return books;
        }

        public async Task<bool> CheckoutBookAsync(string bookId)
        {
            try
            {
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
                _logger.LogInformation("Book checked out successfully: {BookId}", bookId);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                _logger.LogWarning("Book checkout failed: {BookId}", bookId);
                return false;
            }
        }

        public async Task<bool> ReturnBookAsync(string bookId)
        {
            try
            {
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
                _logger.LogInformation("Book returned successfully: {BookId}", bookId);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                _logger.LogWarning("Book return failed: {BookId}", bookId);
                return false;
            }
        }
    }
}

