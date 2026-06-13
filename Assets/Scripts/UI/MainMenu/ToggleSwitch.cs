using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Toggle dạng slider: value 0 = Off, value 1 = On.
/// Dùng cho UI như Mute All.
/// </summary>
[RequireComponent(typeof(Slider))]
public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Slider Setup")]
    [SerializeField, Range(0, 1f)] private float sliderValue;
    [SerializeField] private bool startOn;

    public bool CurrentValue { get; private set; }

    private bool previousValue;
    private Slider slider;

    [Header("Animation")]
    [SerializeField, Range(0, 1f)] private float animationDuration = 0.25f;
    [SerializeField]
    private AnimationCurve slideEase =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine animateSliderCoroutine;

    [Header("Events")]
    [SerializeField] private UnityEvent onToggleOn;
    [SerializeField] private UnityEvent onToggleOff;
    [SerializeField] private UnityEvent<bool> onValueChanged;

    public UnityEvent<bool> OnValueChanged => onValueChanged;

    protected Action transitionEffect;

    private void OnValidate()
    {
        SetupSliderComponent();

        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(sliderValue);
        }
    }

    private void Awake()
    {
        SetupSliderComponent();

        CurrentValue = startOn;
        previousValue = CurrentValue;

        float value = CurrentValue ? 1f : 0f;
        sliderValue = value;

        if (slider != null)
            slider.SetValueWithoutNotify(value);
    }

    private void SetupSliderComponent()
    {
        if (slider != null)
            return;

        slider = GetComponent<Slider>();

        if (slider == null)
        {
            Debug.LogWarning("No slider found.", this);
            return;
        }

        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        ColorBlock sliderColors = slider.colors;
        sliderColors.disabledColor = Color.white;
        slider.colors = sliderColors;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    public void Toggle()
    {
        SetStateAndStartAnimation(!CurrentValue, true);
    }

    public void SetState(bool state)
    {
        SetStateAndStartAnimation(state, true);
    }

    public void SetStateWithoutNotify(bool state)
    {
        if (animateSliderCoroutine != null)
        {
            StopCoroutine(animateSliderCoroutine);
            animateSliderCoroutine = null;
        }

        CurrentValue = state;
        previousValue = state;

        float value = CurrentValue ? 1f : 0f;
        sliderValue = value;

        if (slider != null)
            slider.SetValueWithoutNotify(value);

        transitionEffect?.Invoke();
    }

    private void SetStateAndStartAnimation(bool state, bool notify)
    {
        previousValue = CurrentValue;
        CurrentValue = state;

        if (notify && previousValue != CurrentValue)
        {
            if (CurrentValue)
                onToggleOn?.Invoke();
            else
                onToggleOff?.Invoke();

            onValueChanged?.Invoke(CurrentValue);
        }

        if (animateSliderCoroutine != null)
            StopCoroutine(animateSliderCoroutine);

        animateSliderCoroutine = StartCoroutine(AnimateSlider());
    }

    private IEnumerator AnimateSlider()
    {
        if (slider == null)
            yield break;

        float startValue = slider.value;
        float endValue = CurrentValue ? 1f : 0f;

        float time = 0f;

        if (animationDuration > 0f)
        {
            while (time < animationDuration)
            {
                time += Time.deltaTime;

                float t = Mathf.Clamp01(time / animationDuration);
                float lerpFactor = slideEase.Evaluate(t);

                sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);
                slider.SetValueWithoutNotify(sliderValue);

                transitionEffect?.Invoke();

                yield return null;
            }
        }

        sliderValue = endValue;
        slider.SetValueWithoutNotify(endValue);

        transitionEffect?.Invoke();
    }
}
