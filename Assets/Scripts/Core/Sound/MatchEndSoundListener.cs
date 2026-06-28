using System.Collections;
using UnityEngine;

public class MatchEndSoundListener : MonoBehaviour
{
    void Start() => StartCoroutine(Bind());

    IEnumerator Bind()
    {
        while (MatchGameplayAuthority.Instance == null)
            yield return null;

        MatchGameplayAuthority.Instance.MatchFinishedChanged += OnMatchFinished;
    }

    void OnDestroy()
    {
        if (MatchGameplayAuthority.Instance != null)
            MatchGameplayAuthority.Instance.MatchFinishedChanged -= OnMatchFinished;
    }

    void OnMatchFinished(bool finished)
    {
        if (!finished) return;
        SoundManager.Instance.StopAllSfx();
        SoundPlayback.StopBgm();
        
    }
}