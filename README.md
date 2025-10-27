# Books Lending Service

A serverless application built with AWS CDK that provides REST API endpoints for managing a library's book lending system.

## API Endpoints

- `POST /books` – Add a new book
- `GET /books` – List all books
- `POST /books/{isbn}/checkout` – Check out a book
- `POST /books/{isbn}/return` – Return a book

## Testing with Postman

1. Import the `Books-Lending-Service.postman_collection.json` file into Postman
2. Update the `baseUrl` variable in your Postman environment with your API Gateway URL
3. Use the provided sample requests to test each endpoint

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

# Synthesize CloudFormation template
cdk synth

# Deploy to AWS
cdk deploy
```