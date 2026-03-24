using System;
using UnityEngine;

/// <summary>
/// 플레이어의 체력(HP)을 관리합니다.
/// TrySacrificeHp()로 스킬 시전 시 체력을 소모할 수 있습니다.
/// </summary>
public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("체력")]
    [SerializeField] private float maxHp = 100f;

    public float CurrentHp  { get; private set; }
    public float MaxHp      => maxHp;
    public float HpRatio    => CurrentHp / maxHp;

    // 체력 변경 시 (현재 HP, 최대 HP)
    public event Action<float, float> OnHpChanged;
    public event Action OnDeath;

    private void Awake()
    {
        CurrentHp = maxHp;
    }

    // IDamageable 구현 — 외부에서 데미지를 줄 때 사용 (corruptionDamage는 PlayerStats에서 무시)
    public void TakeDamage(float hpDamage, float corruptionDamage)
    {
        if (hpDamage <= 0) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - hpDamage);
        OnHpChanged?.Invoke(CurrentHp, maxHp);
        if (CurrentHp <= 0f) OnDeath?.Invoke();
    }

    /// <summary>
    /// 스킬 발동을 위해 체력을 소모합니다.
    /// 소모 후 HP가 0 이하가 되면 실패(false)를 반환합니다.
    /// </summary>
    public bool TrySacrificeHp(float amount)
    {
        if (CurrentHp <= amount) return false;   // 생존 체력 보장
        CurrentHp -= amount;
        OnHpChanged?.Invoke(CurrentHp, maxHp);
        return true;
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        OnHpChanged?.Invoke(CurrentHp, maxHp);
    }
}
