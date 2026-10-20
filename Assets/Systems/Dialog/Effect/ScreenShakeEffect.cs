using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenShakeEffect : MonoBehaviour, IEffect
{
    [SerializeField] private float intensity = 8f;

    private RectTransform _rect;
    private readonly Dictionary<RectTransform, ShakeState> _shakes = new Dictionary<RectTransform, ShakeState>();
    private bool _paused;

    private sealed class ShakeState
    {
        public Vector2 Origin;
        public Coroutine Routine;
    }

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void Shake()
    {
        Shake(_rect);
    }

    public void Shake(RectTransform target)
    {
        if (_shakes.TryGetValue(target, out ShakeState previous))
        {
            StopCoroutine(previous.Routine);
            target.anchoredPosition = previous.Origin;
        }

        var state = new ShakeState { Origin = target.anchoredPosition };
        _shakes[target] = state;
        state.Routine = StartCoroutine(ShakeRoutine(target, state));
    }

    private IEnumerator ShakeRoutine(RectTransform target, ShakeState state)
    {
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            // A dynamically created portrait can be destroyed when the pair collapses.
            if (target == null) break;
            if (!_paused)
            {
                float progress = elapsed / duration;
                float dampedIntensity = intensity * (1f - progress);
                target.anchoredPosition = state.Origin + Random.insideUnitCircle * dampedIntensity;
                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        if (target != null) target.anchoredPosition = state.Origin;
        _shakes.Remove(target);
    }

    private void OnDisable()
    {
        ResetEffect();
    }

    public bool IsRunning()
    {
        return _shakes.Count > 0;
    }

    public void StartEffect(string param)
    {
        if (param == "抖动")
        {
            Shake();
        }
    }

    public void PauseEffect()
    {
        _paused = true;
    }

    public void ContinueEffect()
    {
        _paused = false;
    }

    public void ResetEffect()
    {
        StopAllCoroutines();
        foreach (var shake in _shakes)
        {
            if (shake.Key != null) shake.Key.anchoredPosition = shake.Value.Origin;
        }
        _shakes.Clear();
        _paused = false;
    }
}
