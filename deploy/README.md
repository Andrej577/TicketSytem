# Docker deployment

This directory contains the deployment configuration for the complete TicketSystem stack:

- PostgreSQL 16
- TicketSystem API and automatic database migrations
- TicketSystem Realtime SignalR service
- TicketSystem Blazor Web application

## Start the stack

Create the local environment file and replace the example database password:

```powershell
Copy-Item deploy/.env.example deploy/.env
```

From the repository root, build and start all containers:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d --build
```

The Dockerfile-specific `.dockerignore` files exclude local Windows `bin` and
`obj` output from the Linux image builds. This prevents local NuGet asset paths
from overwriting the assets generated inside the containers.

Default endpoints:

- Web: `http://localhost:8180`
- API health: `http://localhost:8081/api/health`
- SignalR hub: `http://localhost:8082/hubs/chat`
- PostgreSQL: `localhost:5432`

The PostgreSQL host connection uses the values from `deploy/.env`. For example,
connect to `localhost` on `POSTGRES_PORT` with `POSTGRES_DATABASE`,
`POSTGRES_USER`, and `POSTGRES_PASSWORD` from that file.

The API waits for PostgreSQL to become healthy and then applies pending database migrations automatically.

## Operations

View logs:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml logs -f
```

Stop the containers while preserving database data:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml down
```

Stop the containers and delete the database volume:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yaml down --volumes
```

For a public deployment, set `WEB_ORIGIN` to the public Web URL. TLS should be terminated by a reverse proxy in front of these HTTP services.
