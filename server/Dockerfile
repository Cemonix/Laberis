# Use the official .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy solution file and project files
COPY *.sln ./
COPY Server/server.csproj Server/
COPY Server.Tests/Server.Tests.csproj Server.Tests/

# Copy everything else
COPY . ./

# Restore dependencies
RUN dotnet restore

# Build and publish the application
RUN dotnet publish Server/server.csproj -c Release -o out --no-restore

# Use the official .NET 9.0 runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install ca-certificates and curl for health checks
RUN apt-get update && apt-get install -y ca-certificates curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build-env /app/out .

# Create a non-root user
RUN addgroup --system --gid 1001 laberis && \
    adduser --system --uid 1001 --gid 1001 laberis

# Change ownership of the app directory
RUN chown -R laberis:laberis /app
USER laberis

# Expose the port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "server.dll"]