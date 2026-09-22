@echo off
cd /d %~dp0..
dotnet restore JwtAppToApp.NuGet.sln
dotnet build JwtAppToApp.NuGet.sln -c Release
dotnet pack src\JwtAppToApp.Client\JwtAppToApp.Client.csproj -c Release -o nuget
echo Done -^> nuget\JwtAppToApp.Client.1.0.0.nupkg
