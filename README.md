# Bommy

Bommy is a Unity 2D multiplayer bomber-style game prototype. The project includes the Unity client/server game code, PurrNet networking, Nakama authentication and progression RPCs, character selection, lobby and random match flows, tilemap destruction, bombs, effects, and a local Nakama backend for account and player data tests.

## Requirements

- Unity `6000.4.10f1`
- Docker Desktop with Docker Compose for the Nakama backend
- Node.js and npm for backend TypeScript checks/builds
- Windows standalone build target for local server/client testing

Key Unity packages include URP 2D, Input System, PurrNet, PurrDiction, Steamworks.NET, DOTween, EdgeGap Unity server plugin, and the Unity Test Framework.

## Project Layout

- `Assets/Scenes` - main Unity scenes: `MainMenu`, `CharacterSelect`, `Lobby`, `RandomMatch`, `GameScene`, and `SampleScene`.
- `Assets/Scripts/Core` - networking, runtime mode, Nakama auth, player progression, session state, level config, sound, and diagnostics.
- `Assets/Scripts/Gameplay` - player movement, bombs, explosions, map/tilemap handling, item drops, and effects.
- `Assets/Scripts/UI` - menu, lobby, character select, in-game HUD, pause, and game over UI.
- `Assets/Tests/EditMode` - edit mode tests for flow guard and match join serialization.
- `Backend` - local Nakama backend with a TypeScript runtime module and PostgreSQL.
- `Builds` - local build output area.

## Getting Started

1. Open the repository folder in Unity Hub with Unity `6000.4.10f1`.
2. Let Unity restore packages from `Packages/manifest.json`.
3. Start the local Nakama backend:

   ```powershell
   cd E:\Unity\Bommy\Backend
   npm install
   npm run build
   docker compose up --build
   ```

4. Open `Assets/Scenes/MainMenu.unity` or use the configured build scene order in `ProjectSettings/EditorBuildSettings.asset`.
5. Press Play. The client connects to Nakama at `http://127.0.0.1:7350` using the default `AuthService` settings.

## Local Backend

Start Nakama and PostgreSQL from the backend folder:

```powershell
cd E:\Unity\Bommy\Backend
docker compose up --build
```

Useful local services:

- Nakama client API: `http://127.0.0.1:7350`
- Nakama console: `http://127.0.0.1:7351`
- Default console login: `admin` / `password`
- PostgreSQL: `127.0.0.1:5432`, database `nakama`, user `postgres`, password `localdb`

After TypeScript runtime changes, rebuild Nakama:

```powershell
cd E:\Unity\Bommy\Backend
npm run build
docker compose up --build nakama
```

See `Backend/README.md` for backend-specific commands.

## Local Multiplayer Flow

For the current local auth/progression loop, start the backend and run the game:

```powershell
cd E:\Unity\Bommy\Backend
docker compose up --build
```

Then press Play in Unity or run a local build. Use the AuthGate UI to register, sign in, or use guest login. Player progression and character purchases are served through Nakama RPCs.

Headless PurrNet server/client launch flows are still available for gameplay networking work, but the auth/progression backend is Nakama on port `7350`.

## Testing

Run Unity edit mode tests from the Unity Test Runner, or via batch mode with the Unity editor installed:

```powershell
Unity.exe -batchmode -projectPath E:\Unity\Bommy -runTests -testPlatform EditMode -quit
```

## Notes

- The backend uses Nakama with PostgreSQL for local account/session/progression state.
- Steam authentication and EdgeGap deployment orchestration are represented by hooks/settings and may require additional production implementation.
- Generated Unity folders such as `Library`, `Logs`, `obj`, and `bin` should stay out of source control.
