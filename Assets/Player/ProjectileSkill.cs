using System.Collections;
using UnityEngine;

/// <summary>
/// [I] 원거리 스킬. 단일 대상 견제 및 타격.
///
/// 0단계 [빈 수관 발사]:   어그로용, 데미지 극소
/// 1단계 [수압탄]:         고속 단발 물탄
/// 2단계 [관통 수창]:      일직선 관통 투사체
/// 3단계 [혈연의 쐐기]:    체력 희생(sniperHpCost) → 레이캐스트 즉발 레이저, 부위 파괴
/// </summary>
public class ProjectileSkill : SkillBase
{
    [Header("[I] 원거리 스킬")]
    [SerializeField] private float     baseDamage   = 20f;
    [SerializeField] private float     sniperHpCost = 30f;   // 3단계 체력 소모
    [SerializeField] private float     sniperDamage = 80f;   // 3단계 데미지
    [SerializeField] private float     sniperRange  = 30f;   // 3단계 사거리
    [SerializeField] private Transform firePoint;            // 발사 기준점
    [SerializeField] private Projectile projectilePrefab;

    // ─── CanUse 오버라이드 ───────────────────────────────────────────────────

    protected override bool CanUse()
    {
        if (currentStage >= 3 && Context.Stats.CurrentHp <= sniperHpCost)
        {
            Debug.Log("[I] 체력 부족 — 혈연의 쐐기 사용 불가");
            return false;
        }
        return true;
    }

    // ─── ExecuteSkill 구현 ───────────────────────────────────────────────────

    protected override IEnumerator ExecuteSkill()
    {
        switch (currentStage)
        {
            case 0: FireAggro();       break;
            case 1: FireWaterBullet(); break;
            case 2: FireWaterSpear();  break;
            case 3: FireWaterSniper(); break;
        }
        yield break;
    }

    // 0단계: 어그로용 — 느리고 데미지 극소
    private void FireAggro()
    {
        SpawnProjectile(damage: 1f, speed: 6f, penetrating: false, lifetime: 3f);
    }

    // 1단계: 수압탄 — 고속 단발
    private void FireWaterBullet()
    {
        SpawnProjectile(damage: baseDamage, speed: 20f, penetrating: false, lifetime: 4f);
    }

    // 2단계: 관통 수창 — 직선 관통
    private void FireWaterSpear()
    {
        SpawnProjectile(damage: baseDamage * 1.3f, speed: 15f, penetrating: true, lifetime: 5f);
    }

    // 3단계: 혈연의 쐐기 — 레이캐스트 즉발, 부위 파괴
    private void FireWaterSniper()
    {
        Context.Stats.TrySacrificeHp(sniperHpCost);

        float   dir    = Context.FacingRight ? 1f : -1f;
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.right * dir, sniperRange, enemyLayer);
        if (hits.Length > 0)
        {
            hits[0].collider.GetComponent<IDamageable>()?.TakeDamage(sniperDamage);
            // TODO: 부위 파괴 이벤트 연결
        }

        // 에디터 레이 시각화
        Debug.DrawRay(origin, Vector2.right * dir * sniperRange, Color.cyan, 0.5f);
        // TODO: 레이저 비주얼 이펙트 재생
    }

    // ─── 유틸리티 ────────────────────────────────────────────────────────────

    private void SpawnProjectile(float damage, float speed, bool penetrating, float lifetime)
    {
        if (projectilePrefab == null) return;

        Vector3    pos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = Context.FacingRight ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        Projectile proj = Instantiate(projectilePrefab, pos, rot);
        proj.Initialize(damage, speed, penetrating, lifetime, enemyLayer);
    }
}
