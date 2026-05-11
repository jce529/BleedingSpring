using System;
using UnityEngine;

/// <summary>
/// 보스 전용 스탯 클래스.
/// 3단계 페이즈(Phase) 시스템을 관리하며, 각 페이즈마다 독립적인 패턴 전이를 지원합니다.
/// </summary>
public class BossStats : EnemyStats
{
    [Header("보스 기본 설정")]
    [SerializeField] private string bossName = "Lake Boss";
    
    [Header("페이즈 설정 (HP 비율)")]
    [Range(0f, 1f)] [SerializeField] private float phase2Threshold = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float phase3Threshold = 0.35f;

    public string BossName => bossName;
    public int    CurrentPhase { get; private set; } = 1;

    /// <summary>페이즈가 변경될 때 발생 (newPhase)</summary>
    public event Action<int> OnPhaseChanged;

    private void Start()
    {
        CurrentPhase = 1;
        purificationType = WorldPurificationManager.EnemyType.Lake;
    }

    public override void TakeDamage(float hpDamage, float corruptionDamage)
    {
        base.TakeDamage(hpDamage, corruptionDamage);
        CheckPhaseTransition();
    }

    private void CheckPhaseTransition()
    {
        if (IsDead) return;

        float hpRatio = CurrentHp / MaxHp;
        int nextPhase = CurrentPhase;

        if (hpRatio <= phase3Threshold)
            nextPhase = 3;
        else if (hpRatio <= phase2Threshold)
            nextPhase = 2;

        if (nextPhase > CurrentPhase)
        {
            CurrentPhase = nextPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
            Debug.Log($"[BossStats] {bossName} 페이즈 전환 → {CurrentPhase}단계 (HP {hpRatio*100f:F0}%)");
        }
    }

    /// <summary>
    /// 현재 오염 비율이 정화 가능 구간(Sweet Spot) 내에 있는지 실시간으로 반환합니다.
    /// 부모 클래스의 동적 범위 로직과 완벽히 동기화됩니다.
    /// </summary>
    public bool IsInPurificationRange
    {
        get
        {
            float ratio = CorruptionRatio;
            float hpPercent = Mathf.Clamp01(CurrentHp / MaxHp);

            float shrinkFactor = 1f - hpPercent; 
            float center = (basePurificationMin + basePurificationMax) * 0.5f;
            
            float currentMin = Mathf.Lerp(basePurificationMin - bonusPurificationMargin, center - 0.05f, shrinkFactor);
            float currentMax = Mathf.Lerp(basePurificationMax + bonusPurificationMargin, center + 0.05f, shrinkFactor);

            return ratio >= currentMin && ratio <= currentMax;
        }
    }
}
