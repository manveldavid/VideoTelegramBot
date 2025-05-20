# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# USER app
WORKDIR /app
EXPOSE 8000
EXPOSE 8001
EXPOSE 8443

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VideoTelegramBot.csproj", "."]
RUN dotnet restore "./VideoTelegramBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./VideoTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --chmod=0755 --from=mwader/static-ffmpeg:7.1.1 /ffmpeg .
COPY --chmod=0755 --from=mwader/static-ffmpeg:7.1.1 /ffprobe .

ENTRYPOINT ["dotnet", "VideoTelegramBot.dll"]