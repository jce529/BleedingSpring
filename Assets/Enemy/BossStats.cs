using UnityEngine;

/// <summary>
/// 보스 전용 스탯 클래스.
/// 일반 적의 EnemyStats를 상속하며, 보스 이름 및 정화 가능 구간(Sweet Spot) 체크 기능을 추가합니다.
/// </summary>
public class BossStats : EnemyStats
{
    [Header("보스 설정")]
    [SerializeField] private string bossName = "New Boss";
    
    public string BossName => bossName;

    /// <summary>
    /// 현재 오염 비율이 정화 가능 구간(Sweet Spot) 내에 있는지 실시간으로 반환합니다.
    /// </summary>
    public bool IsInPurificationRange
    {
        get
        {
            float ratio = CorruptionRatio;
            float effectiveMin = basePurificationMin - bonusPurificationMargin;
            float effectiveMax = basePurificationMax + bonusPurificationMargin;
            return ratio >= effectiveMin && ratio <= effectiveMax;
        }
    }
}
