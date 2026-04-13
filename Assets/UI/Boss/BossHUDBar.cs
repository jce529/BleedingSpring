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
    [SerializeField] private CanvasGroup canvasGroup;

    private BossStats _bossStats;
    private bool _isInSweetSpot;

    protected override void Awake()
    {
        // BossHUDBar is usually not a child of Boss, so it will be bound manually or by finding the boss.
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (corruptionFillImage != null)
            _originalCorruptionColor = corruptionFillImage.color;
    }

    protected override void Start()
    {
        // Do nothing in Start, wait for Bind()
    }

    public void Bind(BossStats stats)
    {
        if (_bossStats != null) Unbind();

        _bossStats = stats;
        _bossStats.OnCorruptionChanged += HandleBossCorruptionChanged;
        _bossStats.OnDeath             += HandleBossDeath;

        if (bossNameText != null)
            bossNameText.text = _bossStats.BossName;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // Initial update
        HandleBossCorruptionChanged(_bossStats.CurrentCorruption, _bossStats.MaxCorruption);
    }

    public void Unbind()
    {
        if (_bossStats == null) return;
        _bossStats.OnCorruptionChanged -= HandleBossCorruptionChanged;
        _bossStats.OnDeath             -= HandleBossDeath;
        _bossStats = null;
    }

    protected override void OnDestroy()
    {
        Unbind();
    }

    protected override void UpdatePulseEffect()
    {
        // D-BOSS-08: Sweet Spot Pulse
        if (_isInSweetSpot && corruptionFillImage != null)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            // Use warningColor as the glow target color (or we could add a specific bossGlowColor)
            corruptionFillImage.color = Color.Lerp(_originalCorruptionColor, warningColor, pulse * 0.5f + 0.3f);
        }
        else if (corruptionFillImage != null)
        {
            corruptionFillImage.color = _originalCorruptionColor;
        }
    }

    private void HandleBossCorruptionChanged(float current, float max)
    {
        if (max <= 0f) return;

        // Container scale Y = Current / Max
        HandleMainChanged(current, max);

        // SubValue = Purified Amount (Max - Current)
        float purifiedAmount = max - current;
        _cachedSubValue = purifiedAmount;

        UpdateFill();
        
        // Update Sweet Spot flag for pulse effect
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

    private void HandleBossDeath()
    {
        // Fade out on death
        if (canvasGroup != null)
            StartCoroutine(FadeOut(0.5f));
        Unbind();
    }

    private IEnumerator FadeOut(float duration)
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
