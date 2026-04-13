using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase 2: Vertical Shrinking Enemy HUD (Behind Sprite).
/// Inherits from PlayerHUDBar to reuse scaling and fill logic.
/// Unified indicator where container height = current corruption,
/// and fill amount = purification progress (bottom-to-top).
/// Glows/flashes when in Sweet Spot range.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class EnemyWorldSpaceUI : PlayerHUDBar
{
    [Header("Enemy HUD References")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Enemy Visual Settings")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private Color sweetSpotGlowColor = Color.white;

    private EnemyStats _enemyStats;
    private bool _revealed;
    private Coroutine _fadeCoroutine;
    private bool _isInSweetSpot;

    protected override void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        // Ensure initial state is hidden
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        // D-BOSS-05: 억제 로직 - 보스전 시 일반 월드 스페이스 UI 비활성화
        if (GetComponentInParent<BossStats>() != null)
        {
            gameObject.SetActive(false);
            return;
        }

        _enemyStats = GetComponentInParent<EnemyStats>();
        
        // Reuse corruptionFillImage for purification fill visual
        if (corruptionFillImage != null)
            _originalCorruptionColor = corruptionFillImage.color;
    }

    protected override void Start()
    {
        if (_enemyStats == null) return;

        Bind(_enemyStats);
    }

    protected override void OnDestroy()
    {
        Unbind();
    }

    protected override void UpdatePulseEffect()
    {
        // Custom pulse effect for Sweet Spot instead of Player's warning pulse
        if (_isInSweetSpot && corruptionFillImage != null)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            corruptionFillImage.color = Color.Lerp(_originalCorruptionColor, sweetSpotGlowColor, pulse * 0.5f + 0.3f);
        }
        else if (corruptionFillImage != null)
        {
            corruptionFillImage.color = _originalCorruptionColor;
        }
    }

    public void Bind(EnemyStats stats)
    {
        if (_enemyStats != null && _enemyStats != stats) Unbind();

        _enemyStats = stats;
        _enemyStats.OnCorruptionChanged += HandleEnemyCorruptionChanged;
        _enemyStats.OnDamaged           += HandleFirstDamage;
        _enemyStats.OnDeath             += HandleDeath;

        // Initial setup
        HandleEnemyCorruptionChanged(_enemyStats.CurrentCorruption, _enemyStats.MaxCorruption);
        
        _revealed = false;
    }

    public void Unbind()
    {
        if (_enemyStats == null) return;
        _enemyStats.OnCorruptionChanged -= HandleEnemyCorruptionChanged;
        _enemyStats.OnDamaged           -= HandleFirstDamage;
        _enemyStats.OnDeath             -= HandleDeath;
        _enemyStats = null;
    }

    private void HandleEnemyCorruptionChanged(float current, float max)
    {
        if (max <= 0f) return;

        float corruptionRatio = Mathf.Clamp01(current / max);
        
        // D-02: Container height shrinks with Current Corruption
        // We use base.HandleMainChanged to set the scale based on corruptionRatio
        base.HandleMainChanged(current, max);

        // D-03: Purification Fill rises from bottom. 
        // Logic: FillAmount = (Max - Current) / Current (relative to current container)
        float purifiedAmount = max - current;
        
        // We set cached values so base.UpdateFill() can use them
        _cachedCurrentValue = current;
        _cachedSubValue = purifiedAmount;

        UpdateFill();
        CheckSweetSpot(corruptionRatio);
    }

    protected override void UpdateFill()
    {
        if (corruptionFillImage == null) return;

        // Step 3: Fill purification RELATIVE to the current container height (Current Corruption)
        float relativeRatio = (_cachedCurrentValue > 0.01f) ? Mathf.Clamp01(_cachedSubValue / _cachedCurrentValue) : 1f;
        corruptionFillImage.fillAmount = relativeRatio;
    }

    private void CheckSweetSpot(float corruptionRatio)
    {
        if (_enemyStats == null) return;

        // Check if current corruption ratio is within the sweet spot range
        float min = _enemyStats.basePurificationMin - _enemyStats.bonusPurificationMargin;
        float max = _enemyStats.basePurificationMax + _enemyStats.bonusPurificationMargin;

        bool wasInSweetSpot = _isInSweetSpot;
        _isInSweetSpot = (corruptionRatio >= min && corruptionRatio <= max);

        // D-07: Trigger Flash on entry
        if (_isInSweetSpot && !wasInSweetSpot)
        {
            StartCoroutine(TriggerFlash());
        }
    }

    private IEnumerator TriggerFlash()
    {
        if (corruptionFillImage == null) yield break;
        
        Color prevColor = corruptionFillImage.color;
        corruptionFillImage.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        corruptionFillImage.color = sweetSpotGlowColor;
        yield return new WaitForSeconds(0.05f);
        corruptionFillImage.color = prevColor;
    }

    private void HandleFirstDamage()
    {
        if (_revealed) return;
        _revealed = true;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeIn(fadeInDuration));
    }

    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private void HandleDeath()
    {
        Unbind();
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        gameObject.SetActive(false);
    }
}
