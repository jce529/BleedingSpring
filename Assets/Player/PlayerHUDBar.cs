using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World Space HUD bar attached to the player.
/// Implementation: "Shrinking Survival Container"
/// The entire bar container (containerRect) scales vertically based on Current Water (HP).
/// The corruption fills up from the bottom relative to the current container height.
/// </summary>
public class PlayerHUDBar : MonoBehaviour
{
    [Header("Container & Fill")]
    [Tooltip("The RectTransform that will be scaled vertically based on current water.")]
    [SerializeField] protected RectTransform containerRect;
    [SerializeField] protected Image corruptionFillImage;
    [SerializeField] protected Image waterBackgroundFillImage; // Optional: The 'water' part of the shrinking bar

    [Header("Colors & Animation")]
    [SerializeField] protected Color warningColor = Color.red;
    [SerializeField] protected float pulseSpeed = 10f;

    protected PlayerWaterStats _stats;
    protected float _cachedCurrentValue; // Renamed from _cachedCurrentWater for generality
    protected float _cachedMaxValue;     // Renamed from _cachedMaxWater
    protected float _cachedSubValue;     // Renamed from _cachedCorruption (e.g. corruption for player, purification for enemy)
    protected Color _originalCorruptionColor;

    protected virtual void Awake()
    {
        _stats = GetComponentInParent<PlayerWaterStats>();
        if (corruptionFillImage != null)
            _originalCorruptionColor = corruptionFillImage.color;
    }

    protected virtual void Start()
    {
        if (_stats == null)
        {
            // Only log if this is exactly PlayerHUDBar, subclasses might bind differently
            if (GetType() == typeof(PlayerHUDBar))
                Debug.LogWarning("[PlayerHUDBar] PlayerWaterStats not found in parent hierarchy.");
            return;
        }

        _stats.OnWaterChanged      += HandleMainChanged;
        _stats.OnCorruptionChanged += HandleSubChanged;

        // Initialize with current values
        HandleMainChanged(_stats.CurrentCleanWater, _stats.MaxCleanWater);
        HandleSubChanged(_stats.CurrentCorruption, _stats.maxCorruptionThreshold);
    }

    protected virtual void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnWaterChanged      -= HandleMainChanged;
        _stats.OnCorruptionChanged -= HandleSubChanged;
    }

    protected virtual void Update()
    {
        UpdatePulseEffect();
    }

    protected virtual void UpdatePulseEffect()
    {
        // Warning triggers when SubValue / MainValue >= 80%
        float ratio = (_cachedCurrentValue > 0f) ? Mathf.Clamp01(_cachedSubValue / _cachedCurrentValue) : 1f;

        if (ratio >= 0.8f && corruptionFillImage != null)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            corruptionFillImage.color = Color.Lerp(_originalCorruptionColor, warningColor, t);
        }
        else if (corruptionFillImage != null)
        {
            corruptionFillImage.color = _originalCorruptionColor;
        }
    }

    protected virtual void HandleMainChanged(float current, float max)
    {
        _cachedCurrentValue = current;
        _cachedMaxValue     = max;

        // Step 1: Shrink/Expand the entire container
        if (containerRect != null)
        {
            float heightRatio = (max > 0f) ? (current / max) : 0f;
            containerRect.localScale = new Vector3(1f, heightRatio, 1f);
        }

        // Step 2: Update the background fill if present
        if (waterBackgroundFillImage != null)
        {
            waterBackgroundFillImage.fillAmount = 1f;
        }
        
        UpdateFill();
    }

    protected virtual void HandleSubChanged(float current, float max)
    {
        _cachedSubValue = current;
        UpdateFill();
    }

    protected virtual void UpdateFill()
    {
        if (corruptionFillImage == null) return;

        // Step 3: Fill sub-value RELATIVE to the current container height
        float relativeRatio = (_cachedCurrentValue > 0f) ? Mathf.Clamp01(_cachedSubValue / _cachedCurrentValue) : 1f;
        corruptionFillImage.fillAmount = relativeRatio;
    }
}
