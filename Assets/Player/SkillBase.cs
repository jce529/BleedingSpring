using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 스킬 클래스의 추상 기반.
/// 쿨다운 관리와 Attacking 상태 진입/복귀를 공통으로 처리합니다.
/// </summary>
public abstract class SkillBase : MonoBehaviour, ISkill
{
    [Header("공통 스킬 설정")]
    [SerializeField, Range(0, 3)] protected int      currentStage     = 0;
    [SerializeField]              protected float     cooldownDuration = 0.5f;
    [SerializeField]              protected LayerMask enemyLayer;

    private float _cooldownRemaining;

    [Header("단계별 오염도 데미지 비율 (HP 데미지 대비, 0단계 = 0%)")]
    [SerializeField] protected float[] corruptionRatioPerStage = { 0f, 0.3f, 0.6f, 1.0f };

    [Header("공격 범위 표시")]
    [SerializeField] protected GameObject attackRangePrefab;
    [SerializeField] protected float      indicatorDuration = 0.2f;
    /// <summary>플레이어 중심에서 박스 시작점까지의 전방 간격 (캐릭터 몸 너비 절반)</summary>
    [SerializeField] protected float      frontOffset       = 0.4f;
    /// <summary>박스 중심의 Y 오프셋. 피벗이 발바닥일 때 캐릭터 허리 높이로 맞춤.</summary>
    [SerializeField] protected float      boxCenterY        = 0.9f;

    public bool IsOnCooldown { get; private set; }
    public int  Stage        => currentStage;
    public float CooldownRemaining => _cooldownRemaining;
    public float CooldownDuration  => cooldownDuration;

    protected IPlayerContext Context { get; private set; }
    protected Animator       Anim    { get; private set; }

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    public void Initialize(IPlayerContext context)
    {
        Context = context;
        Anim    = GetComponent<Animator>();
        OnInitialize();
    }

    /// <summary>하위 클래스에서 초기화 시 추가 작업이 필요하면 오버라이드합니다.</summary>
    protected virtual void OnInitialize() { }

    // ─── ISkill 구현 ─────────────────────────────────────────────────────────

    public void SetStage(int stage) => currentStage = Mathf.Clamp(stage, 0, 3);

    public void TryUse()
    {
        if (IsOnCooldown) return;
        if (!CanUse())    return;
        StartCoroutine(UseCoroutine());
    }

    // ─── 하위 클래스 훅 ──────────────────────────────────────────────────────

    protected virtual bool CanUse() => true;

    protected abstract IEnumerator ExecuteSkill();

    // ─── Animator 헬퍼 ───────────────────────────────────────────────────────

    /// <summary>Animator Controller에서 클립 이름으로 재생 시간(초)을 반환합니다.</summary>
    protected float GetClipLength(string clipName)
    {
        if (Anim == null || Anim.runtimeAnimatorController == null) return 0f;
        foreach (var clip in Anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 0f;
    }

    // ─── 범위 표시 헬퍼 ──────────────────────────────────────────────────────

    /// <summary>전방 사각형 범위를 프리팹으로 표시합니다.</summary>
    protected void ShowBoxIndicator(Vector2 center, Vector2 size)
    {
        if (attackRangePrefab == null) return;
        var obj = Instantiate(attackRangePrefab, center, Quaternion.identity);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        Destroy(obj, indicatorDuration);
    }

    /// <summary>현재 단계의 오염도 데미지를 반환합니다. (hpDamage × 단계별 비율)</summary>
    protected float GetCorruptionDamage(float hpDamage)
    {
        int idx = Mathf.Clamp(currentStage, 0, corruptionRatioPerStage.Length - 1);
        return hpDamage * corruptionRatioPerStage[idx];
    }

    // ─── 전방 박스 중심 계산 ─────────────────────────────────────────────────

    /// <summary>
    /// 플레이어 정면 중앙에 박스 중심을 반환합니다.
    /// frontOffset(몸 너비 절반) + boxWidth * 0.5 = 박스 근처 끝이 몸 앞에 딱 붙음.
    /// </summary>
    protected Vector2 GetFrontBoxCenter(float boxWidth, float offsetY = 0f)
    {
        float dir = Context.FacingRight ? 1f : -1f;
        return (Vector2)transform.position
               + Vector2.right * dir * (frontOffset + boxWidth * 0.5f)
               + Vector2.up    * (boxCenterY + offsetY);
    }

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private IEnumerator UseCoroutine()
    {
        IsOnCooldown = true;
        Context.ChangeState(PlayerState.Attacking);

        // 공격 시작 시 수평 이동 즉시 정지
        var rb = Context.Rigidbody;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        Debug.Log($"[{GetType().Name}] 발동 — {currentStage}단계 | " +
                  $"HP: {Context.Stats.CurrentCleanWater:F0} | " +
                  $"오염도: {Context.Stats.CurrentCorruption:F0}");

        yield return StartCoroutine(ExecuteSkill());

        _cooldownRemaining = cooldownDuration;
        while (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= Time.deltaTime;
            yield return null;
        }
        _cooldownRemaining = 0f;
        IsOnCooldown = false;

        if (Context.CurrentState == PlayerState.Attacking)
            Context.ChangeState(PlayerState.Idle);
    }
}
