.PHONY: build test deploy clean

build:
	dotnet publish BlsApi/BlsApi.csproj -c Release
	dotnet build BlsCdkApp/BlsCdkApp.csproj -c Release

test:
	dotnet test

deploy: test build
	cdk synth
	cdk deploy

clean:
	dotnet clean
	rm -rf cdk.out
	rm -rf BlsApi/bin BlsApi/obj
	rm -rf BlsApi.Tests/bin BlsApi.Tests/obj
	rm -rf BlsCdkApp/bin BlsCdkApp/obj
