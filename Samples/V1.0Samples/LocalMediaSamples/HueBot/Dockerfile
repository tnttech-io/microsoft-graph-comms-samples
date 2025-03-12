# Use the ASP.NET 8.0 base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

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
RUN dotnet publish "HueBot.csproj" -c Release -o /app/publish --no-build

# Final stage to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HueBot.dll"]
