# Setup Local DynamoDB for Development (PowerShell)

Write-Host "Setting up local DynamoDB..." -ForegroundColor Cyan

# Check if Docker is running
try {
    docker info | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running"
    }
} catch {
    Write-Host "Error: Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Check if local DynamoDB is already running
$existingContainer = docker ps --filter "ancestor=amazon/dynamodb-local" --format "{{.Names}}"
if ($existingContainer) {
    Write-Host "Local DynamoDB is already running." -ForegroundColor Yellow
} else {
    Write-Host "Starting local DynamoDB container..." -ForegroundColor Cyan
    docker run -d -p 8000:8000 --name dynamodb-local amazon/dynamodb-local
    Start-Sleep -Seconds 2
}

# Create the table
Write-Host "Creating BooksTable-Dev..." -ForegroundColor Cyan
$createTableOutput = aws dynamodb create-table `
    --table-name BooksTable-Dev `
    --attribute-definitions `
        AttributeName=PK,AttributeType=S `
        AttributeName=SK,AttributeType=S `
    --key-schema `
        AttributeName=PK,KeyType=HASH `
        AttributeName=SK,KeyType=RANGE `
    --billing-mode PAY_PER_REQUEST `
    --endpoint-url http://localhost:8000 `
    2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Table created successfully!" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Table may already exist (this is fine)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✅ Local DynamoDB is ready!" -ForegroundColor Green
Write-Host "You can now run the API with: cd BlsApi; dotnet run" -ForegroundColor Cyan
