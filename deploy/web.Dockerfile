FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TicketSystem.Web/TicketSystem.Web.csproj", "src/TicketSystem.Web/"]
COPY ["src/TicketSystem.Client/TicketSystem.Client.csproj", "src/TicketSystem.Client/"]
COPY ["src/TicketSystem.SharedUI/TicketSystem.SharedUI.csproj", "src/TicketSystem.SharedUI/"]
COPY ["src/TicketSystem.Shared/TicketSystem.Shared.csproj", "src/TicketSystem.Shared/"]
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
RUN mkdir -p /var/ticketsystem/data-protection \
    && touch /var/ticketsystem/data-protection/.keep \
    && chown -R app:app /var/ticketsystem
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketSystem.Web.dll"]
