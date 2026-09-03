$ErrorActionPreference = "Stop"
dotnet restore
dotnet build KeyGlance.sln
dotnet test
