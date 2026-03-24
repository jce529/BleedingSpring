using System.Collections;
using UnityEngine;

/// <summary>
/// [J] 기본 공격. 전방 작은 직사각형 범위.
///
/// 0단계: 물리 찌르기
/// 1단계: 물보라, 리치 +0.3
/// 2단계: 물보라 강화, 리치 +0.6
/// 3단계: 물의 잔상 — 0.1초 후 50% 추가 타격
/// </summary>
public class BasicAttackSkill : SkillBase
{
    [Header("[J] 기본 공격 — 박스 범위")]
    [SerializeField] private float baseDamage  = 15f;
    [SerializeField] private float boxWidth    = 1.5f;   // 전방 너비
    [SerializeField] private float boxHeight   = 1.0f;   // 세로 높이

    // 단계별 너비 보너스
    private static readonly float[] WidthBonus = { 0f, 0.3f, 0.6f, 0f };

    private static readonly string[] StageName =
        { "물리 찌르기", "물보라 베기", "강화 물보라", "물의 잔상" };

    // 쿨다운을 Attack1 애니메이션 길이에 자동 맞춤
    protected override void OnInitialize()
    {
        float clipLen = GetClipLength("HeroKnight_Attack1");
        if (clipLen > 0f) cooldownDuration = clipLen;
    }

    protected override IEnumerator ExecuteSkill()
    {
        float   width  = boxWidth + WidthBonus[currentStage];
        Vector2 size   = new Vector2(width, boxHeight);
        Vector2 center = GetFrontBoxCenter(width);

        Debug.Log($"[J] {StageName[currentStage]} — 박스: {width:F2}×{boxHeight:F2} | 데미지: {baseDamage:F0}");
        ShowBoxIndicator(center, size);

        int hits = DamageBox(center, size, baseDamage);
        Debug.Log($"[J] 1차 적중: {hits}명");

        // 3단계: 잔상 추가 타격
        if (currentStage >= 3)
        {
            yield return new WaitForSeconds(0.1f);
            ShowBoxIndicator(center, size);
            int echoHits = DamageBox(center, size, baseDamage * 0.5f);
            Debug.Log($"[J] 잔상 추가 적중: {echoHits}명 | 데미지: {baseDamage * 0.5f:F0}");
        }

        yield break;
    }

    private int DamageBox(Vector2 center, Vector2 size, float damage)
    {
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        foreach (var h in hits)
            h.GetComponent<IDamageable>()?.TakeDamage(damage, GetCorruptionDamage(damage));
        return hits.Length;
    }

    private void OnDrawGizmosSelected()
    {
        float   width  = boxWidth + (currentStage < 4 ? WidthBonus[currentStage] : 0f);
        float   dir    = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 center = (Vector2)transform.position + Vector2.right * dir * width * 0.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, new Vector3(width, boxHeight, 0f));
    }
}
