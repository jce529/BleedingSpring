using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World Space 세로 HUD 바: 물(위→아래) + 오염(아래→위) 이중 채움 + 워터 티어 구슬 3개.
/// 플레이어 자식 오브젝트에 부착합니다.
/// </summary>
public class PlayerHUDBar : MonoBehaviour
{
    [Header("Bar Fill Images")]
    [SerializeField] private Image waterFillImage;
    [SerializeField] private Image corruptionFillImage;

    [Header("Water Tier Orbs (3 Images)")]
    [SerializeField] private Image[] orbImages = new Image[3];

    [Header("Orb Colors")]
    [SerializeField] private Color litColor = new Color(0.4f, 0.85f, 1f, 1f);
    [SerializeField] private Color dimColor = new Color(0.2f, 0.2f, 0.25f, 0.5f);

    private PlayerWaterStats _stats;
    private float _cachedCurrentWater;
    private float _cachedMaxWater;
    private float _cachedCorruption;

    private void Awake()
    {
        _stats = GetComponentInParent<PlayerWaterStats>();
    }

    private void Start()
    {
        if (_stats == null)
        {
            Debug.LogWarning("[PlayerHUDBar] PlayerWaterStats not found in parent hierarchy.");
            return;
        }

        _stats.OnWaterChanged      += HandleWaterChanged;
        _stats.OnCorruptionChanged += HandleCorruptionChanged;
        _stats.OnWaterTierChanged  += HandleWaterTierChanged;

        // Initialize with current values
        HandleWaterChanged(_stats.CurrentCleanWater, _stats.MaxCleanWater);
        HandleCorruptionChanged(_stats.CurrentCorruption, _stats.maxCorruptionThreshold);
        HandleWaterTierChanged(_stats.WaterTier);
    }

    private void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnWaterChanged      -= HandleWaterChanged;
        _stats.OnCorruptionChanged -= HandleCorruptionChanged;
        _stats.OnWaterTierChanged  -= HandleWaterTierChanged;
    }

    private void HandleWaterChanged(float current, float max)
    {
        _cachedCurrentWater = current;
        _cachedMaxWater     = max;

        // Water fills from top: fillAmount = current / max
        if (waterFillImage != null)
            waterFillImage.fillAmount = (max > 0f) ? (current / max) : 0f;

        // Corruption ratio depends on current water, so update corruption fill too
        UpdateCorruptionFill();
    }

    private void HandleCorruptionChanged(float current, float max)
    {
        _cachedCorruption = current;
        UpdateCorruptionFill();
    }

    private void UpdateCorruptionFill()
    {
        if (corruptionFillImage == null) return;

        // Corruption fills from bottom: fillAmount = corruption / currentWater
        // Guard against divide-by-zero: if currentWater <= 0, show full corruption
        float ratio = (_cachedCurrentWater > 0f)
            ? Mathf.Clamp01(_cachedCorruption / _cachedCurrentWater)
            : 1f;
        corruptionFillImage.fillAmount = ratio;
    }

    private void HandleWaterTierChanged(int tier)
    {
        for (int i = 0; i < orbImages.Length; i++)
        {
            if (orbImages[i] != null)
                orbImages[i].color = (i < tier) ? litColor : dimColor;
        }
    }
}
