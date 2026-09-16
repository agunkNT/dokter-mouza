Write-Host "Running Dokter Abdurrahman Automation Tests..." -ForegroundColor Cyan

# Restore dependencies
dotnet restore

# Run tests
dotnet test DokterAbdurrahman.Tests/DokterAbdurrahman.Tests.csproj --logger "console;verbosity=normal"

Write-Host "Tests Completed." -ForegroundColor Green
