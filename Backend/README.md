# Bommy Backend

Local ASP.NET Core backend for Bommy P2 networking/auth testing.

## Run

```powershell
dotnet run --project E:\Unity\Bommy\Backend --urls http://localhost:8080
```

Scalar API reference:

```text
http://localhost:8080/scalar
```

OpenAPI document:

```text
http://localhost:8080/openapi/v1.json
```

## Supported local endpoints

- `POST /v1/auth/dev`
- `POST /v1/auth/steam`
- `GET /v1/players/me`
- `POST /v1/shop/purchase`
- `POST /v1/matches/create`
- `POST /v1/matches/{matchId}/server-ready`
- `POST /v1/matches/{matchId}/validate-player`
- `GET /health`

This first cut uses in-memory accounts and matches. It is intended for local multi-client testing only; real Steam ticket validation, persistence, and EdgeGap deployment orchestration are separate follow-up pieces.

For local testing, `/v1/matches/create` only returns an allocation after the headless Unity server has registered `/v1/matches/{matchId}/server-ready`. If the server is not running, create returns `success: false` with a `match server is not ready` error.

## Local flow

Start the backend first:

```powershell
dotnet run --project E:\Unity\Bommy\Backend --urls http://localhost:8080
```

Start a Unity server:

```powershell
Bommy.exe -batchmode -nographics -server -port 5000 -matchId local-dev-match-1 -backendUrl http://localhost:8080
```

Start dev clients:

```powershell
Bommy.exe -authProvider dev -devAuthId dev-client-1 -backendUrl http://localhost:8080
Bommy.exe -authProvider dev -devAuthId dev-client-2 -backendUrl http://localhost:8080
```
