# Use Linux-based .NET 8.0 runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 🔥 FIXED: Copy the entire solution to restore dependencies
COPY ["V1.0Samples/LocalMediaSamples/HueBot/HueBot/HueBot.csproj", "HueBot/"]
COPY ["Common/Sample.Common/Sample.Common.csproj", "Sample.Common/"]

RUN dotnet restore "HueBot/HueBot.csproj"

# Copy all source files
COPY . .
WORKDIR "/src/V1.0Samples/LocalMediaSamples/HueBot/HueBot"
RUN dotnet build "HueBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HueBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HueBot.dll"]
