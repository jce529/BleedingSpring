using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마을의 상태와 정화된 NPC들의 업그레이드 단계를 관리하는 싱글톤 매니저입니다.
/// </summary>
public class VillageManager : MonoBehaviour
{
    public static VillageManager Instance { get; private set; }

    [Serializable]
    public class UpgradeData
    {
        public int CurrentLevel = 0;
        public int MaxLevel = 5;
        public int BaseCost = 20;
        public float CostMultiplierPerLevel = 1.5f;

        public int GetNextUpgradeCost() => Mathf.FloorToInt(BaseCost * Mathf.Pow(CostMultiplierPerLevel, CurrentLevel));
        public bool IsMaxLevel => CurrentLevel >= MaxLevel;
    }

    [Header("NPC 업그레이드 데이터")]
    public UpgradeData ShamanUpgrade = new UpgradeData();
    public UpgradeData BlacksmithUpgrade = new UpgradeData();
    public UpgradeData StorytellerUpgrade = new UpgradeData();

    // ─── 상태 ───────────────────────────────────────────────────────────────

    private HashSet<RiverNPCRole> _unlockedRoles = new HashSet<RiverNPCRole>();

    // ─── 이벤트 ─────────────────────────────────────────────────────────────

    public event Action<RiverNPCRole> OnRoleUnlocked;
    public event Action<RiverNPCRole, int> OnUpgradeLeveledUp;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ─── 공개 API ────────────────────────────────────────────────────────────

    public void UnlockNPCRole(RiverNPCRole role)
    {
        if (role == RiverNPCRole.None) return;
        if (_unlockedRoles.Add(role))
        {
            Debug.Log($"[VillageManager] {role} NPC가 합류했습니다. (현재 서비스 단계: 0)");
            OnRoleUnlocked?.Invoke(role);
        }
    }

    public bool IsRoleUnlocked(RiverNPCRole role) => _unlockedRoles.Contains(role);

    /// <summary>NPC의 서비스를 한 단계 업그레이드합니다. 재화와 조건을 검사합니다.</summary>
    public bool TryUpgradeService(RiverNPCRole role)
    {
        if (!IsRoleUnlocked(role)) return false;

        UpgradeData data = GetUpgradeData(role);
        if (data == null || data.IsMaxLevel) return false;

        int cost = data.GetNextUpgradeCost();
        if (VillageCurrencyManager.Instance.TrySpendEssence(cost))
        {
            data.CurrentLevel++;
            Debug.Log($"[VillageManager] {role} 서비스 업그레이드 완료! (Level {data.CurrentLevel})");
            OnUpgradeLeveledUp?.Invoke(role, data.CurrentLevel);
            
            // 주술사 업그레이드 시 즉시 체력 보정
            if (role == RiverNPCRole.Shaman) ApplyShamanImmediateEffect();
            
            return true;
        }

        return false;
    }

    // ─── 수치 계산 API (업그레이드 레벨 반영) ───────────────────────────────────

    public float GetMaxHpBonus() 
    {
        // 레벨당 +10 HP
        return ShamanUpgrade.CurrentLevel * 10f;
    }
    
    public float GetCostMultiplier() 
    {
        // 레벨당 5% 감소 (0.95, 0.90, ...)
        return Mathf.Max(0.5f, 1f - (ShamanUpgrade.CurrentLevel * 0.05f));
    }

    public float GetPurifyMarginBonus() 
    {
        // 레벨당 +2% 범위 확장
        return BlacksmithUpgrade.CurrentLevel * 0.02f;
    }

    public float GetTier0PowerMultiplier() 
    {
        // 레벨당 10% 위력 증가 (1.1, 1.2, ...)
        return 1f + (StorytellerUpgrade.CurrentLevel * 0.1f);
    }

    // ─── 내부 로직 ───────────────────────────────────────────────────────────

    private UpgradeData GetUpgradeData(RiverNPCRole role) => role switch
    {
        RiverNPCRole.Shaman => ShamanUpgrade,
        RiverNPCRole.Blacksmith => BlacksmithUpgrade,
        RiverNPCRole.Storyteller => StorytellerUpgrade,
        _ => null
    };

    private void ApplyShamanImmediateEffect()
    {
        var playerStats = PlayerController.Instance?.GetComponent<PlayerWaterStats>();
        if (playerStats != null)
        {
            playerStats.Heal(10f); // 레벨업 증분만큼 회복 시도
        }
    }
}
