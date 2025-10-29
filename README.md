# Books Lending Service

A .NET 8 Web API for managing a library's book lending system. The application runs locally with `dotnet run` and deploys to AWS Lambda + API Gateway with the same codebase for production use.

### Key Features

- ✅ **.NET 8 Web API development**
- ✅ **Local development** with `dotnet run` or `make run-local`
- ✅ **IaC - Deploys to AWS** using CDK (CloudFormation stack) seamlessly (same codebase!)
- ✅ **Swagger/OpenAPI** documentation at `/swagger`
- ✅ **Local & AWS DynamoDB** support (switch via configuration)
- ✅ **Comprehensive testing** with xUnit, Moq, and FluentAssertions
- ✅ **Service layer architecture** for clean separation
- ✅ **Dependency injection** throughout
- ✅ **Input validation** with Data Annotations
- ✅ **CORS enabled** for frontend integration
- ✅ **Structured logging** to Console (local) or CloudWatch (AWS)
- ✅ **CI/CD automation** with GitHub Actions
- ✅ **Postman collection** with environments

---

## 📋 Table of Contents

1. [Quick Start](#-quick-start)
2. [API Endpoints](#-api-endpoints)
3. [AWS Deployment](#-aws-deployment)
4. [Testing](#-testing)
5. [Architecture](#-architecture)
6. [Configuration](#-configuration)
7. [Make Commands](#-make-commands)
8. [Troubleshooting](#-troubleshooting)
9. [CI/CD](#-cicd)
10. [Request Validation](#-request-validation)
11. [Quick Command Reference](#-quick-command-reference)

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (for local DynamoDB)
- [AWS CLI](https://aws.amazon.com/cli/) (for DynamoDB setup)
- [AWS CDK](https://docs.aws.amazon.com/cdk/) (optional, for AWS deployment)

### Local Dev Setup

```bash
# 1. Start local DynamoDB and Create the database table
./setup-local-dynamodb.sh

# 2. Run the API
make run-local
```

**That's it!** The API is now running at http://localhost:5000

### Verify Setup

```bash
# Check if DynamoDB is running
docker ps

# List tables
aws dynamodb list-tables --endpoint-url http://localhost:8000

# Should show: BooksTable-Dev
```

### Using Swagger UI

1. Start the API in Development mode
2. Navigate to: http://localhost:5000/swagger
3. Try out endpoints interactively

**Note:** Swagger only appears when `ASPNETCORE_ENVIRONMENT=Development`

---

## 📡 API Endpoints

All endpoints are prefixed with `/api`:

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| `GET` | `/api/books` | List all books | - |
| `POST` | `/api/books` | Add a new book | `{ "title", "author", "isbn" }` |
| `POST` | `/api/books/{id}/checkout` | Check out a book | - |
| `POST` | `/api/books/{id}/return` | Return a book | - |

### Example Usage

**Add a Book:**
```bash
curl -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Clean Code",
    "author": "Robert C. Martin",
    "isbn": "978-0132350884"
  }'
```

**Response:**
```json
{
  "id": "abc-123-def",
  "title": "Clean Code",
  "author": "Robert C. Martin",
  "isbn": "978-0132350884",
  "isCheckedOut": false
}
```

**List All Books:**
```bash
curl http://localhost:5000/api/books
```

**Checkout a Book:**
```bash
curl -X POST http://localhost:5000/api/books/abc-123-def/checkout
```

**Return a Book:**
```bash
curl -X POST http://localhost:5000/api/books/abc-123-def/return
```

---

## ☁️ AWS Deployment

Deploy your API to AWS Lambda + API Gateway with a single command!

### First-Time Setup

```bash
# 1. Configure AWS credentials
aws configure

# 2. Bootstrap CDK (one-time per account/region)
cdk bootstrap
```

### Deploy


```bash
make deploy
```


### What Gets Deployed

The CDK stack creates:
- ✅ **DynamoDB Table** (`BooksTable`) - Serverless database
- ✅ **Lambda Function** (`BooksApiFunction`) - Your ASP.NET Core Web API
  - Runtime: .NET 8
  - Memory: 1024 MB
  - Timeout: 30 seconds
- ✅ **API Gateway** (REST API) - HTTPS endpoint with proxy integration
- ✅ **IAM Roles** - Least-privilege permissions for DynamoDB
- ✅ **CloudWatch Logs** - Automatic logging (2-year retention)

### After Deployment

You'll see output like:
```
✅ Deployment complete!

Outputs:
BlsCdkAppStack.ApiUrl = https://abc123.execute-api.us-east-1.amazonaws.com/prod/

Stack ARN:
arn:aws:cloudformation:...
```

**Save the API URL!** This is your production endpoint.

### Test Deployed API

```bash
# Set your API URL
API_URL="https://abc123.execute-api.us-east-1.amazonaws.com/prod"

# Test endpoints
curl $API_URL/api/books
```

### Update Deployment

Just run the deploy command again - CDK will only update what changed:
```bash
make deploy
```

### Teardown

To delete all AWS resources:
```bash
cdk destroy
```

### View Logs

```bash
# Real-time logs
aws logs tail /aws/lambda/BlsCdkAppStack-BooksApiFunction --follow

# Or in AWS Console
# CloudWatch → Log Groups → /aws/lambda/BlsCdkAppStack-BooksApiFunction
```

---

## 🧪 Testing

### Run Tests

```bash
make test
```

### Test Suite

**21 unit tests** covering:

- **BookService Tests** (12 tests)
  - Add book functionality
  - List books with various scenarios
  - Checkout/return operations
  - Error handling
  - DynamoDB conditional updates

- **BooksController Tests** (8 tests)
  - HTTP response handling
  - Validation error responses
  - Success scenarios
  - Edge cases

- **ValidationHelper Tests** (1 test)
  - Input validation logic

### Test with Postman

**1. Import Collection and Environments:**
- `Books-Lending-Service.postman_collection.json`
- `Books-Lending-Service.postman_environment_local.json` (for local)
- `Books-Lending-Service.postman_environment_aws.json` (for AWS)

**2. Select Environment:**
- Use the dropdown in Postman to switch between Local and AWS

**3. Test Flow:**
1. List Books (empty initially)
2. Add Book (save the `id` from response)
3. Update the `bookId` environment variable
4. Checkout Book
5. Return Book

---

## 🏗️ Architecture

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | .NET 8, ASP.NET Core |
| **Database** | DynamoDB (AWS SDK) |
| **API Docs** | Swagger/Swashbuckle |
| **Testing** | xUnit, Moq, FluentAssertions |
| **Infrastructure** | AWS CDK (C#) |
| **Deployment** | AWS Lambda + API Gateway |
| **Logging** | Microsoft.Extensions.Logging |

### Architecture Diagram

```
┌─────────────────────────────────────────────┐
│          LOCAL DEVELOPMENT                   │
├─────────────────────────────────────────────┤
│  Browser/Postman                            │
│       ↓                                     │
│  Kestrel Web Server (localhost:5000)       │
│       ↓                                     │
│  ASP.NET Core Web API                       │
│       ↓                                     │
│  Local DynamoDB (Docker, port 8000)        │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│          AWS PRODUCTION                      │
├─────────────────────────────────────────────┤
│  Internet (HTTPS)                           │
│       ↓                                     │
│  API Gateway (REST API)                     │
│       ↓                                     │
│  Lambda Function (ASP.NET Core Web API)    │
│       ↓                                     │
│  DynamoDB (BooksTable)                      │
│       ↓                                     │
│  CloudWatch Logs                            │
└─────────────────────────────────────────────┘
```

### Design Patterns

- **Controller-Service Pattern**: Separation of HTTP handling and business logic
- **Dependency Injection**: Constructor injection for testability
- **Repository Pattern**: Service layer abstracts data access
- **Single-Table Design**: DynamoDB with composite keys (PK/SK)

### Key Design Decisions

1. **Web API + Lambda Hosting**: Same code runs locally AND in Lambda
   - Uses `Amazon.Lambda.AspNetCoreServer.Hosting`
   - `LambdaEventSource.RestApi` for API Gateway integration

2. **Environment-based Configuration**: 
   - `appsettings.Development.json` → Local DynamoDB
   - `appsettings.json` → AWS DynamoDB

3. **Service Layer**: Business logic separated from controllers for testability

4. **Data Annotations**: Simple, built-in validation (can upgrade to FluentValidation if needed)

---

## 🎯 Architecture and Design Decisions

### Current Implementation

- **Web API + Lambda**: ASP.NET Core Web API that runs locally with `dotnet run` and deploys to AWS Lambda
- **Infrastructure as Code**: AWS CDK with C# for type-safe infrastructure definitions
- **Single-Table Design**: DynamoDB with composite keys (PK/SK) for flexible data modeling
- **Single Lambda Function**: One Lambda handling all endpoints via API Gateway proxy integration
- **Service Layer Architecture**: Controllers → Services → DynamoDB for separation of concerns
- **Request Validation**: Data Annotations with `ValidationHelper` utility for input validation
- **JSON Serialization**: Lowercase property names for REST API convention compliance

### Key Tradeoffs

#### 1. **Web API + Lambda vs Lambda-Only Functions**

**Current Choice: Single Lambda with ASP.NET Core Web API**

✅ **Pros:**
- Can run locally with `dotnet run` (main requirement!)
- Standard ASP.NET Core patterns (familiar to .NET developers)
- Swagger/OpenAPI documentation included
- Easy debugging and testing locally
- Service layer for clean architecture
- Same codebase works everywhere (local, AWS, containers, etc.)
- Standard dependency injection
- Easy to write unit tests

❌ **Cons:**
- Larger deployment package (~15-20 MB vs ~5-10 MB per function)
- Slower cold starts (~2-3s vs ~1-2s)
- Higher memory usage (~150-200 MB vs ~40-80 MB)
- All endpoints scale together (can't optimize individually)
- Slightly higher cost (~70% more for low traffic)

**Alternative: Lambda-Only Functions**
- Individual Lambda per endpoint
- Smaller, faster cold starts
- Independent scaling per endpoint
- Lower cost at scale
- But: Requires AWS SAM Local or LocalStack for local testing, AWS-specific code, less portable

**Why we chose Web API + Lambda:**
The requirement was for ".NET 8 Web API that can run locally with `dotnet run`" - meaning standard .NET development experience without additional tooling. Lambda-only solutions CAN run locally using AWS SAM Local (`sam local start-api`) or LocalStack, but require:
- Installing AWS SAM CLI or LocalStack
- Docker containers for Lambda runtime emulation
- Lambda-specific testing setup
- Different local vs. production code paths

The Web API approach gives us native `dotnet run`, standard debugging, and the same code everywhere. The extra cost might be negligible compared to developer productivity gains.

#### 2. **Single Lambda vs Multiple Lambdas**

**Current Choice: Single Lambda (Monolithic)**

✅ **Pros:**
- Simpler infrastructure (one function to manage)
- Shared code and dependencies
- One deployment unit (easier CI/CD)
- Consistent performance characteristics
- Standard Web API patterns
- Less cold start impact overall (one function vs four)

❌ **Cons:**
- All endpoints use same memory/timeout settings
- Can't optimize per-endpoint
- Harder to see per-endpoint costs in CloudWatch
- All endpoints scale together

**Alternative: Lambda per Endpoint**
- Independent scaling and configuration
- Fine-grained metrics
- Can optimize heavy operations separately
- But: More complex infrastructure, more cold starts

**Why we chose Single Lambda:**
For a small API with similar endpoint requirements, a monolithic approach is simpler. If one endpoint becomes a bottleneck, it can be split out later.

#### 3. **Single-Table Design vs Multiple Tables**

**Current Choice: Single-Table Design (PK/SK pattern)**

✅ **Pros:**
- Cost-effective (one table = one billing unit)
- Efficient for access patterns with relationships
- Supports complex queries with composite keys
- Better performance for related data fetches

❌ **Cons:**
- Steeper learning curve
- Requires careful schema design and access patterns upfront
- Harder to query ad-hoc (not as flexible as SQL)
- Potential for hot partitions if not designed well

**Alternative: Table per Entity**
- Simpler mental model (one table per concept)
- Easier to understand for SQL developers
- More flexible for querying

**Why we chose Single-Table:**
For a small-to-medium scale library system, single-table design is more cost-effective and performant. 

#### 4. **Data Annotations vs FluentValidation**

**Current Choice: Data Annotations**

✅ **Pros:**
- Built-in (no extra dependencies)
- Simple and declarative
- Easy to read and maintain
- Good for straightforward validation rules

❌ **Cons:**
- Limited for complex validation logic
- No conditional validation out-of-the-box
- Harder to test validation rules in isolation
- Can't easily inject dependencies

**Alternative: FluentValidation**
- More powerful and flexible
- Better for complex business rules
- Easier to test
- Supports async validation

**Why we chose Data Annotations:**
Current validation needs are simple (required fields, string length). FluentValidation can be added later if validation becomes more complex.

#### 5. **Constructor Injection vs Service Locator**

**Current Choice: Constructor Injection**

✅ **Pros:**
- Dependencies are explicit
- Easier to test (can inject mocks)
- Compile-time safety
- Follows SOLID principles

❌ **Cons:**
- More verbose
- Can lead to large constructor parameter lists

**Alternative: Service Locator Pattern**
- Dependencies resolved at runtime
- More flexible but less explicit

**Why we chose Constructor Injection:**
Better testability and clearer dependencies. ASP.NET Core's built-in DI container makes this the standard pattern.

#### 6. **REST API vs GraphQL**

**Current Choice: REST API**

✅ **Pros:**
- Simpler to implement and understand
- Better caching support
- Mature tooling and ecosystem
- Standard HTTP status codes

❌ **Cons:**
- Over-fetching/under-fetching of data
- Multiple round trips for related resources
- Versioning challenges

**Alternative: GraphQL**
- Flexible data fetching
- Single endpoint
- Strong typing
- Better for complex UIs

**Why we chose REST:**
For a simple CRUD API with well-defined endpoints, REST is sufficient and simpler. GraphQL adds complexity that's not needed for this use case.

#### 7. **No Caching vs API Gateway/DAX Caching**

**Current Choice: No Caching**

✅ **Pros:**
- Simpler architecture
- Always fresh data
- No cache invalidation complexity

❌ **Cons:**
- Higher DynamoDB read costs
- Slower response times for read-heavy endpoints
- More load on database

**Alternative: Add Caching**
- API Gateway caching (simple, managed)
- DynamoDB DAX (in-memory cache)
- Redis/ElastiCache (more control)

**Why we chose No Caching:**
Starting simple. Caching can be added later when performance metrics indicate it's needed. Premature optimization is avoided.

### Best Practices & Future Improvements

#### 1. **Split Heavy Operations**

If `ListBooks` becomes a bottleneck:
```
Web API handles most endpoints
    ↓
Heavy scan operation → Separate optimized Lambda
```

Benefits:
- Optimize the slow operation independently
- Keep dev experience for other endpoints
- Best of both worlds

#### 2. **Add API Gateway Request Validation**

Current: Validation in application code
Future: Add validation at API Gateway level
- Reject invalid requests before Lambda invocation
- Save Lambda execution costs
- Faster error responses

#### 3. **Add Authentication**

Options:
- Lambda Authorizers (custom auth logic)
- API Gateway API Keys (simple)
- AWS Cognito (full user management)
- OAuth2/OIDC integration

#### 4. **Monitoring & Observability**

Add:
- CloudWatch Dashboards for key metrics
- CloudWatch Alarms for errors and latency
- AWS X-Ray for distributed tracing
- Structured logging with correlation IDs

#### 5. **Performance Optimizations**

When needed:
- Provisioned concurrency (eliminate cold starts)
- DynamoDB DAX caching (faster reads)
- API Gateway caching (reduce Lambda invocations)
- Connection pooling for DynamoDB client

#### 6. **Advanced Validation**

Upgrade to FluentValidation if needed:
```csharp
public class BookValidator : AbstractValidator<Book>
{
    public BookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);
        
        RuleFor(x => x.ISBN)
            .NotEmpty()
            .Must(BeValidISBN);
    }
}
```

#### 7. **Multi-Environment Deployment**

Set up:
- Dev environment (lower resources, test data)
- Staging environment (prod-like testing)
- Production environment (full resources)

Use CDK context or parameters to configure per-environment.

---

## ⚙️ Configuration

### Local Development (`appsettings.Development.json`)

```json
{
  "DynamoDB": {
    "TableName": "BooksTable-Dev",
    "ServiceUrl": "http://localhost:8000"
  }
}
```

### AWS Production (`appsettings.json`)

```json
{
  "DynamoDB": {
    "TableName": "BooksTable",
    "ServiceUrl": ""  // Empty = use AWS
  }
}
```

### Environment Variables (Set by CDK in Lambda)

- `DynamoDB__TableName` - Table name
- `ASPNETCORE_ENVIRONMENT` - Set to `Production`

---

## 🔧 Make Commands

```bash
make help          # Show all available commands
make run-local     # Run API locally in Development mode
make test          # Run all unit tests
make build         # Build and publish for deployment
make deploy        # Deploy to AWS (runs tests first)
make clean         # Clean all build artifacts
make restore       # Restore NuGet packages
```

---

## 🐛 Troubleshooting

### Port Already in Use

```bash
# Use a different port
dotnet run --urls "http://localhost:5050"
```

### Docker/DynamoDB Issues

```bash
# Check if Docker is running
docker info

# Check if DynamoDB container is running
docker ps

# Restart DynamoDB container
docker rm -f dynamodb-local
docker run -d -p 8000:8000 --name dynamodb-local amazon/dynamodb-local

# Verify connection
aws dynamodb list-tables --endpoint-url http://localhost:8000
```

### Table Not Found

```bash
# Check if table exists
aws dynamodb list-tables --endpoint-url http://localhost:8000

# If not, create it
./setup-local-dynamodb.sh
```

### Swagger Not Showing

Make sure you are running in Development mode:
```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
# or
make 
```

### AWS Deployment Errors

```bash
# Check AWS credentials
aws sts get-caller-identity

# Check CDK bootstrap
cdk bootstrap

# View CloudFormation events
aws cloudformation describe-stack-events --stack-name BlsCdkAppStack
```

---

## 🔄 CI/CD

### GitHub Actions Workflow

The project includes automated deployment via GitHub Actions (`.github/workflows/deploy.yml`).

**Triggers:**
- Push to `main` branch
- Manual trigger via GitHub UI

**Workflow Steps:**
1. ✅ Setup .NET 8 and Node.js
2. ✅ Run all tests
3. ✅ Publish Web API
4. ✅ Deploy with CDK
5. ✅ Output API URL

### Setup GitHub Actions

**1. Create OIDC Provider in AWS** (one-time):
```bash
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com
```

**2. Create IAM Role** with trust policy for GitHub

**3. Add GitHub Secrets**:
- `AWS_ROLE_ARN` - Your GitHub OIDC role ARN
- `AWS_REGION` - Your AWS region (e.g., `us-east-1`)

**4. Push to main**:
```bash
git push origin main
```

GitHub Actions will automatically deploy!

---

## 📝 Request Validation

### Validation Rules

- **Title**: Required, 1-500 characters
- **Author**: Required, 1-200 characters
- **ISBN**: Required

### Error Responses

**Validation Errors (400):**
```json
{
  "errors": [
    "Title is required",
    "Author must be between 1 and 200 characters",
    "ISBN is required"
  ]
}
```

**Business Logic Errors (400):**
```json
{
  "error": "Book is already checked out or does not exist"
}
```

**Server Errors (500):**
```json
{
  "error": "Could not retrieve books"
}
```

---

## Quick Command Reference

```bash
# Documentation
cat README.md              # This file - complete guide
make help                  # See all Make commands

# Local Development
./setup-local-dynamodb.sh  # Setup local DynamoDB
make run-local             # Run API locally

# Testing
make test                  # Run unit tests
dotnet test --verbosity normal

# Deployment
make deploy                # Alternative (with tests)

# Utilities
make clean                 # Clean build artifacts
make restore               # Restore packages
```

## Key Files

| Task | Files |
|------|-------|
| Add endpoint | `Controllers/BooksController.cs`, `Services/BookService.cs` |
| Business logic | `Services/BookService.cs` |
| Validation | `Models/Book.cs` |
| AWS resources | `BlsCdkApp/BlsCdkAppStack.cs` |
| Configuration | `appsettings.json`, `appsettings.Development.json` |
| CI/CD | `.github/workflows/deploy.yml` |


---

## 💡 What Changed from Lambda-Only?

This project was originally Lambda-only. Here's what was improved:

### Before (Lambda-Only)
```
❌ Could not run locally with `dotnet run`
❌ Required AWS for testing
❌ No Swagger documentation
❌ Lambda functions only
```

### After (Web API + Lambda)
```
✅ Runs locally with `dotnet run`
✅ Swagger UI for interactive testing
✅ Same code works locally AND in Lambda
✅ Service layer for clean architecture
✅ Configuration-based DynamoDB switching
✅ Multiple deployment scripts
✅ CI/CD ready
```

### Migration Summary

- **Changed**: `BlsApi.csproj` from Lambda SDK to Web SDK
- **Added**: `Program.cs`, `BooksController.cs`, `BookService.cs`
- **Added**: Lambda hosting adapter for dual-mode operation
- **Removed**: Individual Lambda function files
- **Updated**: CDK stack for single Lambda with proxy integration
- **Result**: Same codebase runs locally AND on Lambda!

---

**Built with ❤️ using .NET 8 and AWS**
