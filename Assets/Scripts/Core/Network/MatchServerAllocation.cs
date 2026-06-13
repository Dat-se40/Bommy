using System;

[Serializable]
public class MatchServerAllocation
{
    public string matchId;
    public string host;
    public int port = 5000;
    public string matchToken;
    public string edgegapDeploymentId;
}
