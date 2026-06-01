FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Amuse.Worker.Scheduler/Amuse.Worker.Scheduler.csproj", "Amuse.Worker.Scheduler/"]
COPY ["Amuse.Modules/Amuse.Modules.csproj", "Amuse.Modules/"]
COPY ["Amuse.Domain/Amuse.Domain.csproj", "Amuse.Domain/"]
RUN dotnet restore "Amuse.Worker.Scheduler/Amuse.Worker.Scheduler.csproj"
COPY . .
WORKDIR "/src/Amuse.Worker.Scheduler"
RUN dotnet publish "./Amuse.Worker.Scheduler.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Amuse.Worker.Scheduler.dll"]
