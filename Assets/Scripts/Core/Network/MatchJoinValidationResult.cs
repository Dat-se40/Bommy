using System;

[Serializable]
public class MatchJoinValidationResult
{
    public bool success;
    public PlayerMatchProfile profile;
    public string error;

    public static MatchJoinValidationResult Offline(MatchJoinPayload payload)
    {
        PlayerMatchProfile profile = payload.ToProfile();

        return new MatchJoinValidationResult
        {
            success = FlowGuard.IsValidSpawnProfile(profile, out _),
            profile = profile,
            error = null
        };
    }
}
