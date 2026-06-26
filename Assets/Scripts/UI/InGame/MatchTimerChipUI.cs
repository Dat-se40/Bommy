using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// TimerChip — hiển thị thời gian phase, đổi màu dần sang đỏ, rung nhẹ ở giây cuối.
/// </summary>
public class MatchTimerChipUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private RectTransform shakeTarget;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new(0.97f, 0.89f, 0.71f, 1f);
    [SerializeField] private Color urgentColor = new(0.95f, 0.2f, 0.15f, 1f);

    [Header("Urgency")]
    [SerializeField] private float shakeStartRemainingSeconds = 10f;
    [SerializeField] private float shakeStrength = 6f;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private int shakeVibrato = 8;

    Tween shakeTween;
    int lastShakeSecond = -1;

    void Awake()
    {
        TryAutoBind();

        if (shakeTarget == null && timerLabel != null)
            shakeTarget = timerLabel.rectTransform;
    }

    void OnEnable()
    {
        if (MatchPhaseBroadcast.Instance != null)
            MatchPhaseBroadcast.Instance.PhaseChanged += Refresh;

        Refresh();
    }

    void OnDisable()
    {
        if (MatchPhaseBroadcast.Instance != null)
            MatchPhaseBroadcast.Instance.PhaseChanged -= Refresh;

        shakeTween?.Kill();
        shakeTween = null;
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;

        if (broadcast == null || timerLabel == null)
            return;

        float remaining = broadcast.PhaseRemainingSeconds;
        float duration = broadcast.PhaseDurationSeconds;
        float urgency = duration > 0.01f ? 1f - Mathf.Clamp01(remaining / duration) : 1f;

        timerLabel.color = Color.Lerp(normalColor, urgentColor, urgency);
        timerLabel.text = FormatTime(remaining);

        int secondBucket = Mathf.CeilToInt(remaining);

        if (remaining <= shakeStartRemainingSeconds && remaining > 0f && secondBucket != lastShakeSecond)
        {
            lastShakeSecond = secondBucket;
            PlayLightShake();
            SoundManager.Instance?.PlaySfx(SoundKey.SfxCountdown);
        }
    }

    void PlayLightShake()
    {
        if (shakeTarget == null)
            return;

        shakeTween?.Kill();
        shakeTween = shakeTarget
            .DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
            .SetUpdate(true);
    }

    static string FormatTime(float totalSeconds)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, totalSeconds));
        int minutes = seconds / 60;
        int secs = seconds % 60;
        return $"{minutes:00}:{secs:00}";
    }

    void TryAutoBind()
    {
        if (timerLabel == null)
        {
            timerLabel = UIAutoBindUtility.FindChildComponent<TMP_Text>(
                this,
                "Timerlbl",
                "TimerLbl",
                "TimerLabel",
                "Time"
            );
        }

        if (shakeTarget == null)
            shakeTarget = transform as RectTransform;
    }
}
