using System.Collections;
using UnityEngine;

/// <summary>
/// [I] 원거리 스킬.
///
/// 0단계: 발사 없음
/// 1단계: 느린 투사체 (단발)
/// 2단계: 빠른 투사체 (단발)
/// 3단계: [혈연의 쐐기] 즉발 레이저 — 직선 관통, 체력 희생
/// </summary>
public class ProjectileSkill : SkillBase
{
    [Header("[I] 투사체 설정")]
    [SerializeField] private float      baseDamage    = 20f;
    [SerializeField] private float      stage1Speed   = 8f;
    [SerializeField] private float      stage2Speed   = 20f;
    [SerializeField] private float      projectileLifetime = 5f;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private Projectile projectilePrefab;

    [Header("[I] 3단계 레이저")]
    [SerializeField] private float sniperHpCost  = 30f;
    [SerializeField] private float sniperDamage  = 80f;
    [SerializeField] private float sniperRange   = 15f;
    [SerializeField] private float laserHeight   = 0.3f;   // 레이저 두께 (시각)

    protected override bool CanUse()
    {
        if (currentStage >= 3 && Context.Stats.CurrentCleanWater <= sniperHpCost)
        {
            Debug.Log($"[I] 혈연의 쐐기 불가 — HP 부족 ({Context.Stats.CurrentCleanWater:F0}/{sniperHpCost:F0})");
            return false;
        }
        return true;
    }

    protected override IEnumerator ExecuteSkill()
    {
        switch (currentStage)
        {
            case 0:
                Debug.Log("[I] 0단계 — 발사 없음 (어그로 모션)");
                break;

            case 1:
                FireProjectile(baseDamage, stage1Speed, penetrating: false);
                Debug.Log($"[I] 수압탄 — 속도: {stage1Speed} | 데미지: {baseDamage:F0}");
                break;

            case 2:
                FireProjectile(baseDamage * 1.3f, stage2Speed, penetrating: false);
                Debug.Log($"[I] 고속 수압탄 — 속도: {stage2Speed} | 데미지: {baseDamage * 1.3f:F0}");
                break;

            case 3:
                FireLaser();
                break;
        }
        yield break;
    }

    // 투사체 발사 (1~2단계)
    private void FireProjectile(float damage, float speed, bool penetrating)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[I] Projectile Prefab이 연결되지 않았습니다.");
            return;
        }

        Vector3    pos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = Context.FacingRight ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        Projectile proj = Instantiate(projectilePrefab, pos, rot);
        proj.Initialize(damage, GetCorruptionDamage(damage), speed, penetrating, projectileLifetime, enemyLayer);

        // 발사 위치 표시
        ShowBoxIndicator(firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position,
                         new Vector2(0.2f, 0.2f));
    }

    // 즉발 레이저 (3단계) — 직선 관통
    private void FireLaser()
    {
        Context.Stats.SacrificeWater(sniperHpCost);

        Vector2 size   = new Vector2(sniperRange, laserHeight);
        Vector2 center = GetFrontBoxCenter(sniperRange);

        // 레이저 시각 표시
        ShowBoxIndicator(center, size);

        // 관통 판정 — 범위 내 모든 적 타격
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        foreach (var h in hits)
            h.GetComponent<IDamageable>()?.TakeDamage(sniperDamage, GetCorruptionDamage(sniperDamage));

        // 에디터용 레이 시각화
        float   dir    = Context.FacingRight ? 1f : -1f;
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Debug.DrawRay(origin, Vector2.right * dir * sniperRange, Color.cyan, 0.5f);

        Debug.Log($"[I] 혈연의 쐐기 — 레이저 사거리: {sniperRange:F0} | " +
                  $"관통 적중: {hits.Length}명 | 데미지: {sniperDamage:F0} | HP 소모: {sniperHpCost:F0}");
    }

    private void OnDrawGizmosSelected()
    {
        if (currentStage == 3)
        {
            float   dir    = transform.localScale.x > 0 ? 1f : -1f;
            Vector2 center = (Vector2)transform.position + Vector2.right * dir * sniperRange * 0.5f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, new Vector3(sniperRange, laserHeight, 0f));
        }
        else
        {
            Vector3 pos = firePoint != null ? firePoint.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pos, 0.15f);
        }
    }
}
