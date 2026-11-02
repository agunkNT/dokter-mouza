# Multi-stage Dockerfile for .NET 10 Web App
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY Website/DokterAbdurrahman.csproj Website/
RUN dotnet restore Website/DokterAbdurrahman.csproj

# Copy the rest of the source code and publish
COPY Website/ Website/
WORKDIR /src/Website
RUN dotnet publish DokterAbdurrahman.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Expose default HTTP port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Start the application
ENTRYPOINT ["dotnet", "DokterAbdurrahman.dll"]
