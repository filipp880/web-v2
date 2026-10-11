FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TramWebApp.sln", "."]
COPY ["src/TramWebApp.Domain/TramWebApp.Domain.csproj", "src/TramWebApp.Domain/"]
COPY ["src/TramWebApp.Application/TramWebApp.Application.csproj", "src/TramWebApp.Application/"]
COPY ["src/TramWebApp.Infrastructure/TramWebApp.Infrastructure.csproj", "src/TramWebApp.Infrastructure/"]
COPY ["src/TramWebApp.Api/TramWebApp.Api.csproj", "src/TramWebApp.Api/"]

RUN dotnet restore "TramWebApp.sln"

COPY . .
WORKDIR "/src/src/TramWebApp.Api"
RUN dotnet build "TramWebApp.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TramWebApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TramWebApp.Api.dll"]