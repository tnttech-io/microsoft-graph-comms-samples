# Use the latest ASP.NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 9444
EXPOSE 8445

# Use the .NET SDK 8.0 for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["HueBot/HueBot.csproj", "HueBot/"]
RUN dotnet restore "HueBot/HueBot.csproj"

# Copy all source files AFTER restoring dependencies
COPY . .
WORKDIR /src/HueBot

# Build and publish the application in one step
RUN dotnet publish "HueBot.csproj" -c Release -o /app/publish --no-restore --self-contained false

# Final stage: Use the runtime image
FROM base AS final
WORKDIR /app

# Set up a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN chown -R appuser:appgroup /app
USER appuser

# Copy the published app from the previous stage
COPY --from=build /app/publish .

# Define health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
  CMD wget --spider -q http://localhost:9444/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "HueBot.dll"]
