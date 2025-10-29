#!/bin/bash

# Setup Local DynamoDB for Development

echo "Setting up local DynamoDB..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker first."
    exit 1
fi

# Check if local DynamoDB is already running
if docker ps | grep -q amazon/dynamodb-local; then
    echo "Local DynamoDB is already running."
else
    echo "Starting local DynamoDB container..."
    docker run -d -p 8000:8000 --name dynamodb-local amazon/dynamodb-local
    sleep 2
fi

# Create the table
echo "Creating BooksTable-Dev..."
aws dynamodb create-table \
    --table-name BooksTable-Dev \
    --attribute-definitions \
        AttributeName=PK,AttributeType=S \
        AttributeName=SK,AttributeType=S \
    --key-schema \
        AttributeName=PK,KeyType=HASH \
        AttributeName=SK,KeyType=RANGE \
    --billing-mode PAY_PER_REQUEST \
    --endpoint-url http://localhost:8000 \
    2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ Table created successfully!"
else
    echo "ℹ️  Table may already exist (this is fine)"
fi

echo ""
echo "✅ Local DynamoDB is ready!"
echo "You can now run the API with: cd BlsApi && dotnet run"

