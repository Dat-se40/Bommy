# Core Lobby

Middle layer giữa UI và backend lobby. **UI không gọi REST/Nakama trực tiếp** — mọi thao tác đi qua `LobbyManager`.

Layer này dùng `LobbyManager` làm facade UI và `NakamaLobbyService` làm client RPC/socket service. Lobby runtime state nằm trong Nakama authoritative match runtime.

---

## Cấu trúc file

| File | Vai trò |
|------|---------|
| `LobbyApiContracts.cs` | Hằng số + DTO request/response. Mỗi class có ghi chú endpoint REST tương lai. |
| `LobbyManager.cs` | Singleton `MonoBehaviour`: facade cho UI, phát events, sync `GameSession`. |
| `NakamaLobbyService.cs` | RPC/socket service cho custom lobby trên Nakama authoritative match runtime. |
| `FriendsService.cs` | Nakama client friend graph wrapper: list, add, accept, decline, invite RPC. |
| `LobbyInviteService.cs` | Socket notification listener for lobby invites. |

---

## Kiến trúc

```txt
LobbyUIController
       │  Request*() / subscribe events
       ▼
LobbyManager  ──►  GameSession (roomName, mapId, maxPlayers, mapName)
       │
       ▼
NakamaLobbyService ──► Nakama RPC + socket match
```

---

## LobbyManager — cách dùng

### Khởi tạo

```csharp
LobbyManager manager = LobbyManager.EnsureExists();
```

- Nếu scene chưa có `LobbyManager`, `EnsureExists()` tự tạo `GameObject` runtime.
- Khuyến nghị: gắn component trên cùng GameObject với `LobbyUIController` trong scene `Lobby`.

### Events (subscribe từ UI)

| Event | Kiểu | Khi nào fire |
|-------|------|--------------|
| `RoomsListed` | `Action<ListRoomsResponse>` | Sau `RequestRoomList` |
| `CurrentRoomChanged` | `Action<LobbyRoomDto>` | Create/join room thành công |
| `OperationFailed` | `Action<string>` | Lỗi validate hoặc API fail |
| `FriendsListed` | `Action<FriendDto[]>` | Sau `RequestFriendsList` |
| `FriendRequestsListed` | `Action<FriendRequestDto[]>` | Sau `RequestFriendRequests` |

Luôn unsubscribe trong `OnDestroy` để tránh leak.

### API phòng

| Method | Request DTO | Mô tả |
|--------|-------------|-------|
| `RequestRoomList` | `ListRoomsRequest` (optional) | Danh sách phòng, phân trang |
| `RequestCreateRoom` | `CreateRoomRequest` | Tạo phòng, set host |
| `RequestJoinRoom` | `JoinRoomRequest` | Join theo `roomId` |
| `RequestJoinRoomByCode` | `JoinRoomByCodeRequest` | Join theo mã (header input) |
| `TryStartMatch` | — | Host bấm Start; backend chuyển lobby sang `Starting` |

### API bạn bè

| Method | Request |
|--------|---------|
| `RequestFriendsList` | — |
| `RequestFriendRequests` | — |
| `RequestAddFriend` | `AddFriendRequest` |
| `RequestAcceptFriend` | `friendId` string |
| `RequestDeclineFriend` | `friendId` string |
| `RequestInviteFriend` | `InviteFriendRequest` |
| `RequestJoinFriendRoom` | `friendId` string |

### Session

- `ApplyCurrentRoom` gọi `GameSession.SetRoom(...)` khi create/join thành công.
- `ClearCurrentRoom()` khi rời lobby (ví dụ Back to Main Menu).
- `CurrentRoom` / `HasCurrentRoom` đọc phòng hiện tại.

---

## LobbyApiContracts — DTO chính

### Rooms

- `LobbyRoomDto` — một phòng trong list hoặc phòng đang ở.
- `CreateRoomRequest` — `roomName`, `mapId`, `mapName`, `maxPlayers`, `preferredRoomId`; backend dùng `roomId` làm join password.
- `JoinRoomRequest` / `JoinRoomByCodeRequest` — `roomId` hoặc `roomCode` + `password`; password hợp lệ là chính `roomId`.

### Maps

- `LobbyMapOption` — chỉ dùng ở UI dropdown; **map id = index trong list + 1** (xem README `UI/Lobby`).

### Hằng số

- `DefaultMapId = 2` — fallback khi không có map options.
- `MaxPageSize = 20` — giới hạn page size list rooms.

---

## Backend runtime

- Active lobby state lives in Nakama authoritative match runtime, not local mock room data.
- `list_lobbies` reads live match labels and returns open custom lobbies.
- `create_lobby`, `join_lobby`, `join_lobby_by_code`, `leave_lobby`, and `start_lobby_match` are Nakama RPCs.
- Friends use Nakama's built-in friend APIs on the client.
- Lobby invites use backend RPC `invite_lobby_friend`, which validates lobby membership and sends a Nakama notification.

---

## Backend contract

1. Giữ nguyên class trong `LobbyApiContracts.cs` để UI không đổi.
2. Lỗi RPC/socket → gọi `Fail(message)` để UI nhận qua `OperationFailed`.
3. Thành công → invoke event tương ứng (`RoomsListed`, `CurrentRoomChanged`, ...).
4. `TryStartMatch` không load game scene trong Phase 3; Phase 5 sẽ cấp server PurrNet.

```csharp
void Fail(string message)
{
    FlowGuard.Error(FlowGuard.TagSetup, message);
    OperationFailed?.Invoke(message);
}
```

---

## Liên kết gameplay

Sau join/create, `GameSession` nhận:

- `MapId` — dùng bởi `MatchPrepState` → `MatchPhaseBroadcast.ServerSetActiveMap`
- `MapLoader` load prefab theo `mapId` trong `MapEntry[]`

**Quan trọng:** `mapId` từ lobby phải khớp `MapLoader.maps[].mapId` trong scene game.

---

## Checklist scene Lobby

- [ ] GameObject có `LobbyManager` (hoặc rely `EnsureExists()`)
- [ ] `LobbyUIController` subscribe events trong `Start`, unsubscribe `OnDestroy`
- [ ] Không gọi networking trực tiếp từ UI

Xem thêm: [`Assets/Scripts/UI/Lobby/README.md`](../../UI/Lobby/README.md)
