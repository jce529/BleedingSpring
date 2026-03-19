using System.Collections;
using UnityEngine;

/// <summary>
/// [J] 기본 공격 스킬. 단계별 물 속성 근거리 공격.
///
/// 0단계: 순수 물리 찌르기/베기
/// 1단계: 검 끝 물보라, 리치 +0.3
/// 2단계: 물보라 강화, 리치 +0.6
/// 3단계: 거대한 물의 잔상 — 0.1초 후 50% 추가 타격
/// </summary>
public class BasicAttackSkill : SkillBase
{
    [Header("[J] 기본 공격")]
    [SerializeField] private float     baseDamage   = 15f;
    [SerializeField] private float     baseRange    = 1.5f;
    [SerializeField] private Transform attackPoint;

    // 단계별 추가 리치 (3단계는 리치 대신 다중 타격이므로 0)
    private static readonly float[] RangeBonus = { 0f, 0.3f, 0.6f, 0f };

    protected override IEnumerator ExecuteSkill()
    {
        float   range  = baseRange + RangeBonus[currentStage];
        Vector2 center = attackPoint != null
            ? (Vector2)attackPoint.position
            : (Vector2)transform.position;

        // 1차 타격
        DamageAll(center, range, baseDamage);

        // 3단계: 물의 잔상 연속 타격 (50%)
        if (currentStage >= 3)
        {
            yield return new WaitForSeconds(0.1f);
            DamageAll(center, range, baseDamage * 0.5f);
        }

        yield break;
    }

    private void DamageAll(Vector2 center, float radius, float damage)
    {
        foreach (var hit in Physics2D.OverlapCircleAll(center, radius, enemyLayer))
            hit.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 pos = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(pos, baseRange);
    }
}
