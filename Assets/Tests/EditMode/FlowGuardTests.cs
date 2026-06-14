#if UNITY_EDITOR
using NUnit.Framework;

public class FlowGuardTests
{
    [Test]
    public void IsValidSpawnProfile_AcceptsValidProfile()
    {
        var profile = new PlayerMatchProfile
        {
            characterId = 1,
            catalogIndex = 0,
            displayName = "MIMI"
        };

        Assert.IsTrue(FlowGuard.IsValidSpawnProfile(profile, out string reason));
        Assert.IsNull(reason);
    }

    [Test]
    public void IsValidSpawnProfile_RejectsMissingCharacterId()
    {
        var profile = new PlayerMatchProfile
        {
            characterId = 0,
            catalogIndex = 0,
            displayName = "MIMI"
        };

        Assert.IsFalse(FlowGuard.IsValidSpawnProfile(profile, out string reason));
        Assert.IsNotEmpty(reason);
    }

    [Test]
    public void IsValidSlotIndex_RejectsOutOfRange()
    {
        Assert.IsFalse(FlowGuard.IsValidSlotIndex(4, 4, out string reason));
        Assert.IsNotEmpty(reason);
    }

    [Test]
    public void IsValidSlotIndex_AcceptsInRange()
    {
        Assert.IsTrue(FlowGuard.IsValidSlotIndex(2, 4, out string reason));
        Assert.IsNull(reason);
    }
}
#endif
