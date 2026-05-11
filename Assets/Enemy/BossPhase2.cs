using UnityEngine;

/// <summary>
/// 보스 페이즈 2: 광역 공격(AoE) 및 돌진 패턴 추가.
/// </summary>
public class BossPhase2 : IBossPhase
{
    private BossAI boss;
    private float patternTimer;
    private float patternCooldown = 3.0f;
    private float moveSpeed = 3.5f;
    private float attackRange = 1.8f;

    public BossPhase2(BossAI boss)
    {
        this.boss = boss;
    }

    public void OnPhaseEnter()
    {
        Debug.Log("[BossPhase2] 페이즈 2 진입: 속도 증가 및 특수 패턴");
        patternTimer = 0f;
    }

    public void OnPhaseUpdate()
    {
        if (boss.PlayerTransform == null) return;

        float distance = Vector2.Distance(boss.transform.position, boss.PlayerTransform.position);
        patternTimer += Time.deltaTime;

        if (distance > attackRange)
        {
            boss.MoveTowardsPlayer(moveSpeed);
        }
        else
        {
            boss.StopMovement();
            if (patternTimer >= patternCooldown)
            {
                patternTimer = 0f;
                ExecuteRandomPattern();
            }
        }
    }

    public void OnPhaseExit()
    {
        boss.StopMovement();
    }

    private void ExecuteRandomPattern()
    {
        int rand = Random.Range(0, 2);
        if (rand == 0)
        {
            // 돌진 공격 시 일시적으로 속도 증가 가능
            boss.GetComponent<Animator>()?.SetTrigger("ChargeAttack");
            boss.GetComponent<EnemyAttack>()?.AttackPlayer();
            Debug.Log("[BossPhase2] 돌진 공격!");
        }
        else
        {
            boss.GetComponent<Animator>()?.SetTrigger("AoEAttack");
            // AoE는 별도 로직이 필요할 수 있으나 일단 기본 공격 호출
            boss.GetComponent<EnemyAttack>()?.AttackPlayer();
            Debug.Log("[BossPhase2] 광역 폭발 공격!");
        }
    }
}
