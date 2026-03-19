using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 스킬 클래스의 추상 기반.
/// 쿨다운 관리와 Attacking 상태 진입/복귀를 공통으로 처리합니다. (SRP — 스킬 실행 흐름만 담당)
/// 구체적인 타격 로직은 각 하위 클래스에서 ExecuteSkill()을 오버라이드해 구현합니다. (OCP)
/// </summary>
public abstract class SkillBase : MonoBehaviour, ISkill
{
    [Header("공통 스킬 설정")]
    [SerializeField, Range(0, 3)] protected int   currentStage      = 0;
    [SerializeField]              protected float  cooldownDuration  = 1f;
    [SerializeField]              protected LayerMask enemyLayer;

    public bool IsOnCooldown { get; private set; }
    public int  Stage        => currentStage;

    protected IPlayerContext Context { get; private set; }

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    /// <summary>PlayerController.Start()에서 호출해야 합니다.</summary>
    public void Initialize(IPlayerContext context)
    {
        Context = context;
    }

    // ─── ISkill 구현 ─────────────────────────────────────────────────────────

    public void SetStage(int stage) => currentStage = Mathf.Clamp(stage, 0, 3);

    public void TryUse()
    {
        if (IsOnCooldown) return;
        if (!CanUse())    return;
        StartCoroutine(UseCoroutine());
    }

    // ─── 하위 클래스 훅 ──────────────────────────────────────────────────────

    /// <summary>
    /// 사용 전 선행 조건 검사 (예: 체력 희생 가능 여부).
    /// 기본값은 항상 true입니다.
    /// </summary>
    protected virtual bool CanUse() => true;

    /// <summary>
    /// 실제 스킬 로직을 구현합니다.
    /// yield return을 사용해 시간 기반 동작을 표현하거나, 동기 처리 후 yield break를 반환합니다.
    /// </summary>
    protected abstract IEnumerator ExecuteSkill();

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private IEnumerator UseCoroutine()
    {
        IsOnCooldown = true;
        Context.ChangeState(PlayerState.Attacking);

        yield return StartCoroutine(ExecuteSkill());

        yield return new WaitForSeconds(cooldownDuration);
        IsOnCooldown = false;

        // 공격 중이면 Idle로 복귀
        if (Context.CurrentState == PlayerState.Attacking)
            Context.ChangeState(PlayerState.Idle);
    }
}
