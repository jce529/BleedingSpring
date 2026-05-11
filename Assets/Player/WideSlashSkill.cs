using System.Collections;
using UnityEngine;

/// <summary>
/// [U] 참격 스킬. 단계별로 박스 크기가 커집니다.
///
/// 0단계: 기본공격과 동일한 크기 박스 + 밀쳐냄
/// 1단계: 중간 박스
/// 2단계: 큰 박스
/// 3단계: [해일참] 초대형 박스, 체력 희생, 오염 장판 제거
/// </summary>
public class WideSlashSkill : SkillBase
{
    [Header("[U] 참격 — 기준 박스 (0단계 = 기본공격 크기)")]
    [SerializeField] private float  baseDamage   = 30f;
    [SerializeField] private float  baseWidth    = 1.5f;   // 0단계 너비 (기본공격과 동일)
    [SerializeField] private float  baseHeight   = 1.0f;   // 0단계 높이

    [Header("[U] 3단계 해일참")]
    [SerializeField] private float      tsunamiHpCost = 40f;
    [SerializeField] private float      tsunamiWidth  = 15f;
    [SerializeField] private float      tsunamiHeight = 6f;
    [SerializeField] private GameObject waterPillarEffect;
    [SerializeField] private GameObject tsunamiEffect;

    // 단계별 크기 배율 (0단계 = 1.0 기준)
    private static readonly float[] WidthMult  = { 1.0f, 1.7f, 2.5f, 0f };
    private static readonly float[] HeightMult = { 1.0f, 1.5f, 2.0f, 0f };

    // 단계별 데미지 배율
    private static readonly float[] DamageMult = { 1.0f, 1.2f, 1.5f, 3.0f };

    protected override bool CanUse()
    {
        if (Stage >= 3 && Context.Stats.CurrentCleanWater <= tsunamiHpCost * costMultiplier)
        {
            Debug.Log($"[U] 해일참 불가 — HP 부족 ({Context.Stats.CurrentCleanWater:F0}/{tsunamiHpCost * costMultiplier:F0})");
            return false;
        }
        return true;
    }

    protected override IEnumerator ExecuteSkill()
    {
        if (Stage <= 2)
            ExecuteSlash(Stage);
        else
            ExecuteTsunamiSlash();

        yield break;
    }

    // 0~2단계 공통 슬래시
    private void ExecuteSlash(int stage)
    {
        float   width  = baseWidth  * WidthMult[stage];
        float   height = baseHeight * HeightMult[stage];
        float   damage = baseDamage * DamageMult[stage] * effectMultiplier;
        Vector2 size   = new Vector2(width, height);
        Vector2 center = GetFrontBoxCenter(width);

        ShowBoxIndicator(center, size);

        var hits      = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        int knockbacks = 0;

        foreach (var h in hits)
        {
            h.GetComponent<IDamageable>()?.TakeDamage(damage, GetCorruptionDamage(damage));

            // 0단계: 밀쳐냄
            if (stage == 0)
            {
                var rb = h.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = (h.transform.position - transform.position).normalized;
                    rb.AddForce(dir * 8f, ForceMode2D.Impulse);
                    knockbacks++;
                }
            }
        }

        string[] stageNames = { "횡베기", "수면 가르기", "폭풍 베기" };
        string   knockInfo  = stage == 0 ? $" | 넉백: {knockbacks}명" : "";
        Debug.Log($"[U] {stageNames[stage]} — 박스: {width:F1}×{height:F1} | 적중: {hits.Length}명{knockInfo} | 데미지: {damage:F0}");
    }

    // 3단계: 해일참
    private void ExecuteTsunamiSlash()
    {
        Context.Stats.SacrificeWater(tsunamiHpCost * costMultiplier);

        Vector2 size   = new Vector2(tsunamiWidth, tsunamiHeight);
        Vector2 center = GetFrontBoxCenter(tsunamiWidth);

        ShowBoxIndicator(center, size);

        var enemyHits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        foreach (var h in enemyHits)
        {
            float dmg = baseDamage * DamageMult[3] * effectMultiplier;
            h.GetComponent<IDamageable>()?.TakeDamage(dmg, GetCorruptionDamage(dmg));
        }

        var contamHits = Physics2D.OverlapBoxAll(center, size, 0f, LayerMask.GetMask("Contamination"));
        foreach (var c in contamHits)
            Destroy(c.gameObject);

        Debug.Log($"[U] 해일참 — 박스: {tsunamiWidth:F0}×{tsunamiHeight:F0} | " +
                  $"적중: {enemyHits.Length}명 | HP 소모: {tsunamiHpCost * costMultiplier:F0} | 오염 제거: {contamHits.Length}개");

        if (tsunamiEffect != null)
            Instantiate(tsunamiEffect, transform.position, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        float width, height;

        if (Stage >= 3)
        {
            width  = tsunamiWidth;
            height = tsunamiHeight;
        }
        else
        {
            int s = Mathf.Clamp(Stage, 0, 2);
            width  = baseWidth  * WidthMult[s];
            height = baseHeight * HeightMult[s];
        }

        Vector2 center = GetFrontBoxCenter(width);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0f));
    }
}
