using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [U] 참격 스킬. 넓은 범위 공격.
///
/// 0단계 [단순 횡베기]:  부채꼴 물리 공격 + 밀쳐냄
/// 1단계 [수면 가르기]:  부채꼴 물결, 리치 ×1.3, 데미지 ×1.2
/// 2단계 [폭포 베기]:    점프 후 내리찍기 → 지면 충격파 + 물기둥
/// 3단계 [해일참]:       체력 희생(tsunamiHpCost) → 화면 절반 범위, 오염 장판 제거
/// </summary>
public class WideSlashSkill : SkillBase
{
    [Header("[U] 참격 스킬")]
    [SerializeField] private float     baseDamage      = 30f;
    [SerializeField] private float     slashRange      = 3f;
    [SerializeField] private float     slashAngle      = 120f;   // 부채꼴 각도
    [SerializeField] private float     tsunamiHpCost   = 40f;    // 3단계 체력 소모
    [SerializeField] private float     tsunamiRange    = 15f;    // 3단계 가로 범위
    [SerializeField] private float     tsunamiHeight   = 6f;     // 3단계 세로 범위
    [SerializeField] private GameObject waterPillarEffect;       // 2단계 물기둥 이펙트
    [SerializeField] private GameObject tsunamiEffect;           // 3단계 해일 이펙트

    // ─── CanUse 오버라이드 ───────────────────────────────────────────────────

    protected override bool CanUse()
    {
        // 3단계: 소모 후에도 생존 가능한지 미리 확인
        if (currentStage >= 3 && Context.Stats.CurrentHp <= tsunamiHpCost)
        {
            Debug.Log("[U] 체력 부족 — 해일참 사용 불가");
            return false;
        }
        return true;
    }

    // ─── ExecuteSkill 구현 ───────────────────────────────────────────────────

    protected override IEnumerator ExecuteSkill()
    {
        switch (currentStage)
        {
            case 0: ExecuteSimpleSlash();                                  break;
            case 1: ExecuteWaveSlash();                                    break;
            case 2: yield return StartCoroutine(ExecuteWaterfallSlash()); break;
            case 3: ExecuteTsunamiSlash();                                 break;
        }
        yield break;
    }

    // 0단계: 단순 횡베기 + 적 밀쳐냄
    private void ExecuteSimpleSlash()
    {
        foreach (var hit in GetFanHits(slashRange, slashAngle))
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(baseDamage);

            var enemyRb = hit.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 dir = (hit.transform.position - transform.position).normalized;
                enemyRb.AddForce(dir * 8f, ForceMode2D.Impulse);
            }
        }
    }

    // 1단계: 부채꼴 물결
    private void ExecuteWaveSlash()
    {
        foreach (var hit in GetFanHits(slashRange * 1.3f, slashAngle))
            hit.GetComponent<IDamageable>()?.TakeDamage(baseDamage * 1.2f);

        // TODO: 물결 파티클 이펙트 재생
    }

    // 2단계: 상승 후 내리찍기 → 지면 충격파 + 물기둥
    private IEnumerator ExecuteWaterfallSlash()
    {
        var rb = Context.Rigidbody;

        // 상승
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 9f);
        yield return new WaitForSeconds(0.25f);

        // 내리찍기
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -20f);
        yield return new WaitForSeconds(0.2f);

        // 착지 충격 — 전방위
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, slashRange, enemyLayer))
            hit.GetComponent<IDamageable>()?.TakeDamage(baseDamage * 1.5f);

        if (waterPillarEffect != null)
            Instantiate(waterPillarEffect, transform.position, Quaternion.identity);
    }

    // 3단계: 해일참 — 체력 희생, 대범위, 오염 장판 제거
    private void ExecuteTsunamiSlash()
    {
        // CanUse()에서 이미 가능 여부를 확인했으므로 안전하게 소모
        Context.Stats.TrySacrificeHp(tsunamiHpCost);

        float   dir    = Context.FacingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position + new Vector2(dir * tsunamiRange * 0.5f, 0f);
        Vector2 size   = new Vector2(tsunamiRange, tsunamiHeight);

        // 적 타격
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer))
            hit.GetComponent<IDamageable>()?.TakeDamage(baseDamage * 3f);

        // 오염 장판 제거
        foreach (var c in Physics2D.OverlapBoxAll(center, size, 0f, LayerMask.GetMask("Contamination")))
            Destroy(c.gameObject);

        if (tsunamiEffect != null)
            Instantiate(tsunamiEffect, transform.position, Quaternion.identity);
    }

    // ─── 유틸리티 ────────────────────────────────────────────────────────────

    /// <summary>전방 부채꼴 범위 안의 Collider2D 목록을 반환합니다.</summary>
    private List<Collider2D> GetFanHits(float range, float angle)
    {
        Collider2D[] allHits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);

        if (angle >= 360f)
            return new List<Collider2D>(allHits);

        float facingAngle = Context.FacingRight ? 0f : 180f;
        var   result      = new List<Collider2D>();

        foreach (var hit in allHits)
        {
            Vector2 dir      = (hit.transform.position - transform.position).normalized;
            float   hitAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (Mathf.Abs(Mathf.DeltaAngle(hitAngle, facingAngle)) <= angle * 0.5f)
                result.Add(hit);
        }

        return result;
    }
}
