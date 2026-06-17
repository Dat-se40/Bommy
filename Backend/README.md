# Bommy Nakama Backend

Local authoritative backend for Bommy, built on Nakama with a TypeScript runtime module and PostgreSQL.

## Requirements

- Docker Desktop with Docker Compose
- Node.js and npm for local TypeScript checks

## Commands

```powershell
cd E:\Unity\Bommy\Backend
npm install
npm run type-check
npm run build
docker compose up --build
```

Nakama endpoints:

- Client API: `http://127.0.0.1:7350`
- Console: `http://127.0.0.1:7351`
- Default console login: `admin` / `password`
- PostgreSQL: `127.0.0.1:5432`, database `nakama`, user `postgres`, password `localdb`

Rebuild Nakama after TypeScript changes:

```powershell
docker compose up --build nakama
```

Stop containers while keeping the database volume:

```powershell
docker compose down
```

Reset local database data:

```powershell
docker compose down -v
```

