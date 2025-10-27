# Books Lending Service

A serverless application built with AWS CDK that provides REST API endpoints for managing a library's book lending system.

## API Endpoints

- `POST /books` – Add a new book
- `GET /books` – List all books
- `POST /books/{isbn}/checkout` – Check out a book
- `POST /books/{isbn}/return` – Return a book

## Request Validation

All API requests are validated using Data Annotations. The following validations are enforced:

### Book Model Validation Rules

- **Title**: Required, must be between 1-500 characters
- **Author**: Required, must be between 1-200 characters
- **ISBN**: Required

### Error Responses

**Invalid JSON Format:**
Returns `400 Bad Request` when JSON is malformed:
```json
{
  "error": "Invalid JSON format",
  "details": "..."
}
```

**Validation Errors:**
Returns `400 Bad Request` when validation fails:
```json
{
  "errors": [
    "Title is required",
    "Author must be between 1 and 200 characters",
    "ISBN is required"
  ]
}
```

## Testing with Postman

1. Import the `Books-Lending-Service.postman_collection.json` file into Postman
2. Update the `baseUrl` variable in your Postman environment with your API Gateway URL
3. Use the provided sample requests to test each endpoint

The collection includes examples for:
- ✅ Valid book creation
- ❌ Validation errors (missing fields)

### Sample Book Data

```json
{
    "title": "The Pragmatic Programmer",
    "author": "David Thomas, Andrew Hunt",
    "isbn": "978-0135957059"
}
```

## Development

### Prerequisites

- .NET 8.0
- AWS CDK CLI
- AWS CLI configured with appropriate credentials

### Running Tests

```bash
# Run all unit tests
dotnet test

# Or use make
make test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

#### Test Suite Overview

The project includes **38 unit tests** using **xUnit**, **Moq**, and **FluentAssertions**:

**Test Structure:**
```
BlsApi.Tests/
├── Functions/
│   ├── AddBookFunctionTests.cs       # Tests for AddBookFunction (11 tests)
│   ├── ListBooksFunctionTests.cs     # Tests for ListBooksFunction (7 tests)
│   ├── CheckoutBookFunctionTests.cs  # Tests for CheckoutBookFunction (7 tests)
│   └── ReturnBookFunctionTests.cs    # Tests for ReturnBookFunction (6 tests)
└── Utils/
    └── ValidationHelperTests.cs      # Tests for validation logic (7 tests)
```

**Test Coverage:**
- ✅ **AddBookFunction**: Valid book creation, validation errors, JSON parsing, DynamoDB failures, CORS, key structure
- ✅ **ListBooksFunction**: List books, empty list, scan filters, missing attributes, DynamoDB failures, CORS
- ✅ **CheckoutBookFunction**: Checkout available book, already checked out, non-existent book, DynamoDB operations, CORS
- ✅ **ReturnBookFunction**: Return book, not checked out error, non-existent book, DynamoDB operations, CORS
- ✅ **ValidationHelper**: Field validation, length validation, null handling, multiple errors

**Testing Best Practices:**
1. **Follow AAA Pattern**: Arrange, Act, Assert
2. **Use descriptive test names**: `Handler_WithValidBook_ShouldReturn201`
3. **Mock external dependencies**: DynamoDB, S3, etc.
4. **Test edge cases**: null, empty, invalid inputs
5. **Verify mock interactions**: Ensure methods are called correctly
6. **Use FluentAssertions** for readable assertions

### Build and Deploy

```bash
# Build the solution
make build

# Bootstrap CDK (only required on first deployment in a region/account)
cdk bootstrap

# Synthesize CloudFormation template
cdk synth

# Deploy to AWS
cdk deploy
```

**Note:** If this is your first time deploying a CDK app to your AWS account/region, you must run `cdk bootstrap` first. This creates the necessary AWS resources (S3 bucket, IAM roles, etc.) that CDK needs to deploy your application.

## CI/CD with GitHub Actions

This project includes a GitHub Actions workflow that automatically deploys to AWS on every push to the `main` branch.

### Setup Instructions

1. **Create GitHub OIDC Provider in AWS (One-time Setup)**

First, create the OIDC identity provider in AWS:

**Option A: Using AWS Console**
- Go to IAM → Identity providers → Add provider
- Provider type: OpenID Connect
- Provider URL: `https://token.actions.githubusercontent.com`
- Audience: `sts.amazonaws.com`
- Click "Add provider"

