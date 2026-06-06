# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
EXPOSE 6000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Amuse.Api/Amuse.Api.csproj", "Amuse.Api/"]
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    --mount=type=cache,id=nuget-http,target=/root/.local/share/NuGet/http-cache,sharing=locked \
    dotnet restore "Amuse.Api/Amuse.Api.csproj"
COPY . .
WORKDIR "/src/Amuse.Api"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    --mount=type=cache,id=nuget-http,target=/root/.local/share/NuGet/http-cache,sharing=locked \
    dotnet build "./Amuse.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    --mount=type=cache,id=nuget-http,target=/root/.local/share/NuGet/http-cache,sharing=locked \
    dotnet publish "./Amuse.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Amuse.Api.dll"]
