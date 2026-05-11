using System;
using UnityEngine;

/// <summary>
/// 적의 체력(HP)과 오염도(Corruption)를 관리하고, 정화(Purify) vs 파괴(Die) 판정을 수행합니다.
///
/// 핵심 메커니즘:
///   - 적은 처음에 최대 오염도(완전히 오염된 상태)에서 시작합니다.
///   - 플레이어의 공격으로 HP와 오염도가 함께 감소합니다.
///   - HP가 0 이하가 되는 순간 '현재 오염 비율'을 검사해 정화/파괴를 결정합니다.
///   - 오염도는 0 이하로 내려갈 수 있습니다 (과정화 상태 → 파괴 판정).
/// </summary>
public class EnemyStats : MonoBehaviour, IDamageable
{
    // ─── Inspector 설정 ───────────────────────────────────────────────────────

    [Header("체력 설정")]
    [SerializeField] private float maxHp          = 100f;
    [SerializeField] private float maxCorruption  = 100f;

    [Header("정화 판정 범위 (오염 비율 0.0 ~ 1.0)")]
    [Tooltip("기본 정화 최소 비율. 예: 0.3 = 오염도 30% 이상")]
    public float basePurificationMin     = 0.3f;

    [Tooltip("기본 정화 최대 비율. 예: 0.7 = 오염도 70% 이하")]
    public float basePurificationMax     = 0.7f;

    [Tooltip("아이템/업그레이드로 추가되는 범위 확장치. min에서 빼고 max에 더합니다.")]
    public float bonusPurificationMargin = 0f;

    // ─── 공개 상태 ───────────────────────────────────────────────────────────

    public float CurrentHp         { get; private set; }
    public float CurrentCorruption { get; private set; }
    public float MaxHp             => maxHp;
    public float MaxCorruption     => maxCorruption;

    /// <summary>현재 오염 비율 (CurrentCorruption / maxCorruption). 음수도 가능(과정화).</summary>
    public float CorruptionRatio   => CurrentCorruption / maxCorruption;

    // ─── 이벤트 ──────────────────────────────────────────────────────────────

    /// <summary>피격(TakeDamage) 시 발생. EnemyAI가 Hit 상태 진입에 사용합니다.</summary>
    public event Action OnDamaged;

    /// <summary>사망(Die 또는 Purify) 시 발생. EnemyAI가 Dead 상태 진입에 사용합니다.</summary>
    public event Action OnDeath;

    /// <summary>HP 변경 시 발생 (current, max). UI 바가 구독한다. TECH-02.</summary>
    public event Action<float, float> OnHpChanged;

    /// <summary>오염도 변경 시 발생 (current, max). UI 바가 구독한다.</summary>
    public event Action<float, float> OnCorruptionChanged;

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private bool isDead;
    private bool isInvincible;

    public bool IsDead       => isDead;
    public bool IsInvincible { get => isInvincible; set => isInvincible = value; }

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        CurrentHp         = maxHp;
        CurrentCorruption = maxCorruption;  // 적은 완전히 오염된 상태에서 시작
        isDead            = false;
        isInvincible      = false;

