FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TicketSystem.Api/TicketSystem.Api.csproj", "src/TicketSystem.Api/"]
COPY ["src/TicketSystem.Shared/TicketSystem.Shared.csproj", "src/TicketSystem.Shared/"]
RUN dotnet restore "src/TicketSystem.Api/TicketSystem.Api.csproj"

COPY . .
RUN dotnet publish "src/TicketSystem.Api/TicketSystem.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketSystem.Api.dll"]
