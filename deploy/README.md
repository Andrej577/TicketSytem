# Docker deployment

This directory contains the deployment configuration for the complete TicketSystem stack:

- PostgreSQL 16
- TicketSystem API and automatic database migrations
- TicketSystem Realtime SignalR service
- TicketSystem Blazor Web application

All commands below are run from the repository root. Docker Desktop must be running.

## Configuration

Create `deploy/.env`. If `deploy/.env.example` is available locally, it can be copied first:

```powershell
Copy-Item deploy/.env.example deploy/.env
```

The complete file has the following format. Replace the example password before starting the containers:

```dotenv
POSTGRES_DATABASE=ticket_system
POSTGRES_USER=user
POSTGRES_PASSWORD=passwd
POSTGRES_PORT=5432

WEB_PORT=8180
WEB_ORIGIN=http://localhost:8180
API_PORT=8081
REALTIME_PORT=8082
```

`deploy/.env` contains the following settings:

| Variable | Required | Example value | Compose fallback | Purpose |
| --- | --- | --- | --- | --- |
| `POSTGRES_DATABASE` | No | `ticket_system` | `ticket_system` | PostgreSQL database name. |
| `POSTGRES_USER` | No | `user` | `ticket_system` | PostgreSQL user name. |
| `POSTGRES_PASSWORD` | Yes | `passwd` | None | PostgreSQL password. Replace the example value. |
| `POSTGRES_PORT` | No | `5432` | `5432` | PostgreSQL port exposed on the host. |
| `WEB_PORT` | No | `8180` | `8180` | Web port exposed on the host. |
| `WEB_ORIGIN` | No | `http://localhost:8180` | `http://localhost:8180` | Browser origin allowed by the Realtime service. Update it when `WEB_PORT` or the public URL changes. |
| `API_PORT` | No | `8081` | `8081` | API port exposed on the host. |
| `REALTIME_PORT` | No | `8082` | `8082` | Realtime port exposed on the host. |

All ASP.NET containers listen on port `8080` internally. The Web container calls the API through `http://api:8080`, and the API connects to PostgreSQL through `database:5432`. Host port settings do not change these internal addresses.

## Standard startup

Build and start the complete stack:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d --build
```

Validate the merged Compose configuration without starting containers:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml config
```

Start existing images without rebuilding them:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d
```

The services are named `database`, `api`, `realtime`, and `web`. Default endpoints are:

- Web: `http://localhost:8180`
- API: `http://localhost:8081`
- Realtime service: `http://localhost:8082`
- SignalR hub: `http://localhost:8082/hubs/chat`
- PostgreSQL: `localhost:5432`

The API waits for PostgreSQL to become healthy and then applies pending database migrations automatically. The Dockerfile-specific `.dockerignore` files prevent local Windows `bin` and `obj` output from being copied into Linux image builds.

## Web hot reload

Use `compose.web-watch.yaml` when developing the Web project. It runs only the Web container with `dotnet watch`; API, Realtime, and PostgreSQL keep their standard configuration. Changes in `TicketSystem.Web` and its referenced `TicketSystem.Shared` project are detected automatically.

Start the complete stack with Web hot reload enabled:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml -f deploy/compose.web-watch.yaml up -d --build
```

If the standard stack is already running, recreate only Web with hot reload enabled:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml -f deploy/compose.web-watch.yaml up -d --build web
```

After the watched Web container starts, normal `.razor`, `.cs`, and CSS changes do not require another Docker command. Follow its watcher output with:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml -f deploy/compose.web-watch.yaml logs -f web
```

Switch Web back to its standard production image:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d --build --force-recreate web
```

## Common operations

Show container status:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml ps
```

View all logs or one service's logs:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml logs -f
docker compose --env-file deploy/.env -f deploy/compose.yaml logs -f api
```

Rebuild and recreate one application service:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d --build web
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d --build api
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d --build realtime
```

Stop the containers while preserving PostgreSQL data:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml down
```

Stop the containers and permanently delete PostgreSQL data:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml down --volumes
```

To recreate an empty database, run the destructive command above and then start the complete stack with `--build`. The API creates the schema by applying all migrations.

For a public deployment, set `WEB_ORIGIN` to the public Web URL. TLS should be terminated by a reverse proxy in front of these HTTP services.
