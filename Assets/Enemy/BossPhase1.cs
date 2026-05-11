using UnityEngine;

/// <summary>
/// 보스 페이즈 1: 기본적인 근접 공격과 이동.
/// </summary>
public class BossPhase1 : IBossPhase
{
    private BossAI boss;
    private float attackTimer;
    private float attackCooldown = 2.5f;
    private float moveSpeed = 2f;
    private float attackRange = 1.5f;

    public BossPhase1(BossAI boss)
    {
        this.boss = boss;
    }

    public void OnPhaseEnter()
    {
        Debug.Log("[BossPhase1] 페이즈 1 진입: 추적 및 근접 전투");
        attackTimer = 0f;
    }

    public void OnPhaseUpdate()
    {
        if (boss.PlayerTransform == null) return;

        float distance = Vector2.Distance(boss.transform.position, boss.PlayerTransform.position);
        attackTimer += Time.deltaTime;

        if (distance > attackRange)
        {
            boss.MoveTowardsPlayer(moveSpeed);
        }
        else
        {
            boss.StopMovement();
            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                PerformBasicAttack();
            }
        }
    }

    public void OnPhaseExit()
    {
        boss.StopMovement();
    }

    private void PerformBasicAttack()
    {
        boss.GetComponent<Animator>()?.SetTrigger("Attack1");
        // EnemyAttack 컴포넌트가 있다면 호출
        boss.GetComponent<EnemyAttack>()?.AttackPlayer();
        Debug.Log("[BossPhase1] 기본 휘두르기 공격!");
    }
}
