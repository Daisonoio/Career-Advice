FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Aegis.API/Aegis.API.csproj", "src/Aegis.API/"]
COPY ["src/Aegis.Application/Aegis.Application.csproj", "src/Aegis.Application/"]
COPY ["src/Aegis.Domain/Aegis.Domain.csproj", "src/Aegis.Domain/"]
COPY ["src/Aegis.Infrastructure/Aegis.Infrastructure.csproj", "src/Aegis.Infrastructure/"]
RUN dotnet restore "src/Aegis.API/Aegis.API.csproj"

COPY . .
WORKDIR "/src/src/Aegis.API"
RUN dotnet build "Aegis.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Aegis.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aegis.API.dll"]
