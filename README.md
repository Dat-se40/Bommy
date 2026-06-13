# Bommy

Bommy is a Unity 2D multiplayer bomber-style game prototype. The project includes the Unity client/server game code, PurrNet networking, Steam/dev authentication hooks, character selection, lobby and random match flows, tilemap destruction, bombs, effects, and a small local backend for account and matchmaking tests.

## Requirements

- Unity `6000.4.10f1`
- .NET SDK with `net10.0` support for the backend
- Windows standalone build target for local server/client testing

Key Unity packages include URP 2D, Input System, PurrNet, PurrDiction, Steamworks.NET, DOTween, EdgeGap Unity server plugin, and the Unity Test Framework.

## Project Layout

- `Assets/Scenes` - main Unity scenes: `MainMenu`, `CharacterSelect`, `Lobby`, `RandomMatch`, `GameScene`, and `SampleScene`.
- `Assets/Scripts/Core` - networking, runtime mode, auth, backend API client, session state, level config, sound, and diagnostics.
- `Assets/Scripts/Gameplay` - player movement, bombs, explosions, map/tilemap handling, item drops, and effects.
- `Assets/Scripts/UI` - menu, lobby, character select, in-game HUD, pause, and game over UI.
- `Assets/Tests/EditMode` - edit mode tests for flow guard and match join serialization.
- `Backend` - local ASP.NET Core API used for development auth, player data, shop purchases, and match allocation.
- `Builds` - local build output area.

## Getting Started

1. Open the repository folder in Unity Hub with Unity `6000.4.10f1`.
2. Let Unity restore packages from `Packages/manifest.json`.
3. Open `Assets/Scenes/MainMenu.unity` or use the configured build scene order in `ProjectSettings/EditorBuildSettings.asset`.
4. Press Play for editor testing, or create Windows standalone builds for multi-client/server testing.

## Local Backend

Start the backend on port `8080`:

```powershell
dotnet run --project E:\Unity\Bommy\Backend --urls http://localhost:8080
```

Useful backend URLs:

- API reference: `http://localhost:8080/scalar`
- OpenAPI document: `http://localhost:8080/openapi/v1.json`
- Health check: `http://localhost:8080/health`

See `Backend/README.md` for the full endpoint list and local match flow.

## Local Multiplayer Flow

After building `Bommy.exe`, run one headless server and one or more dev clients:

```powershell
Bommy.exe -batchmode -nographics -server -port 5000 -matchId local-dev-match-1 -backendUrl http://localhost:8080
Bommy.exe -authProvider dev -devAuthId dev-client-1 -backendUrl http://localhost:8080
Bommy.exe -authProvider dev -devAuthId dev-client-2 -backendUrl http://localhost:8080
```

Runtime options can also be supplied through environment variables such as `SERVER_PORT`, `PORT`, `MATCH_ID`, `BACKEND_URL`, `AUTH_PROVIDER`, and `DEV_AUTH_ID`.

## Testing

Run Unity edit mode tests from the Unity Test Runner, or via batch mode with the Unity editor installed:

```powershell
Unity.exe -batchmode -projectPath E:\Unity\Bommy -runTests -testPlatform EditMode -quit
```

## Notes

- The backend currently uses in-memory accounts and matches for local development.
- Steam authentication, persistence, and EdgeGap deployment orchestration are represented by hooks/settings and may require additional production implementation.
- Generated Unity folders such as `Library`, `Logs`, `obj`, and `bin` should stay out of source control.
