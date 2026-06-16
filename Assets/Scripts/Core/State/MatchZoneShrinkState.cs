using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Phase bo thu hẹp — server tick từng ô, replicate qua ExplosionCreator.shrinkCells.
/// </summary>
public class MatchZoneShrinkState : MatchTimedStateNode
{
    [SerializeField] private float zoneShrinkDurationSeconds = MatchTiming.ZoneShrinkSeconds;
    [SerializeField] private float shrinkCellIntervalSeconds = 0.5f;

    Coroutine shrinkRoutine;

    protected override MatchPhaseKind PhaseKind => MatchPhaseKind.ZoneShrink;
    protected override float DurationSeconds => zoneShrinkDurationSeconds;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        if (ExplosionCreator.Instance == null)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                "Thiếu ExplosionCreator trong GameScene.",
                this
            );
            return;
        }

        if (MapRefs.Instance == null || MapRefs.Instance.Shrink == null)
        {
            FlowGuard.Error(FlowGuard.TagSetup, "MapRefs hoặc Shrink tilemap chưa sẵn sàng.", this);
            return;
        }

        FlowGuard.Info(FlowGuard.TagGameplay, "Zone shrink started.", this);
        shrinkRoutine = StartCoroutine(ShrinkBorderRoutine());
    }

    public override void Exit(bool asServer)
    {
        if (asServer && shrinkRoutine != null)
        {
            StopCoroutine(shrinkRoutine);
            shrinkRoutine = null;
        }

        base.Exit(asServer);
    }

    IEnumerator ShrinkBorderRoutine()
    {
        ExplosionCreator authority = ExplosionCreator.Instance;
        Tilemap shrinkMap = MapRefs.Instance.Shrink;

        BoundsInt bounds = shrinkMap.cellBounds;
        int minX = bounds.xMin;
        int maxX = bounds.xMax - 1;
        int minY = bounds.yMin;
        int maxY = bounds.yMax - 1;

        while (minX <= maxX && minY <= maxY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                yield return WaitShrinkInterval();
                authority.ServerAddShrinkCell(new Vector3Int(x, maxY));
            }

            for (int y = maxY - 1; y >= minY; y--)
            {
                yield return WaitShrinkInterval();
                authority.ServerAddShrinkCell(new Vector3Int(maxX, y));
            }

            for (int x = maxX - 1; x >= minX; x--)
            {
                yield return WaitShrinkInterval();
                authority.ServerAddShrinkCell(new Vector3Int(x, minY));
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                yield return WaitShrinkInterval();
                authority.ServerAddShrinkCell(new Vector3Int(minX, y));
            }

            minX++;
            maxX--;
            minY++;
            maxY--;
        }

        shrinkRoutine = null;
    }

    IEnumerator WaitShrinkInterval()
    {
        if (shrinkCellIntervalSeconds <= 0f)
            yield return null;
        else
            yield return new WaitForSeconds(shrinkCellIntervalSeconds);
    }

    protected override void OnDurationElapsed()
    {
        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;
        if (authority == null)
        {
            FlowGuard.Error(FlowGuard.TagGameplay, "MatchGameplayAuthority missing — cannot end match.", this);
            return;
        }

        authority.GameOver();
    }
}
