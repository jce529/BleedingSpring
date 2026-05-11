using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Controls a full-screen red vignette image.
/// Pulsates when Player water (HP) is below 25%.
/// </summary>
public class PlayerVignette : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image vignetteImage;

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] private float triggerThreshold = 0.25f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float maxAlpha = 0.5f;

    private PlayerWaterStats _stats;
    private float _currentWaterRatio = 1f;

    private void Awake()
    {
        if (vignetteImage == null)
            vignetteImage = GetComponent<Image>();

        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = 0f;
            vignetteImage.color = c;
            vignetteImage.raycastTarget = false;
        }
    }

    private void Start()
    {
        // More compatible find method
        _stats = UnityEngine.Object.FindAnyObjectByType<PlayerWaterStats>();
        
        if (_stats != null)
        {
            _stats.OnWaterChanged += HandleWaterChanged;
            HandleWaterChanged(_stats.CurrentCleanWater, _stats.MaxCleanWater);
        }
    }

    private void OnDestroy()
    {
        if (_stats != null)
            _stats.OnWaterChanged -= HandleWaterChanged;
    }

    private void HandleWaterChanged(float current, float max)
    {
        _currentWaterRatio = (max > 0f) ? (current / max) : 0f;
    }

    private void Update()
    {
        if (vignetteImage == null) return;

        if (_currentWaterRatio < triggerThreshold)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float intensity = 1f - (_currentWaterRatio / triggerThreshold);
            float targetAlpha = t * maxAlpha * intensity;

            Color c = vignetteImage.color;
            c.a = targetAlpha;
            vignetteImage.color = c;
        }
        else
        {
            if (vignetteImage.color.a > 0f)
            {
                Color c = vignetteImage.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * pulseSpeed);
                vignetteImage.color = c;
            }
        }
    }
}
