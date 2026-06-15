using PurrNet.StateMachine;
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

        if (MatchPhaseBroadcast.Instance == null)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                "Thiếu MatchPhaseBroadcast + PurrNet StateMachine trên 'MatchSystems'. Thêm StateMachine, MatchPhaseBroadcast, và 3 state con: Prep → Gameplay → ZoneShrink.",
                this
            );
        }

        if (FindAnyObjectByType<StateMachine>() == null)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                "Thiếu PurrNet StateMachine trong GameScene. Gắn trên MatchSystems cùng MatchPhaseBroadcast.",
                this
            );
        }

        if (FindAnyObjectByType<MatchStateMachineBootstrap>() == null)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                "Thiếu MatchStateMachineBootstrap trên MatchSystems (cần cho host/editor).",
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
