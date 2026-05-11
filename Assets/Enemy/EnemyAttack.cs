using UnityEngine;

/// <summary>
/// 적의 공격 로직을 담당합니다.
///
/// 핵심 메커니즘:
///   - 공격은 자신의 체력(HP)을 소모하여 실행됩니다. 적도 물로 이루어진 존재이기 때문입니다.
///   - 공격 시 플레이어에게 데미지(attackHpCost)와 오염도(corruptionTransferRatio)를 전이합니다.
///   - 공격 비용으로 자신의 HP가 0 이하가 되면, 정화 판정 없이 즉시 파괴됩니다.
/// </summary>
[RequireComponent(typeof(EnemyStats))]
public class EnemyAttack : MonoBehaviour
{
    // ─── Inspector 설정 ───────────────────────────────────────────────────────

    [Header("공격 설정")]
    [Tooltip("공격 1회에 소모할 자신의 체력량. 적도 물로 이루어진 존재이기 때문입니다.")]
    public float attackHpCost = 10f;

    [Tooltip("플레이어에게 줄 실제 데미지 배율 (0.0 ~ 1.0). ")]
    [Range(0f, 1f)]
    public float playerDamageMultiplier = 0.5f;

    [Tooltip("플레이어에게 전이할 오염 비율 (0.0 ~ 1.0). " +
             "PlayerWaterStats.maxCorruptionThreshold 기준으로 계산됩니다.")]
    [Range(0f, 1f)]
    public float corruptionTransferRatio = 0.1f;

    [Header("패링")]
    [Tooltip("패링 성공 시 플레이어에게 회복할 HP량.")]
    public float parryHealAmount = 10f;

    [Header("자동 탐지")]
    [Tooltip("씬에서 플레이어를 자동으로 찾습니다. 직접 연결하면 자동 탐지를 건너뜁니다.")]
    [SerializeField] private PlayerWaterStats playerStats;

    // ─── 컴포넌트 참조 ───────────────────────────────────────────────────────

    private EnemyStats       stats;
    private EnemyAI          enemyAI;
    private PlayerController playerController;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        stats   = GetComponent<EnemyStats>();
        enemyAI = GetComponent<EnemyAI>();
    }

    private void Start()
    {
        // 싱글톤을 통한 효율적인 참조
        if (playerStats == null && PlayerController.Instance != null)
        {
            playerStats      = PlayerController.Instance.Stats;
            playerController = PlayerController.Instance;
        }

        if (playerStats == null)
            Debug.LogWarning($"[EnemyAttack] {gameObject.name}: 플레이어를 찾지 못했습니다.");
    }

    // ─── 핵심 공개 API ───────────────────────────────────────────────────────

    /// <summary>
    /// 공격을 실행합니다.
    /// 1. 자신의 HP를 attackHpCost만큼 소모합니다.
    /// 2. HP가 0 이하가 되면 정화 불가 파괴(Die)가 즉시 실행됩니다.
    /// 3. 플레이어에게 데미지(attackHpCost)와 오염도(corruptionTransferRatio)를 전달합니다.
    ///    (적이 공격하다 쓰러져도 최후의 일격은 플레이어에게 전달됩니다.)
    /// </summary>
    public void AttackPlayer()
    {
        if (stats == null)
        {
            Debug.LogError($"[EnemyAttack] {gameObject.name}: EnemyStats 컴포넌트가 없습니다.");
            return;
        }

        // [취약점 수정] 이미 사망하거나 정화된 적은 공격할 수 없음
        if (stats.IsDead) return;

        if (playerStats == null)
        {
            Debug.LogWarning($"[EnemyAttack] {gameObject.name}: 공격 대상 플레이어가 없습니다.");
            return;
        }

        // 1. 자신의 HP 소모 (HP가 0 이하가 되면 Die() 자동 호출 — 정화 불가)
        stats.SpendHpOnAttack(attackHpCost);

        // 2. 패링 판정 — 플레이어가 0단계 패링 중이면 역류 처리
        bool isParried = playerController != null
                      && playerController.CurrentState == PlayerState.Parrying
                      && playerController.Stats.WaterTier == 0;

        if (isParried)
        {
            playerStats.Heal(parryHealAmount);
            enemyAI?.ForceHitStun();
            Debug.Log($"[EnemyAttack] {gameObject.name} — 패링 성공! 플레이어 HP +{parryHealAmount}");
            return;
        }

        // 3. 일반 피격 — 플레이어에게 데미지 + 오염도 전달
        float finalDamage = attackHpCost * playerDamageMultiplier;
        playerStats.ReceiveAttack(finalDamage, corruptionTransferRatio);

        Debug.Log($"[EnemyAttack] {gameObject.name} 공격! " +
                  $"데미지: {finalDamage} (배율: {playerDamageMultiplier}), 오염 전이 비율: {corruptionTransferRatio * 100f:F0}%");
    }

    /// <summary>
    /// 지정한 PlayerWaterStats에 직접 공격합니다. (트리거, 충돌 이벤트에서 호출 시 사용)
    /// </summary>
    public void AttackPlayer(PlayerWaterStats target)
    {
        if (target == null) return;
        playerStats      = target;
        playerController = target.GetComponent<PlayerController>();
        AttackPlayer();
    }

    // ─── 충돌 기반 공격 트리거 (선택적 사용) ─────────────────────────────────

    /// <summary>
    /// 플레이어 레이어와 충돌 시 자동으로 AttackPlayer()를 호출합니다.
    /// 사용하려면 적 오브젝트의 Collider2D를 Is Trigger로 설정하세요.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 태그로 플레이어 판별 (Unity Editor에서 Player 오브젝트에 "Player" 태그를 설정해야 합니다)
        if (!other.CompareTag("Player")) return;

        PlayerWaterStats target = other.GetComponent<PlayerWaterStats>();
        if (target != null)
            AttackPlayer(target);
    }
}