        // 대장장이 보너스 적용
        if (VillageManager.Instance != null)
        {
            bonusPurificationMargin += VillageManager.Instance.GetPurifyMarginBonus();
        }
    }

    // ─── 핵심 공개 API ───────────────────────────────────────────────────────

    /// <summary>
    /// 피격 처리. 체력과 오염도를 각각 감소시킵니다.
    /// 오염도는 0 이하로 떨어질 수 있습니다 (과정화 → 파괴 판정).
    /// 체력이 0 이하가 되면 CheckDeathState()를 호출합니다.
    /// </summary>
    public virtual void TakeDamage(float hpDamage, float corruptionDamage)
    {
        if (isDead || isInvincible) return;

        CurrentHp         -= hpDamage;
        CurrentCorruption -= corruptionDamage;  // 0 이하도 허용 (과정화)

        OnHpChanged?.Invoke(CurrentHp, maxHp);
        OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruption);

        Debug.Log($"[EnemyStats] {gameObject.name} 피격 — " +
                  $"HP: {CurrentHp:F1}/{maxHp} | " +
                  $"오염도: {CurrentCorruption:F1}/{maxCorruption} " +
                  $"({CorruptionRatio * 100f:F1}%)");

        OnDamaged?.Invoke();

        if (CurrentHp <= 0f)
            CheckDeathState();
    }

    /// <summary>
    /// EnemyAttack에서 공격 자원으로 체력을 소모할 때 호출합니다.
    /// 체력이 0 이하가 되면 정화 불가 파괴(Die)를 즉시 실행합니다.
    /// </summary>
    /// <param name="cost">소모할 체력량</param>
    /// <returns>소모 후 생존 시 true, 사망 시 false</returns>
    public bool SpendHpOnAttack(float cost)
    {
        if (isDead) return false;

        CurrentHp -= cost;

        OnHpChanged?.Invoke(CurrentHp, maxHp);

        if (CurrentHp <= 0f)
        {
            Die();  // 공격하다 죽은 것 → 정화 불가, 바로 파괴
            return false;
        }

        return true;
    }

    /// <summary>
    /// 아이템/업그레이드로 정화 유효 범위를 넓혀줍니다.
    /// bonusPurificationMargin을 amount만큼 증가시킵니다.
    /// </summary>
    /// <param name="amount">추가할 범위 넓이 (양수)</param>
    public void WidenPurificationRange(float amount)
    {
        bonusPurificationMargin += amount;
        Debug.Log($"[EnemyStats] 정화 범위 확장 +{amount:F2} → " +
                  $"실제 범위: [{basePurificationMin - bonusPurificationMargin:F2}, " +
                  $"{basePurificationMax + bonusPurificationMargin:F2}]");
    }

    // ─── 파괴/정화 판정 ──────────────────────────────────────────────────────

    /// <summary>
    /// 현재 오염 비율을 검사해 정화(Purify) 또는 파괴(Die)를 결정합니다.
    /// [세계관 준수] HP가 낮을수록 자아 형성을 위한 정화 범위(Sweet Spot)가 좁아집니다.
    /// </summary>
    protected virtual void CheckDeathState()
    {
        float ratio = CorruptionRatio;
        float hpPercent = Mathf.Clamp01(CurrentHp / maxHp); // 0이겠지만 안전을 위해

        // HP가 낮을수록 범위가 좁아짐 (기본 범위의 중심인 0.5f로 수렴)
        // hpPercent가 0에 가까울수록 lerp 결과가 1.0f가 되어 범위 폭이 최소화됨
        float shrinkFactor = 1f - hpPercent; 
        float center = (basePurificationMin + basePurificationMax) * 0.5f;
        
        // HP 100%일 때 기존 범위, HP 0%에 가까울수록 center로 수축 (최소 10%의 여유는 보장)
        float currentMin = Mathf.Lerp(basePurificationMin - bonusPurificationMargin, center - 0.05f, shrinkFactor);
        float currentMax = Mathf.Lerp(basePurificationMax + bonusPurificationMargin, center + 0.05f, shrinkFactor);

        Debug.Log($"[EnemyStats] 최종 사망 판정 — 오염 비율: {ratio * 100f:F1}% | " +
                  $"동적 정화 범위: [{currentMin * 100f:F1}% ~ {currentMax * 100f:F1}%] (수축률: {shrinkFactor * 100f:F0}%)");

        if (ratio >= currentMin && ratio <= currentMax)
        {
            isDead = true;
            Purify();
        }
        else
        {
            Die();
        }
    }

    /// <summary>정화 판정 처리. PurifiedNPC가 있으면 NPC로 전환, 없으면 오브젝트 제거.</summary>
    protected virtual void Purify()
    {
        Debug.Log($"[EnemyStats] ✦ {gameObject.name} 정화(Purified) 성공! ✦");

        WorldPurificationManager.Instance?.ReportPurification(purificationType);

        // 재화 획득 (Pond: 1 | River: 10 | Lake: 50)
        int reward = purificationType switch
        {
            WorldPurificationManager.EnemyType.Pond => 1,
            WorldPurificationManager.EnemyType.River => 10,
            WorldPurificationManager.EnemyType.Lake => 50,
            _ => 0
        };
        VillageCurrencyManager.Instance?.AddEssence(reward);

        OnDeath?.Invoke();
        enabled = false;   // EnemyStats 비활성화

        // PurifiedNPC가 없으면 자동으로 추가
        var npc = GetComponent<PurifiedNPC>() ?? gameObject.AddComponent<PurifiedNPC>();
        npc.Activate();

        OnPurified(npc);
    }

    /// <summary>하위 클래스에서 정화 성공 후 NPC 데이터를 설정하는 등의 후처리에 사용합니다.</summary>
    protected virtual void OnPurified(PurifiedNPC npc) { }

    /// <summary>파괴 판정 처리. 오염이 너무 높거나 낮은 상태로 사망 또는 공격 비용으로 사망.</summary>
    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[EnemyStats] ✕ {gameObject.name} 파괴(Died). 정화 실패.");
        
        WorldPurificationManager.Instance?.ReportDestruction(purificationType);

        OnDeath?.Invoke();
        // TODO: 파괴 이펙트, 패널티 처리
        Destroy(gameObject);
    }

    // ─── 디버그 ──────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnGUI()
    {
        float effectiveMin = basePurificationMin - bonusPurificationMargin;
        float effectiveMax = basePurificationMax + bonusPurificationMargin;

        GUIStyle style      = new GUIStyle(GUI.skin.label);
        style.fontSize      = 14;
        style.normal.textColor = Color.red;

        string info =
            $"[{gameObject.name}]\n" +
            $"HP        : {CurrentHp:F0}/{maxHp}\n" +
            $"Corruption: {CurrentCorruption:F0}/{maxCorruption} ({CorruptionRatio * 100f:F1}%)\n" +
            $"Sweet Spot: [{effectiveMin * 100f:F0}% ~ {effectiveMax * 100f:F0}%]";

        GUI.Label(new Rect(10, 200, 300, 120), info, style);
    }
#endif
}
bel(new Rect(10, 200, 300, 120), info, style);
    }
#endif
}
