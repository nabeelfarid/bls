using Amazon.DynamoDBv2;
using BlsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Lambda support (works locally AND in Lambda)
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DynamoDB
builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    var config = builder.Configuration;
    var serviceUrl = config["DynamoDB:ServiceUrl"];
    
    if (!string.IsNullOrEmpty(serviceUrl))
    {
        // Local DynamoDB
        var clientConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = serviceUrl
        };
        return new AmazonDynamoDBClient(clientConfig);
    }
    
    // AWS DynamoDB
    return new AmazonDynamoDBClient();
});

// Register services
builder.Services.AddSingleton<IBookService, BookService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

