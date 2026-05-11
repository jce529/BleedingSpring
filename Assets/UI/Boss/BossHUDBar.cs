using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boss HUD Controller (Screen Space).
/// Fixed to the right side of the screen.
/// - Container scales vertically based on Boss's Current Corruption.
/// - Fill shows purification progress relative to current corruption.
/// - Pulsates when in Sweet Spot.
/// </summary>
public class BossHUDBar : PlayerHUDBar
{
    [Header("Boss UI References")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI phaseText;   // 신규: 페이즈 표시
    [SerializeField] private RectTransform sweetSpotGuide; // 신규: 동적 범위 가이드 UI
    [SerializeField] private CanvasGroup canvasGroup;

    private BossStats _bossStats;
    private bool _isInSweetSpot;

    protected override void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (corruptionFillImage != null)
            _originalCorruptionColor = corruptionFillImage.color;
        
        if (phaseText != null) phaseText.text = "PHASE 1";
    }

    public void Bind(BossStats stats)
    {
        if (_bossStats != null) Unbind();

        _bossStats = stats;
        _bossStats.OnCorruptionChanged += HandleBossCorruptionChanged;
        _bossStats.OnPhaseChanged      += HandlePhaseChanged; // 페이즈 구독

        if (bossNameText != null)
            bossNameText.text = _bossStats.BossName;

        // 초기 업데이트
        HandleBossCorruptionChanged(_bossStats.CurrentCorruption, _bossStats.MaxCorruption);
        HandlePhaseChanged(_bossStats.CurrentPhase);
    }

    public void Unbind()
    {
        if (_bossStats == null) return;
        _bossStats.OnCorruptionChanged -= HandleBossCorruptionChanged;
        _bossStats.OnPhaseChanged      -= HandlePhaseChanged;
        _bossStats = null;
    }

    private void HandlePhaseChanged(int phase)
    {
        if (phaseText != null)
            phaseText.text = $"PHASE {phase}";
        
        // 페이즈 전환 시 UI 연출 (예: 흔들림 효과 등 나중에 추가 가능)
        Debug.Log($"[BossUI] UI 페이즈 업데이트: {phase}");
    }

    protected override void UpdatePulseEffect()
    {
        // Sweet Spot 맥동 효과
        if (_isInSweetSpot && corruptionFillImage != null)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            corruptionFillImage.color = Color.Lerp(_originalCorruptionColor, warningColor, pulse * 0.5f + 0.3f);
        }
        else if (corruptionFillImage != null)
        {
            corruptionFillImage.color = _originalCorruptionColor;
        }

        // [추가] 동적 범위 가이드 UI 크기 조절
        UpdateSweetSpotGuide();
    }

    private void UpdateSweetSpotGuide()
    {
        if (sweetSpotGuide == null || _bossStats == null) return;

        // BossStats의 동적 범위 계산 로직을 UI에 투영
        float hpPercent = Mathf.Clamp01(_bossStats.CurrentHp / _bossStats.MaxHp);
        float shrinkFactor = 1f - hpPercent;
        float center = (_bossStats.basePurificationMin + _bossStats.basePurificationMax) * 0.5f;

        float currentMin = Mathf.Lerp(_bossStats.basePurificationMin - _bossStats.bonusPurificationMargin, center - 0.05f, shrinkFactor);
        float currentMax = Mathf.Lerp(_bossStats.basePurificationMax + _bossStats.bonusPurificationMargin, center + 0.05f, shrinkFactor);

        // UI 상에서 가이드 영역의 위치와 크기 설정 (Vertical Bar 기준이라 가정)
        // anchorMin.y와 anchorMax.y를 이용해 가이드 박스 표시
        sweetSpotGuide.anchorMin = new Vector2(0f, currentMin);
        sweetSpotGuide.anchorMax = new Vector2(1f, currentMax);
    }

    private void HandleBossCorruptionChanged(float current, float max)
    {
        if (max <= 0f) return;

        HandleMainChanged(current, max);

        // SubValue = 현재 채워진 정화량 (과거의 오염도에서 깎인 만큼)
        // 세계관상 몬스터는 MaxCorruption에서 시작해서 0으로 가는 구조
        float purifiedAmount = max - current;
        _cachedSubValue = purifiedAmount;

        UpdateFill();
        
        if (_bossStats != null)
            _isInSweetSpot = _bossStats.IsInPurificationRange;
    }

    protected override void UpdateFill()
    {
        if (corruptionFillImage == null) return;

        // Step 3: Fill purification RELATIVE to the current container height (Current Corruption)
        // Matches EnemyWorldSpaceUI logic for visual consistency
        float relativeRatio = (_cachedCurrentValue > 0.01f) ? Mathf.Clamp01(_cachedSubValue / _cachedCurrentValue) : 1f;
        corruptionFillImage.fillAmount = relativeRatio;
    }
}
