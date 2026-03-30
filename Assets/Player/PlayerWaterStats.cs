using System;
using UnityEngine;

/// <summary>
/// 플레이어의 핵심 자원(물 = 체력, 오염도)을 관리합니다.
///
/// 게임 핵심 메커니즘:
///   - 물(Water)은 체력이자 스킬 자원입니다. 스킬 사용 시 SacrificeWater()로 소모합니다.
///   - 적에게 피격 시 데미지뿐 아니라 오염도(Corruption)도 증가합니다.
///   - 오염도가 현재 체력(CurrentCleanWater) 이상이 되면 즉사합니다.
///   - waterTier(0~3)는 O키로 순환하며, 스킬 위력과 소모량에 영향을 줍니다.
/// </summary>
public class PlayerWaterStats : MonoBehaviour, IDamageable
{
    // ─── Inspector 설정 ───────────────────────────────────────────────────────

    [Header("물(체력) 설정")]
    [SerializeField] private float maxCleanWater = 100f;

    [Header("오염도 설정")]
    public float maxCorruptionThreshold = 100f;   // public: 적마다 오염 비율이 다르므로 외부 참조 가능

    // ─── 공개 상태 ───────────────────────────────────────────────────────────

    public float CurrentCleanWater  { get; private set; }
    public float MaxCleanWater      => maxCleanWater;
    public float WaterRatio         => CurrentCleanWater / maxCleanWater;

    public float CurrentCorruption  { get; private set; }
    /// <summary>오염도 비율 (0~1). UI 게이지 표시에 사용합니다.</summary>
    public float CorruptionRatio    => CurrentCorruption / maxCorruptionThreshold;

    /// <summary>현재 수분 단계 (0~3). O키로 순환합니다.</summary>
    public int WaterTier            { get; private set; } = 0;

    // ─── 이벤트 ──────────────────────────────────────────────────────────────

    /// <summary>(현재 물, 최대 물) — UI HP 바 갱신에 사용합니다.</summary>
    public event Action<float, float> OnWaterChanged;

    /// <summary>(현재 오염도, 최대 임계치) — UI 오염 게이지 갱신에 사용합니다.</summary>
    public event Action<float, float> OnCorruptionChanged;

    /// <summary>waterTier가 변경될 때 발생합니다.</summary>
    public event Action<int> OnWaterTierChanged;

    /// <summary>사망(체력 0 또는 오염도 초과) 시 한 번 발생합니다.</summary>
    public event Action OnDeath;

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private bool isDead;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        CurrentCleanWater = maxCleanWater;
        CurrentCorruption = 0f;
        isDead            = false;
    }

    // ─── IDamageable 구현 ────────────────────────────────────────────────────

    /// <summary>
    /// IDamageable 인터페이스 구현. hpDamage만큼 체력을, corruptionDamage만큼 오염도를 절댓값으로 증가시킵니다.
    /// (환경 피해, 낙하 데미지 등에 사용. corruptionDamage = 0이면 오염도 변화 없음)
    /// </summary>
    public void TakeDamage(float hpDamage, float corruptionDamage)
    {
        if (isDead || hpDamage <= 0f) return;

        CurrentCleanWater = Mathf.Max(0f, CurrentCleanWater - hpDamage);
        OnWaterChanged?.Invoke(CurrentCleanWater, maxCleanWater);

        if (corruptionDamage > 0f)
        {
            CurrentCorruption = Mathf.Min(maxCorruptionThreshold, CurrentCorruption + corruptionDamage);
            OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionThreshold);
        }

        CheckDeath();
    }

    // ─── 핵심 공개 API ───────────────────────────────────────────────────────

    /// <summary>
    /// 적의 공격을 받습니다.
    /// 체력을 damage만큼 줄이고, attackerCorruptionRatio에 비례해 오염도를 증가시킵니다.
    /// </summary>
    /// <param name="damage">받는 데미지</param>
    /// <param name="attackerCorruptionRatio">공격자의 오염 비율 (0~1). 이 비율 × maxCorruptionThreshold 만큼 오염도 증가.</param>
    public void ReceiveAttack(float damage, float attackerCorruptionRatio)
    {
        if (isDead || damage <= 0f) return;

        // 체력 감소
        CurrentCleanWater = Mathf.Max(0f, CurrentCleanWater - damage);
        OnWaterChanged?.Invoke(CurrentCleanWater, maxCleanWater);

        // 오염도 증가
        float corruptionGain = attackerCorruptionRatio * maxCorruptionThreshold;
        if (corruptionGain > 0f)
        {
            CurrentCorruption = Mathf.Min(maxCorruptionThreshold, CurrentCorruption + corruptionGain);
            OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionThreshold);
        }

        CheckDeath();
    }

    /// <summary>
    /// 스킬 발동을 위해 체력을 소모합니다.
    /// 소모 후 물이 0 이하가 되면 실패(false)를 반환하며 소모하지 않습니다.
    /// </summary>
    /// <param name="cost">소모할 물의 양</param>
    /// <returns>소모 성공 여부</returns>
    public bool SacrificeWater(float cost)
    {
        if (isDead) return false;
        if (CurrentCleanWater <= cost) return false;   // 생존 최소 물 보장

        CurrentCleanWater -= cost;
        OnWaterChanged?.Invoke(CurrentCleanWater, maxCleanWater);
        return true;
    }

    /// <summary>체력을 회복합니다. 오염도는 감소하지 않습니다.</summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        CurrentCleanWater = Mathf.Min(maxCleanWater, CurrentCleanWater + amount);
        OnWaterChanged?.Invoke(CurrentCleanWater, maxCleanWater);
    }

    /// <summary>오염도를 정화합니다. (맵 정화 등 외부 이벤트 연동)</summary>
    public void Purify(float amount)
    {
        if (amount <= 0f) return;
        CurrentCorruption = Mathf.Max(0f, CurrentCorruption - amount);
        OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionThreshold);
    }

    /// <summary>O키 입력 시 호출. waterTier를 0→1→2→3→0 순으로 순환합니다.</summary>
    public void CycleWaterTier()
    {
        WaterTier = (WaterTier + 1) % 4;
        OnWaterTierChanged?.Invoke(WaterTier);
    }

    /// <summary>
    /// 사망 조건을 검사합니다.
    ///   - 체력(CurrentCleanWater) ≤ 0
    ///   - 오염도(CurrentCorruption) >= 현재 체력(CurrentCleanWater)
    /// 조건 충족 시 OnDeath 이벤트를 발생시키고 GameStateManager에 GameOver를 알립니다.
    /// </summary>
    public void CheckDeath()
    {
        if (isDead) return;

        bool waterDepleted   = CurrentCleanWater <= 0f;
        bool corruptionDeath = CurrentCorruption >= CurrentCleanWater;

        if (!waterDepleted && !corruptionDeath) return;

        isDead = true;

        if (corruptionDeath)
            Debug.Log("[PlayerWaterStats] 오염도 >= 현재 HP -- 즉사 처리");
        else
            Debug.Log("[PlayerWaterStats] 체력 고갈 — 사망 처리");

        OnDeath?.Invoke();
        GameStateManager.Instance?.SetState(GameStateManager.GameState.GameOver);
    }
}
