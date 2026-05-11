using UnityEngine;

/// <summary>
/// 적 AI 상태머신.
/// Idle → Patrol → Chase → Attack 흐름으로 동작하며,
/// 피격 시 Hit 경직, 사망/정화 시 Dead로 전환됩니다.
///
/// [에디터 설정]
///   - Player Layer : 플레이어 레이어 선택
///   - 순찰 범위    : patrolDistance (시작 위치 기준 좌우 거리)
/// </summary>
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("탐지 범위")]
    [SerializeField] private float     detectionRadius = 5f;
    [SerializeField] private float     attackRadius    = 1.2f;

    [Header("이동")]
    [SerializeField] private float moveSpeed      = 3f;
    [SerializeField] private float patrolDistance = 4f;   // 시작 위치 기준 좌우 거리

    [Header("타이머")]
    [SerializeField] private float idleDuration    = 2f;
    [SerializeField] private float attackCooldown  = 1.5f;
    [SerializeField] private float windupDuration  = 0.6f;   // 공격 예비동작 — 패링 가능 창
    [SerializeField] private float hitStunDuration = 0.25f;

    // ─── 공개 상태 ───────────────────────────────────────────────────────────

    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    // ─── 컴포넌트 / 참조 ─────────────────────────────────────────────────────

    private EnemyStats   stats;
    private EnemyAttack  enemyAttack;
    private Rigidbody2D  rb;
    private SpriteRenderer sr;

    private Transform playerTransform;

    // ─── 내부 변수 ───────────────────────────────────────────────────────────

    private Vector2 patrolCenter;
    private float   patrolDir  = 1f;
    private float   stateTimer;
    private float   attackTimer;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        stats       = GetComponent<EnemyStats>();
        enemyAttack = GetComponent<EnemyAttack>();
        rb          = GetComponent<Rigidbody2D>();
        sr          = GetComponent<SpriteRenderer>();
        patrolCenter = transform.position;
        OnInitialize();
    }

    /// <summary>하위 클래스에서 추가 초기화가 필요하면 사용합니다.</summary>
    protected virtual void OnInitialize() { }

    private void Start()
    {
        if (PlayerController.Instance != null) 
            playerTransform = PlayerController.Instance.transform;

        stats.OnDamaged += HandleHit;
        stats.OnDeath   += HandleDeath;

        ChangeState(EnemyState.Idle);
    }

    private void OnDestroy()
    {
        if (stats == null) return;
        stats.OnDamaged -= HandleHit;
        stats.OnDeath   -= HandleDeath;
    }

    // ─── 메인 루프 ───────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentState == EnemyState.Dead) return;

        stateTimer  += Time.deltaTime;
        attackTimer += Time.deltaTime;

        switch (CurrentState)
        {
            case EnemyState.Idle:   UpdateIdle();   break;
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Chase:  UpdateChase();  break;
            case EnemyState.Attack: UpdateAttack(); break;
            case EnemyState.Windup: UpdateWindup(); break;
            case EnemyState.Hit:    UpdateHit();    break;
        }
    }

    // ─── 상태별 업데이트 ─────────────────────────────────────────────────────

    protected virtual void UpdateIdle()
    {
        if (PlayerInRange(detectionRadius)) { ChangeState(EnemyState.Chase);  return; }
        if (stateTimer >= idleDuration)     { ChangeState(EnemyState.Patrol); }
    }

    protected virtual void UpdatePatrol()
    {
        if (PlayerInRange(detectionRadius)) { ChangeState(EnemyState.Chase); return; }

        // 순찰 범위 끝에 도달하면 방향 전환 후 잠시 대기
        float traveled = (transform.position.x - patrolCenter.x) * patrolDir;
        if (traveled >= patrolDistance)
        {
            patrolDir = -patrolDir;
            ChangeState(EnemyState.Idle);
            return;
        }

        Move(patrolDir);
    }

    protected virtual void UpdateChase()
    {
        if (playerTransform == null) { ChangeState(EnemyState.Idle); return; }

        float dist = DistToPlayer();
        if (dist <= attackRadius)          { ChangeState(EnemyState.Attack); return; }
        if (dist > detectionRadius * 1.5f) { ChangeState(EnemyState.Patrol); return; }

        Move(playerTransform.position.x > transform.position.x ? 1f : -1f);
    }

    protected virtual void UpdateAttack()
    {
        StopHorizontal();

        if (playerTransform == null)              { ChangeState(EnemyState.Idle);  return; }
        if (DistToPlayer() > attackRadius * 1.4f) { ChangeState(EnemyState.Chase); return; }

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            ChangeState(EnemyState.Windup);   // 즉시 타격 대신 예비동작 진입
        }
    }

    protected virtual void UpdateWindup()
    {
        StopHorizontal();

        // 예비동작 중 플레이어가 멀어지면 취소
        if (playerTransform == null || DistToPlayer() > attackRadius * 2f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (stateTimer >= windupDuration)
        {
            enemyAttack?.AttackPlayer();
            ChangeState(EnemyState.Attack);
        }
    }

    [Header("전술적 AI")]
    [SerializeField] private float backstepForce    = 5f;
    [SerializeField] private float backstepDuration = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float backstepChance   = 0.6f;

    // ... (기타 필드 생략)

    private void UpdateHit()
    {
        if (stateTimer >= hitStunDuration)
        {
            // 경직 종료 시 플레이어가 너무 가까우면 일정 확률로 후퇴
            if (PlayerInRange(attackRadius * 1.5f) && Random.value < backstepChance)
            {
                StartCoroutine(BackstepCoroutine());
            }
            else
            {
                ChangeState(EnemyState.Chase);
            }
        }
    }

    private System.Collections.IEnumerator BackstepCoroutine()
    {
        ChangeState(EnemyState.Idle); // 잠시 이동 제어권 해제
        
        float dir = playerTransform.position.x > transform.position.x ? -1f : 1f;
        float elapsed = 0f;

        while (elapsed < backstepDuration)
        {
            rb.linearVelocity = new Vector2(dir * backstepForce, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopHorizontal();
        yield return new WaitForSeconds(0.1f);
        ChangeState(EnemyState.Chase);
    }

    // ─── 상태 전환 ───────────────────────────────────────────────────────────

    private void ChangeState(EnemyState next)
    {
        CurrentState = next;
        stateTimer   = 0f;

        if (next == EnemyState.Idle || next == EnemyState.Dead)
            StopHorizontal();

        Debug.Log($"[EnemyAI] {gameObject.name} → {next}");
    }

    // ─── 공개 API ────────────────────────────────────────────────────────────

    /// <summary>패링 성공 시 외부에서 강제 경직을 부여합니다.</summary>
    public void ForceHitStun()
    {
        if (CurrentState == EnemyState.Dead) return;
        ChangeState(EnemyState.Hit);
    }

    // ─── 이벤트 콜백 ─────────────────────────────────────────────────────────

    private void HandleHit()
    {
        if (CurrentState == EnemyState.Dead) return;
        ChangeState(EnemyState.Hit);
    }

    private void HandleDeath()
    {
        ChangeState(EnemyState.Dead);
    }

    // ─── 헬퍼 ────────────────────────────────────────────────────────────────

    private void Move(float dir)
    {
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        if (sr != null) sr.flipX = dir < 0f;
    }

    private void StopHorizontal()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private float DistToPlayer() =>
        playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;

    private bool PlayerInRange(float radius) =>
        playerTransform != null && DistToPlayer() <= radius;

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // 감지 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 공격 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // 순찰 범위 (파란색)
        Vector2 center = Application.isPlaying ? patrolCenter : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center + Vector2.left  * patrolDistance, 0.2f);
        Gizmos.DrawWireSphere(center + Vector2.right * patrolDistance, 0.2f);
        Gizmos.DrawLine(center + Vector2.left * patrolDistance,
                        center + Vector2.right * patrolDistance);
    }
}
