.PHONY: help build test run-local deploy clean restore

help:
	@echo "📚 Books Lending Service - Available Commands"
	@echo ""
	@echo "  make run-local    - Run the API locally (Development mode)"
	@echo "  make test         - Run all tests"
	@echo "  make build        - Build and publish the API"
	@echo "  make deploy       - Deploy to AWS (runs tests first)"
	@echo "  make clean        - Clean all build artifacts"
	@echo "  make restore      - Restore NuGet packages"
	@echo ""

# Run the API locally in Development mode
run-local:
	@echo "🚀 Starting Books Lending Service locally..."
	@echo "📍 API will be available at: http://localhost:5000"
	@echo "📍 Swagger UI: http://localhost:5000/swagger"
	@echo ""
	cd BlsApi && ASPNETCORE_ENVIRONMENT=Development dotnet run

# Restore NuGet packages
restore:
	@echo "📦 Restoring NuGet packages..."
	dotnet restore

# Build and publish the API for deployment
build:
	@echo "🔨 Publishing Books Lending Service API..."
	dotnet publish BlsApi/BlsApi.csproj -c Release
	@echo "🔨 Building CDK App for Books Lending Service API..."
	dotnet build BlsCdkApp/BlsCdkApp.csproj

# Run all tests
test:
	@echo "🧪 Running tests..."
	dotnet test

# Deploy to AWS (runs tests first)
deploy: test build
	@echo "☁️  Deploying to AWS..."
	@echo "This will create/update:"
	@echo "  - DynamoDB table (BooksTable)"
	@echo "  - Lambda function (BooksApiFunction)"
	@echo "  - API Gateway (Books Lending Service)"
	@echo ""
	cdk deploy --require-approval never
	@echo ""
	@echo "✅ Deployment complete!"
	@echo ""
	@echo "Your API is now live! Check the 'ApiUrl' output above to get your API endpoint."
	@echo ""
	@echo "Example usage:"
	@echo "  curl \$API_URL/api/books"
	@echo ""
	@echo "You can also get your API URL with:"
	@echo "  aws cloudformation describe-stacks --stack-name BlsCdkAppStack --query 'Stacks[0].Outputs[?OutputKey==\`ApiUrl\`].OutputValue' --output text"

# Clean all build artifacts
clean:
	@echo "🧹 Cleaning build artifacts..."
	dotnet clean
	rm -rf cdk.out
	rm -rf BlsApi/bin BlsApi/obj
	rm -rf BlsApi.Tests/bin BlsApi.Tests/obj
	rm -rf BlsCdkApp/bin BlsCdkApp/obj
	@echo "✅ Clean complete!"