**Option B: Using AWS CLI**
```bash
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com \
  --thumbprint-list 6938fd4d98bab03faadb97b34396831e3780aea1
```

2. **Create an IAM Role for GitHub Actions**

Create an IAM role with the following trust policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::YOUR_ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:YOUR_GITHUB_USERNAME/bls:*"
        }
      }
    }
  ]
}
```

Attach the following AWS managed policies to the role:
- `PowerUserAccess` (or create a custom policy with minimal required permissions)

3. **Configure GitHub Secrets**

Go to your GitHub repository → Settings → Secrets and variables → Actions, and add:

- `AWS_ROLE_ARN`: The ARN of the IAM role created above (e.g., `arn:aws:iam::123456789012:role/GitHubActionsRole`)
- `AWS_REGION`: Your AWS region (e.g., `us-east-1`)

4. **First-time Setup**

If you haven't bootstrapped CDK yet, run manually:
```bash
cdk bootstrap
```

5. **Automatic Deployment**

Once configured, every push to `main` will:
- ✅ Build the Lambda functions
- ✅ Synthesize the CDK stack
- ✅ Deploy to AWS
- ✅ Output the API Gateway URL

## Architecture and Design Decisions

### Current Implementation

- **Serverless Architecture**: Built using AWS Lambda + API Gateway + DynamoDB for cost-effectiveness and automatic scaling
- **Infrastructure as Code**: AWS CDK with C# for type-safe infrastructure definitions
- **Single-Table Design**: DynamoDB with composite keys (PK/SK) for flexible data modeling
- **Lambda per Endpoint**: Each API endpoint has its own Lambda function for independent scaling and deployment
- **Request Logging**: Centralized `RequestLogger` utility for consistent logging across all handlers
- **Request Validation**: Data Annotations with `ValidationHelper` utility for input validation
- **JSON Serialization**: Lowercase property names for REST API convention compliance

### Key Tradeoffs

#### 1. **Lambda per Endpoint vs Monolithic Lambda**

**Current Choice: Lambda per Endpoint**

✅ **Pros:**
- Independent scaling per endpoint
- Isolated deployments (update one function without affecting others)
- Smaller deployment packages (faster cold starts)
- Clear separation of concerns
- Fine-grained IAM permissions per function

❌ **Cons:**
- More resources to manage (4 Lambda functions)
- Potential code duplication across functions
- Higher cold start impact (more functions = more potential cold starts)
- More complex infrastructure code

**Alternative: Monolithic Lambda**  
- Single Lambda handling all routes
- Shared code and dependencies
- One deployment unit
- Better for small APIs with similar requirements

**Why we chose Lambda per Endpoint:**
For a library lending system, different endpoints have different characteristics:
- `POST /books` - Write-heavy, needs validation
- `GET /books` - Read-heavy, could benefit from caching
- Checkout/Return - Different business logic and potential for different scaling patterns

#### 2. **Single-Table Design vs Multiple Tables**

**Current Choice: Single-Table Design (PK/SK pattern)**

✅ **Pros:**
- Cost-effective (one table = one billing unit)
- Efficient for access patterns with relationships
- Supports complex queries with composite keys
- Better performance for related data fetches

❌ **Cons:**
- Steeper learning curve
- Requires careful schema design upfront
- Harder to query ad-hoc (not as flexible as SQL)
- Potential for hot partitions if not designed well

**Alternative: Table per Entity**
- Simpler mental model (one table per concept)
- Easier to understand for SQL developers
- More flexible for querying

**Why we chose Single-Table:**
For a small-to-medium scale library system, single-table design is more cost-effective and performant. Our access patterns are well-defined (get by ID, list all, update status).

#### 3. **Data Annotations vs FluentValidation**

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

#### 4. **Constructor Injection vs Service Locator**

**Current Choice: Constructor Injection**

✅ **Pros:**
- Dependencies are explicit
- Easier to test (can inject mocks)
- Compile-time safety
- Follows SOLID principles

❌ **Cons:**
- Requires parameterless constructor for Lambda runtime
- Need to maintain two constructors (production + testing)

**Alternative: Service Locator Pattern**
- Single constructor
- Dependencies resolved at runtime
- More flexible but less explicit

**Why we chose Constructor Injection:**
Better testability and clearer dependencies outweigh the minor inconvenience of maintaining two constructors. Lambda runtime uses the parameterless constructor, tests use the injected one.

#### 5. **REST API vs GraphQL**

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

#### 6. **No Caching vs API Gateway/DAX Caching**

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

#### 7. **Inline Lambda Code vs Separate Deployment**

**Current Choice: Separate .NET Project + Deployment**

✅ **Pros:**
- Full .NET tooling support (IDE, debugging, testing)
- Type safety and compile-time checks
- Easy to add NuGet packages
- Professional development experience

❌ **Cons:**
- Longer deployment times
- Requires build/publish step
- Larger deployment packages

**Why we chose Separate Project:**
For production-quality code with proper testing and maintainability, a full .NET project is essential. The deployment overhead is worth the development benefits.

### Best Practices & Future Improvements

#### 1. **Data Access Layer (DAL)**
Consider creating a separate library project (`BlsApi.Data`) for DynamoDB operations:
```
BlsApi.Data/
  ├── Repositories/
  │   ├── IBookRepository.cs
  │   └── BookRepository.cs
  └── Models/
      └── BookEntity.cs
