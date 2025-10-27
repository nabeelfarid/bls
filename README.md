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

### Validation Error Response

When validation fails, the API returns a `400 Bad Request` with error details:

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

## Architecture Decisions

### Current Implementation

- **Serverless Architecture**: Built using AWS Lambda + API Gateway + DynamoDB for cost-effectiveness and automatic scaling
- **Infrastructure as Code**: AWS CDK with C# for type-safe infrastructure definitions
- **Single-Table Design**: DynamoDB with composite keys (PK/SK) for flexible data modeling
- **Lambda per Endpoint**: Each API endpoint has its own Lambda function for independent scaling and deployment
- **Request Logging**: Centralized `RequestLogger` utility for consistent logging across all handlers
- **JSON Serialization**: Lowercase property names for REST API convention compliance

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

#### 3. **Additional Best Practices**

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