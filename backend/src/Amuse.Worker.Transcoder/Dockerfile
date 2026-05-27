FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg ca-certificates \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Amuse.Worker.Transcoder/Amuse.Worker.Transcoder.csproj", "Amuse.Worker.Transcoder/"]
COPY ["Amuse.Modules/Amuse.Modules.csproj", "Amuse.Modules/"]
COPY ["Amuse.Domain/Amuse.Domain.csproj", "Amuse.Domain/"]
RUN dotnet restore "Amuse.Worker.Transcoder/Amuse.Worker.Transcoder.csproj"
COPY . .
WORKDIR "/src/Amuse.Worker.Transcoder"
RUN dotnet publish "./Amuse.Worker.Transcoder.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Amuse.Worker.Transcoder.dll"]

