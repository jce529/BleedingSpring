using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World Space 세로 HUD 바: 물(아래→위) + 오염(아래→위, 물의 하단부에 표시) 이중 채움 + 워터 티어 구슬 3개.
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

        // 물 바: 전체 최대치 대비 현재 물의 양을 아래서부터 채웁니다.
        if (waterFillImage != null)
        {
            waterFillImage.fillMethod = Image.FillMethod.Vertical;
            waterFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            waterFillImage.fillAmount = (max > 0f) ? (current / max) : 0f;
        }

        // 물의 양이 변하면 오염도 비율(전체 대비)도 다시 계산되어야 하므로 호출
        UpdateCorruptionFill();
    }

    private void HandleCorruptionChanged(float current, float max)
    {
        _cachedCorruption = current;
        UpdateCorruptionFill();
    }

    private void UpdateCorruptionFill()
    {
        if (corruptionFillImage == null || _cachedMaxWater <= 0f) return;

        // 오염도: 아래서부터 전체 바의 최대치 대비 비율로 채웁니다.
        // 이렇게 하면 물 바의 아래쪽 일부가 오염된 것처럼 시각적으로 나타납니다.
        corruptionFillImage.fillMethod = Image.FillMethod.Vertical;
        corruptionFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;

        float ratio = Mathf.Clamp01(_cachedCorruption / _cachedMaxWater);
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
