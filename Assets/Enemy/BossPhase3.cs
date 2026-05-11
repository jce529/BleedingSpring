using UnityEngine;

/// <summary>
/// 보스 페이즈 3: 강력한 궁극기 패턴.
/// </summary>
public class BossPhase3 : IBossPhase
{
    private BossAI boss;
    private float ultimateTimer;
    private float ultimateCooldown = 4.0f;
    private float moveSpeed = 5f;
    private float attackRange = 2.0f;

    public BossPhase3(BossAI boss)
    {
        this.boss = boss;
    }

    public void OnPhaseEnter()
    {
        Debug.Log("[BossPhase3] 페이즈 3 진입: 최종 폭주 상태");
        ultimateTimer = 0f;
    }

    public void OnPhaseUpdate()
    {
        if (boss.PlayerTransform == null) return;

        float distance = Vector2.Distance(boss.transform.position, boss.PlayerTransform.position);
        ultimateTimer += Time.deltaTime;

        // 페이즈 3은 매우 빠르게 추적
        boss.MoveTowardsPlayer(moveSpeed);

        if (distance <= attackRange && ultimateTimer >= ultimateCooldown)
        {
            ultimateTimer = 0f;
            ExecuteUltimatePattern();
        }
    }

    public void OnPhaseExit()
    {
        boss.StopMovement();
    }

    private void ExecuteUltimatePattern()
    {
        boss.GetComponent<Animator>()?.SetTrigger("Ultimate");
        // 궁극기는 더 강한 데미지를 줄 수 있도록 설정 가능 (현재는 기본 공격)
        var attack = boss.GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.AttackPlayer();
            attack.AttackPlayer(); // 임시로 두 번 호출하여 강력함 표현
        }
        Debug.Log("[BossPhase3] ✦ 궁극기 실행! ✦");
    }
}
