using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Middle layer duy nhất giữa các scene cho match/profile.
/// UI và spawner chỉ nói chuyện qua đây.
/// </summary>
public static class MatchSessionBroker
{
    const string CharacterIdKey = "SelectedCharacterId";
    const string CharacterIndexKey = "SelectedCharacterIndex";
    const string CharacterNameKey = "SelectedCharacterName";
    const string CharacterHpKey = "SelectedCharacterHp";
    const string CharacterBombKey = "SelectedCharacterBomb";
    const string CharacterSpeedKey = "SelectedCharacterSpeed";

    static CharacterDatabase characterCatalog;
    static PlayerMatchProfile localPlayer;
    static MatchServerAllocation matchAllocation;
    static readonly List<PlayerMatchProfile> roster = new();
    static readonly Dictionary<ulong, PlayerMatchProfile> networkProfiles = new();

    public static event Action RosterChanged;

    public static CharacterDatabase CharacterCatalog => characterCatalog;
    public static IReadOnlyList<PlayerMatchProfile> Roster => roster;

    public static void SetCharacterCatalog(CharacterDatabase database)
    {
        characterCatalog = database;
    }

    public static void CommitLocalSelection(PlayerMatchProfile profile)
    {
        if (!FlowGuard.IsValidSpawnProfile(profile, out string reason))
        {
            FlowGuard.Error(FlowGuard.TagSetup, $"CommitLocalSelection rejected: {reason}");
            return;
        }

        localPlayer = profile;
        PersistLocalToPlayerPrefs(profile);
        SyncLegacyGameSession(profile);

        UpsertRosterSlot(profile);
        FlowGuard.Info(
            FlowGuard.TagSetup,
            $"Committed local: {profile.displayName} id={profile.characterId}"
        );

        // TODO[NETWORK] Gửi profile lên server khi join room.
    }

    public static PlayerMatchProfile GetLocalPlayer()
    {
        return localPlayer;
    }

    public static bool TryGetRosterSlot(int slotIndex, out PlayerMatchProfile profile)
    {
        for (int i = 0; i < roster.Count; i++)
        {
            if (roster[i].slotIndex == slotIndex)
            {
                profile = roster[i];
                return true;
            }
        }

        profile = default;
        return false;
    }

    public static void ApplyAccountSnapshot(PlayerAccountSnapshot account)
    {
        if (account == null)
            return;

        PlayerPrefs.SetInt("PlayerGold", account.gold);
        PlayerPrefs.SetInt("PlayerLevel", account.level);
        PlayerPrefs.SetString("PlayerDisplayName", account.displayName);

        if (account.ownedCharacterIds != null)
        {
            for (int i = 0; i < account.ownedCharacterIds.Length; i++)
                PlayerPrefs.SetInt(GetOwnedKey(account.ownedCharacterIds[i]), 1);
        }

        PlayerPrefs.Save();
    }

    public static void CommitRoom(string roomName, string mapName, int maxPlayers)
    {
        GameSession.SetRoom(roomName, mapName, maxPlayers);
        FlowGuard.Info(
            FlowGuard.TagSetup,
            $"Committed room: {roomName} map={mapName} maxPlayers={maxPlayers}"
        );
    }

    public static void SetMatchAllocation(MatchServerAllocation allocation)
    {
        matchAllocation = allocation;

        if (allocation == null)
            return;

        if (!string.IsNullOrWhiteSpace(allocation.matchId))
            GameSession.RoomName = allocation.matchId;

        FlowGuard.Info(
            FlowGuard.TagNetwork,
            $"Stored match allocation: {allocation.host}:{allocation.port} matchId={allocation.matchId}"
        );
    }

    public static bool TryGetMatchAllocation(out MatchServerAllocation allocation)
    {
        allocation = matchAllocation;
        return allocation != null &&
            !string.IsNullOrWhiteSpace(allocation.host) &&
            allocation.port > 0;
    }

