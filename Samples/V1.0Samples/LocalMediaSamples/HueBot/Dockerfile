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

# Copy all source files and build the application
COPY . .
WORKDIR /src/HueBot
RUN dotnet build "HueBot.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "HueBot.csproj" -c Release -o /app/publish --no-build --self-contained false

# Final stage: Use the runtime image
FROM base AS final
WORKDIR /app

# Set up a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

# Copy the published app from the previous stage
COPY --from=publish /app/publish .

# Define health check (optional)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
  CMD curl --fail http://localhost:9444/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "HueBot.dll"]
