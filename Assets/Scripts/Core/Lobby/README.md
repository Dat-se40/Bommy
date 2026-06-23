# Core Lobby

Middle layer giữa UI và backend lobby. **UI không gọi REST/Nakama trực tiếp** — mọi thao tác đi qua `LobbyManager`.

Hiện tại layer này dùng **mock data trong memory**. Khi backend sẵn sàng, chỉ cần thay body các method `Request*` bằng HTTP/RPC thật; giữ nguyên DTO và events.

---

## Cấu trúc file

| File | Vai trò |
|------|---------|
| `LobbyApiContracts.cs` | Hằng số + DTO request/response. Mỗi class có ghi chú endpoint REST tương lai. |
| `LobbyManager.cs` | Singleton `MonoBehaviour`: xử lý request, phát events, sync `GameSession`. |

---

## Kiến trúc

```txt
LobbyUIController
       │  Request*() / subscribe events
       ▼
LobbyManager  ──►  GameSession (roomName, mapId, maxPlayers, mapName)
       │
       ▼
[Mock]  hoặc  [Nakama RPC / REST]  (sau này)
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
| `TryStartMatch` | — | Host bấm Start; sync trước khi load GameScene |

### API bạn bè (stub)

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
- `CreateRoomRequest` — `roomName`, `mapId`, `mapName`, `maxPlayers`, `password`, `preferredRoomId`.
- `JoinRoomRequest` / `JoinRoomByCodeRequest` — `roomId` hoặc `roomCode` + `password` (nếu private).

### Maps

- `LobbyMapOption` — chỉ dùng ở UI dropdown; **map id = index trong list + 1** (xem README `UI/Lobby`).

### Hằng số

- `DefaultMapId = 2` — fallback khi không có map options.
- `MaxPageSize = 20` — giới hạn page size list rooms.

---

## Mock data (test local)

`SeedMockData()` chạy trong `Awake`. Một số phòng mẫu:

| Room ID | Private | Password mock |
|---------|---------|---------------|
| `VN01` | Có | `123` |
| `DEV1` | Có | `dev` |
| Các phòng khác | Tùy `isPrivate` | Theo `PlayerPrefs` nếu bạn tự tạo phòng có password |

Phòng bạn tạo qua UI: password lưu `PlayerPrefs` (`CurrentRoomPassword`) để validate join sau.

---

## Migrate sang backend thật

1. Giữ nguyên class trong `LobbyApiContracts.cs` (hoặc map JSON response vào cùng shape).
2. Trong `LobbyManager`, thay từng region:
   - `#region Rooms — API: ListRooms` → `GET /v1/lobbies`
   - `CreateRoom` → `POST /v1/lobbies`
   - `JoinRoom` → `POST /v1/lobbies/{id}/join`
   - `TryStartMatch` → `POST /v1/lobbies/{id}/start` + PurrNet / EdgeGap
3. Lỗi HTTP → gọi `Fail(message)` để UI nhận qua `OperationFailed`.
4. Thành công → invoke event tương ứng (`RoomsListed`, `CurrentRoomChanged`, ...).

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
