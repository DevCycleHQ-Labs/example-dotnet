# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project file and restore dependencies
COPY HelloTogglebot.csproj .
RUN dotnet restore

# Copy source code
COPY . .

# Build the application
RUN dotnet publish -c Release -o out

# Use the official .NET 8.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application
COPY --from=build /app/out .

# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose port 8080 (standard for containerized apps)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "HelloTogglebot.dll"]
