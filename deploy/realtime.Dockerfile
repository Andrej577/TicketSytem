FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TicketSystem.Realtime/TicketSystem.Realtime.csproj", "src/TicketSystem.Realtime/"]
RUN dotnet restore "src/TicketSystem.Realtime/TicketSystem.Realtime.csproj"

COPY . .
RUN dotnet publish "src/TicketSystem.Realtime/TicketSystem.Realtime.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketSystem.Realtime.dll"]
