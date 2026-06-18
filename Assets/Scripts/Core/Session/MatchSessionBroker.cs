using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Middle layer duy nhất giữa các scene cho match/profile.
/// UI và spawner chỉ nói chuyện qua đây.
/// </summary>
public static class MatchSessionBroker
{
    static CharacterDatabase characterCatalog;
    static PlayerMatchProfile localPlayer;
    static readonly List<PlayerMatchProfile> roster = new();

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
        PlayerMatchProfile profile = localPlayer;

        if (profile.characterId > 0)
            profile.displayName = NakamaConnectionManager.EnsureExists().DisplayName;

        return profile;
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

    public static void LoadLocalFromProgression(CharacterDatabase database)
    {
        if (database == null)
            return;

        int characterId = PlayerProgressionService.Instance?.Current?.selectedCharacterId ?? 1;
        int catalogIndex = database.GetIndexById(characterId);

        if (catalogIndex < 0 && database.Count > 0)
        {
            catalogIndex = 0;
            characterId = database.GetByIndex(0).CharacterId;
        }

        CharacterDefinition definition = database.GetById(characterId);

        if (definition == null)
            return;

        string displayName = NakamaConnectionManager.EnsureExists().DisplayName;

        localPlayer = PlayerMatchProfile.FromDefinition(
            definition,
            catalogIndex,
            slotIndex: 0,
            isLocal: true,
            displayNameOverride: displayName
        );
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

    public static void ClearRoster()
    {
        roster.Clear();
        RosterChanged?.Invoke();
    }

    public static void Reset()
    {
        characterCatalog = null;
        localPlayer = default;
        roster.Clear();
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

    static void SyncLegacyGameSession(PlayerMatchProfile profile)
    {
        CharacterDefinition definition = ResolveDefinition(profile);
        string characterName = definition != null ? definition.CharacterName : "Player";

        GameSession.SetSelectedCharacter(
            profile.catalogIndex,
            characterName,
            profile.hp,
            profile.bomb,
            profile.speed
        );
    }

}
