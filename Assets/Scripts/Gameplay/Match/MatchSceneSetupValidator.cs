using UnityEngine;

/// <summary>
/// Kiểm tra GameScene có đủ hệ thống match khi vào Play.
/// Gắn trên bất kỳ object nào trong GameScene (vd. GameManager).
/// </summary>
public class MatchSceneSetupValidator : MonoBehaviour
{
    void Start()
    {
        if (MatchGameplayAuthority.Instance == null)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                "Thiếu MatchGameplayAuthority. Tạo GameObject 'MatchSystems' + NetworkObject + script MatchGameplayAuthority trong GameScene.",
                this
            );
        }

        if (ExplosionCreator.Instance == null)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                "Thiếu ExplosionCreator trong GameScene.",
                this
            );
        }
    }
}
