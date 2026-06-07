# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

RUN --mount=type=cache,id=apt-cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,id=apt-lib,target=/var/lib/apt,sharing=locked \
    apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg ca-certificates \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Amuse.Worker.Transcoder/Amuse.Worker.Transcoder.csproj", "Amuse.Worker.Transcoder/"]
COPY ["Amuse.Modules/Amuse.Modules.csproj", "Amuse.Modules/"]
COPY ["Amuse.Domain/Amuse.Domain.csproj", "Amuse.Domain/"]
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    --mount=type=cache,id=nuget-http,target=/root/.local/share/NuGet/http-cache,sharing=locked \
    dotnet restore "Amuse.Worker.Transcoder/Amuse.Worker.Transcoder.csproj"
COPY . .
WORKDIR "/src/Amuse.Worker.Transcoder"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    --mount=type=cache,id=nuget-http,target=/root/.local/share/NuGet/http-cache,sharing=locked \
    dotnet publish "./Amuse.Worker.Transcoder.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV DOTNET_ENVIRONMENT=Production
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Amuse.Worker.Transcoder.dll"]

