using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// 호수(Lake) 보스 전용 AI 컨트롤러.
/// BossStats의 페이즈 전환에 반응하며, 강인도(Poise) 시스템을 통해 경직을 제어합니다.
/// </summary>
[RequireComponent(typeof(BossStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossAI : MonoBehaviour
{
    public enum BossState { Intro, Phase1, Phase2, Phase3, Staggered, Dead }

    [Header("상태 정보")]
    public BossState CurrentState = BossState.Intro;

    [Header("강인도(Poise) 시스템")]
    [SerializeField] private float maxPoise        = 100f;
    [SerializeField] private float poiseRecovery   = 10f;    // 초당 회복량
    [SerializeField] private float staggerDuration = 2f;    // 그로기 시간
    private float currentPoise;
    private bool  isStaggered;
    private Coroutine staggerCoroutine;

    private BossStats   stats;
    private Rigidbody2D rb;
    private Animator    animator;
    private Transform   playerTransform;

    public Rigidbody2D RB => rb;
    public Transform PlayerTransform => playerTransform;
    public BossStats Stats => stats;

    private IBossPhase currentPhaseLogic;
    private BossPhase1 phase1Logic;
    private BossPhase2 phase2Logic;
    private BossPhase3 phase3Logic;

    private void Awake()
    {
        stats    = GetComponent<BossStats>();
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentPoise = maxPoise;

        // 페이즈 로직 객체 생성
        phase1Logic = new BossPhase1(this);
        phase2Logic = new BossPhase2(this);
        phase3Logic = new BossPhase3(this);
    }

    private void Start()
    {
        if (PlayerController.Instance != null)
            playerTransform = PlayerController.Instance.transform;

        stats.OnPhaseChanged += HandlePhaseChanged;
        stats.OnDamaged      += HandleDamaged;
        stats.OnDeath        += HandleDeath;

        // 게임 시작 시 Phase 1로 진입 (필요시 Intro 추가 가능)
        ChangeState(BossState.Phase1);
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnPhaseChanged -= HandlePhaseChanged;
            stats.OnDamaged      -= HandleDamaged;
            stats.OnDeath        -= HandleDeath;
        }
    }

    private void Update()
    {
        if (CurrentState == BossState.Dead) return;

        RecoverPoise();
        UpdateCurrentPhaseLogic();
    }

    // ─── AI 로직 ─────────────────────────────────────────────────────────────

    private void UpdateCurrentPhaseLogic()
    {
        if (CurrentState == BossState.Staggered) return;
        currentPhaseLogic?.OnPhaseUpdate();
    }

    // ─── 시스템 로직 ─────────────────────────────────────────────────────────

    private void RecoverPoise()
    {
        if (isStaggered || stats.IsInvincible) return;
        currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecovery * Time.deltaTime);
    }

    private void HandleDamaged()
    {
        if (isStaggered || CurrentState == BossState.Dead || stats.IsInvincible) return;

        // 보스는 일반 피격에 경직되지 않고 강인도만 깎임
        
        // [구현 예정] 플레이어의 태세(Stance)와 공격 단계에 따른 경직도 보정
        // 태세별 구현 시 이 부분에 가중치 로직 추가.
        float poiseDamage = 10f; 
        
        currentPoise -= poiseDamage;

        if (currentPoise <= 0f)
        {
            staggerCoroutine = StartCoroutine(StaggerCoroutine());
        }
    }

    private System.Collections.IEnumerator StaggerCoroutine()
    {
        isStaggered = true;
        BossState prevState = CurrentState;
        ChangeState(BossState.Staggered);

        animator?.SetTrigger("Stagger");
        Debug.Log($"[BossAI] {stats.BossName} 그로기 발생!");

        yield return new WaitForSeconds(staggerDuration);

        currentPoise = maxPoise;
        isStaggered = false;
        staggerCoroutine = null;
        ChangeState(prevState);
    }

    private void HandlePhaseChanged(int nextPhase)
    {
        // 페이즈 전환 시 그로기 상태면 즉시 취소
        if (isStaggered)
        {
            if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
            isStaggered = false;
            staggerCoroutine = null;
            currentPoise = maxPoise;
            Debug.Log($"[BossAI] 페이즈 전환으로 인해 {stats.BossName}의 그로기 상태가 해제되었습니다.");
        }

        // 페이즈 전환 연출 및 무적 처리
        StartCoroutine(PhaseTransitionCoroutine(nextPhase));
    }

    private System.Collections.IEnumerator PhaseTransitionCoroutine(int nextPhase)
    {
        stats.IsInvincible = true;
        Debug.Log($"[BossAI] {stats.BossName} 페이즈 {nextPhase} 전환 연출 시작 (무적)");

        // TODO: 페이즈 전환 전용 애니메이션 트리거 (예: roar, transform 등)
        animator?.SetTrigger("PhaseTransition");

        // 연출 대기 (임시 1.5초)
        yield return new WaitForSeconds(1.5f);

        stats.IsInvincible = false;
        Debug.Log($"[BossAI] {stats.BossName} 페이즈 {nextPhase} 전환 완료 (무적 해제)");

        switch (nextPhase)
        {
            case 2: ChangeState(BossState.Phase2); break;
            case 3: ChangeState(BossState.Phase3); break;
        }
    }

    private void HandleDeath()
    {
        if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
        ChangeState(BossState.Dead);
    }

    private void ChangeState(BossState next)
    {
        // 이전 페이즈 로직 종료 처리
        if (CurrentState != next)
        {
            currentPhaseLogic?.OnPhaseExit();
        }

        CurrentState = next;

        // 새로운 페이즈 로직 설정 및 시작
        switch (next)
        {
            case BossState.Phase1: currentPhaseLogic = phase1Logic; break;
            case BossState.Phase2: currentPhaseLogic = phase2Logic; break;
            case BossState.Phase3: currentPhaseLogic = phase3Logic; break;
            case BossState.Staggered: 
            case BossState.Dead:
                currentPhaseLogic = null;
                StopMovement();
                break;
        }

        currentPhaseLogic?.OnPhaseEnter();
        Debug.Log($"[BossAI] {stats.BossName} 상태 전환: {next}");
    }

    public void MoveTowardsPlayer(float speed)
    {
        if (playerTransform == null) return;

        float dir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
        
        // 방향 전환
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = dir < 0f;
    }

    public void StopMovement()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
}
