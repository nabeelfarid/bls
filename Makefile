.PHONY: build deploy
build:
	dotnet publish BlsApi/BlsApi.csproj -c Release
	dotnet build BlsCdkApp/BlsCdkApp.csproj -c Release
	cdk synth

deploy: build
	cdk deploy