    public static void LoadLocalFromPlayerPrefs(CharacterDatabase database)
    {
        if (database == null)
            return;

        int characterId = PlayerPrefs.GetInt(CharacterIdKey, 0);
        int catalogIndex = database.GetIndexById(characterId);

        if (catalogIndex < 0 && database.Count > 0)
        {
            catalogIndex = 0;
            characterId = database.GetByIndex(0).CharacterId;
        }

        CharacterDefinition definition = database.GetById(characterId);

        if (definition == null)
            return;

        string displayName = PlayerPrefs.GetString(
            CharacterNameKey,
            PlayerPrefs.GetString("PlayerDisplayName", definition.CharacterName)
        );

        localPlayer = PlayerMatchProfile.FromDefinition(
            definition,
            catalogIndex,
            slotIndex: 0,
            isLocal: true,
            displayNameOverride: displayName
        );
    }

    public static void SeedSampleLocalPlayer(SamplePlayerDataSheet sheet)
    {
        if (sheet == null)
            return;

        if (sheet.account != null)
            ApplyAccountSnapshot(sheet.account);

        if (sheet.defaultLocalProfile.characterId > 0)
            CommitLocalSelection(sheet.defaultLocalProfile);
    }

    public static CharacterDefinition ResolveDefinition(PlayerMatchProfile profile)
    {
        if (characterCatalog == null)
            return null;

        CharacterDefinition byId = characterCatalog.GetById(profile.characterId);

        if (byId != null)
            return byId;

        return characterCatalog.GetByIndex(profile.catalogIndex);
    }

    public static GameObject ResolvePlayerPrefab(
        PlayerMatchProfile profile,
        GameObject fallbackPrefab
    )
    {
        CharacterDefinition definition = ResolveDefinition(profile);

        if (definition != null && definition.PlayerPrefab != null)
            return definition.PlayerPrefab;

        return fallbackPrefab;
    }

    public static void RegisterRemotePlayer(PlayerMatchProfile profile)
    {
        UpsertRosterSlot(profile);
        // TODO[NETWORK] Nhận từ ServerRpc / SyncList trên server.
    }

    public static void RegisterNetworkPlayerProfile(ulong playerId, PlayerMatchProfile profile)
    {
        networkProfiles[playerId] = profile;
        UpsertRosterSlot(profile);
    }

    public static bool TryGetNetworkPlayerProfile(ulong playerId, out PlayerMatchProfile profile)
    {
        return networkProfiles.TryGetValue(playerId, out profile);
    }

    public static void ClearRoster()
    {
        roster.Clear();
        networkProfiles.Clear();
        RosterChanged?.Invoke();
    }

    static void UpsertRosterSlot(PlayerMatchProfile profile)
    {
        for (int i = 0; i < roster.Count; i++)
        {
            if (roster[i].slotIndex == profile.slotIndex)
            {
                roster[i] = profile;
                RosterChanged?.Invoke();
                return;
            }
        }

        roster.Add(profile);
        RosterChanged?.Invoke();
    }

    static void PersistLocalToPlayerPrefs(PlayerMatchProfile profile)
    {
        PlayerPrefs.SetInt(CharacterIdKey, profile.characterId);
        PlayerPrefs.SetInt(CharacterIndexKey, profile.catalogIndex);
        PlayerPrefs.SetString(CharacterNameKey, profile.displayName);
        PlayerPrefs.SetInt(CharacterHpKey, profile.hp);
        PlayerPrefs.SetInt(CharacterBombKey, profile.bomb);
        PlayerPrefs.SetInt(CharacterSpeedKey, profile.speed);
        PlayerPrefs.Save();
    }

    static void SyncLegacyGameSession(PlayerMatchProfile profile)
    {
        GameSession.SetSelectedCharacter(
            profile.catalogIndex,
            profile.displayName,
            profile.hp,
            profile.bomb,
            profile.speed
        );
    }

    public static string GetOwnedKey(int characterId)
    {
        return "CharacterOwned_" + characterId;
    }
}