```
**Benefits:**
- Separation of concerns
- Easier unit testing with mocked repositories
- Reusable across multiple Lambda functions
- Centralized database logic

#### 2. **Logging Middleware Options**

**Current Approach:** Utility class (`RequestLogger`)
- ✅ Simple and explicit
- ✅ No additional dependencies
- ✅ Easy to maintain

**Alternative Options:**

**a) Base Handler Class:**
- Inherit from a base class that handles logging, error handling, and response formatting
- Good for shared cross-cutting concerns

**b) AWS Lambda Powertools:**
```bash
dotnet add package AWS.Lambda.Powertools.Logging
dotnet add package AWS.Lambda.Powertools.Tracing
dotnet add package AWS.Lambda.Powertools.Metrics
```
- Structured logging with correlation IDs
- X-Ray tracing integration
- CloudWatch metrics
- Production-ready patterns

**c) Decorator Pattern:**
- Wrap handlers with decorators for logging, validation, etc.
- More flexible but adds complexity

#### 3. **Validation**

**Current Approach:** Data Annotations
- ✅ Built-in .NET feature (no extra dependencies)
- ✅ Simple attribute-based validation
- ✅ Good for basic validation rules
- ✅ Easy to read and maintain

**Alternative: FluentValidation** (for complex scenarios)
```bash
dotnet add package FluentValidation
```

```csharp
public class BookValidator : AbstractValidator<Book>
{
    public BookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Title must be between 1 and 500 characters");
        
        RuleFor(x => x.ISBN)
            .NotEmpty()
            .Must(BeValidISBN)
            .WithMessage("Invalid ISBN format");
    }
    
    private bool BeValidISBN(string isbn)
    {
        // Custom validation logic
        return true;
    }
}
```

**When to Use FluentValidation:**
- Complex validation rules
- Conditional validation (validate X only if Y is true)
- Cross-field validation
- Custom validators that need dependency injection
- Async validation (e.g., checking database for uniqueness)
- Reusable validation rules across multiple models

#### 4. **Additional Best Practices**

**API Gateway:**
- Add request validation at API Gateway level
- Implement throttling and rate limiting
- Add API keys for client tracking
- Enable CORS properly for production

**Security:**
- Add Lambda authorizers for authentication
- Use AWS Secrets Manager for sensitive configuration
- Implement least-privilege IAM roles
- Enable CloudTrail for audit logging

**Error Handling:**
- Implement custom exception types
- Add retry logic with exponential backoff
- Use DLQ (Dead Letter Queue) for failed invocations
- Structured error responses

**Testing:**
- Unit tests with mocked dependencies
- Integration tests with LocalStack or SAM Local
- Load testing with Artillery or k6
- Contract testing for API specifications

**Monitoring & Observability:**
- CloudWatch Dashboards for key metrics
- CloudWatch Alarms for errors and latency
- AWS X-Ray for distributed tracing
- Structured logging for better querying

**Performance:**
- DynamoDB DAX for caching (if needed)
- Lambda provisioned concurrency for predictable latency
- API Gateway caching for read-heavy endpoints
- Connection pooling for DynamoDB client

**CI/CD:**
- Automated testing in pipeline
- Multi-stage deployments (dev → staging → prod)
- Automated rollback on deployment failures
- Blue/green deployments for zero downtime

**Code Organization:**
```
BlsApi/
  ├── Common/           # Shared utilities
  ├── Functions/        # Lambda handlers
  ├── Models/           # DTOs and domain models
  ├── Repositories/     # Data access layer
  ├── Services/         # Business logic
  ├── Validators/       # Input validation
  └── Utils/            # Helper utilities
```