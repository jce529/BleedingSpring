using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오염도/HP 비율 기반 비네트 효과.
/// Screen Space Overlay Canvas에 전체화면 방사형 그라디언트 이미지를 배치하고,
/// 비율 단계에 따라 색상과 펄스 속도를 변경합니다.
/// </summary>
public class PlayerVignette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image vignetteImage;
    [SerializeField] private PlayerWaterStats playerStats;

    [Header("Stage Colors")]
    [SerializeField] private Color stage1Color = new Color(0.90f, 0.78f, 1.00f, 1f); // lavender #E6C8FF
    [SerializeField] private Color stage2Color = new Color(0.24f, 0.21f, 0.50f, 1f); // indigo #3D3580
    [SerializeField] private Color stage3Color = new Color(0.42f, 0.18f, 0.55f, 1f); // deep purple #6B2D8B

    [Header("Pulse Settings")]
    [SerializeField] private float stage1PulseSpeed = 0.5f;
    [SerializeField] private float stage2PulseSpeed = 1.0f;
    [SerializeField] private float stage3PulseSpeed = 2.5f;
    [SerializeField] private float baseAlpha = 0.7f;

    private int   _currentStage;
    private float _cachedCurrentWater;
    private float _cachedCorruption;

    private static readonly Color ClearColor = new Color(0f, 0f, 0f, 0f);

    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerWaterStats>();
            if (playerStats == null)
            {
                Debug.LogWarning("[PlayerVignette] PlayerWaterStats not found.");
                return;
            }
        }

        playerStats.OnWaterChanged      += HandleWaterChanged;
        playerStats.OnCorruptionChanged += HandleCorruptionChanged;

        // Initialize with current values
        HandleWaterChanged(playerStats.CurrentCleanWater, playerStats.MaxCleanWater);
        HandleCorruptionChanged(playerStats.CurrentCorruption, playerStats.maxCorruptionThreshold);

        // Ensure vignette does not block input
        if (vignetteImage != null)
            vignetteImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (playerStats == null) return;
        playerStats.OnWaterChanged      -= HandleWaterChanged;
        playerStats.OnCorruptionChanged -= HandleCorruptionChanged;
    }

    private void HandleWaterChanged(float current, float max)
    {
        _cachedCurrentWater = current;
        RecalculateStage();
    }

    private void HandleCorruptionChanged(float current, float max)
    {
        _cachedCorruption = current;
        RecalculateStage();
    }

    private void RecalculateStage()
    {
        // Guard against divide-by-zero: if HP is 0, treat as maximum danger
        float ratio = (_cachedCurrentWater > 0f)
            ? Mathf.Clamp01(_cachedCorruption / _cachedCurrentWater)
            : 1f;

        _currentStage = ratio >= 0.75f ? 3
                       : ratio >= 0.50f ? 2
                       : ratio >= 0.25f ? 1
                       : 0;
    }

    private void Update()
    {
        if (vignetteImage == null) return;

        if (_currentStage == 0)
        {
            vignetteImage.color = ClearColor;
            return;
        }

        Color stageColor;
        float pulseSpeed;

        switch (_currentStage)
        {
            case 1:
                stageColor = stage1Color;
                pulseSpeed = stage1PulseSpeed;
                break;
            case 2:
                stageColor = stage2Color;
                pulseSpeed = stage2PulseSpeed;
                break;
            case 3:
                stageColor = stage3Color;
                pulseSpeed = stage3PulseSpeed;
                break;
            default:
                vignetteImage.color = ClearColor;
                return;
        }

        float t     = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        float alpha = baseAlpha * t;

        vignetteImage.color = new Color(stageColor.r, stageColor.g, stageColor.b, alpha);
    }
}
