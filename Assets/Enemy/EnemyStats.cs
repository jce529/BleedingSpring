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

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private bool isDead;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        CurrentHp         = maxHp;
        CurrentCorruption = maxCorruption;  // 적은 완전히 오염된 상태에서 시작
        isDead            = false;
    }

    // ─── 핵심 공개 API ───────────────────────────────────────────────────────

    /// <summary>
    /// 피격 처리. 체력과 오염도를 각각 감소시킵니다.
    /// 오염도는 0 이하로 떨어질 수 있습니다 (과정화 → 파괴 판정).
    /// 체력이 0 이하가 되면 CheckDeathState()를 호출합니다.
    /// </summary>
    /// <param name="hpDamage">감소할 체력량</param>
    /// <param name="corruptionDamage">감소할 오염도량</param>
    public void TakeDamage(float hpDamage, float corruptionDamage)
    {
        if (isDead) return;

        CurrentHp         -= hpDamage;
        CurrentCorruption -= corruptionDamage;  // 0 이하도 허용 (과정화)

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
    /// 실제 정화 범위: [basePurificationMin - margin, basePurificationMax + margin]
    /// </summary>
    private void CheckDeathState()
    {
        float ratio        = CorruptionRatio;
        float effectiveMin = basePurificationMin - bonusPurificationMargin;
        float effectiveMax = basePurificationMax + bonusPurificationMargin;

        Debug.Log($"[EnemyStats] 사망 판정 — 오염 비율: {ratio * 100f:F1}% | " +
                  $"정화 범위: [{effectiveMin * 100f:F1}% ~ {effectiveMax * 100f:F1}%]");

        if (ratio >= effectiveMin && ratio <= effectiveMax)
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
    private void Purify()
    {
        Debug.Log($"[EnemyStats] ✦ {gameObject.name} 정화(Purified) 성공! ✦");

        OnDeath?.Invoke();
        enabled = false;   // EnemyStats 비활성화

        // PurifiedNPC가 없으면 자동으로 추가
        var npc = GetComponent<PurifiedNPC>() ?? gameObject.AddComponent<PurifiedNPC>();
        npc.Activate();
    }

    /// <summary>파괴 판정 처리. 오염이 너무 높거나 낮은 상태로 사망 또는 공격 비용으로 사망.</summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[EnemyStats] ✕ {gameObject.name} 파괴(Died). 정화 실패.");
        OnDeath?.Invoke();
        // TODO: 파괴 이펙트, 패널티 처리
        Destroy(gameObject);
    }

    // ─── 디버그 ──────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnGUI()
    {
        // 씬 뷰에서 오브젝트를 선택했을 때만 표시하지 않고, 모든 Enemy 상태를 화면에 출력
        // (적이 많을 경우 비활성화 권장)
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
