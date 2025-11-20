FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln .
COPY *.csproj .
COPY . .

# Restore and build
RUN dotnet restore EnterpriseGradeInventoryManagementSystem.sln
RUN dotnet publish EnterpriseGradeInventoryManagementSystem.sln -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Set environment
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

ENTRYPOINT ["dotnet", "EnterpriseGradeInventoryAPI.dll"]
