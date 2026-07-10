FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TicketSystem.Web/TicketSystem.Web.csproj", "src/TicketSystem.Web/"]
RUN dotnet restore "src/TicketSystem.Web/TicketSystem.Web.csproj"

COPY . .
RUN dotnet publish "src/TicketSystem.Web/TicketSystem.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketSystem.Web.dll"]
