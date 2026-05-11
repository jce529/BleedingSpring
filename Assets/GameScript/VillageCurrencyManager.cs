using System;
using UnityEngine;

/// <summary>
/// 플레이어가 보유한 마을 재화(살점 정수)를 관리하는 싱글톤 매니저입니다.
/// 적 정화 시 획득하며, NPC 서비스 구매에 사용됩니다.
/// </summary>
public class VillageCurrencyManager : MonoBehaviour
{
    public static VillageCurrencyManager Instance { get; private set; }

    [Header("보유 재화")]
    [SerializeField] private int fleshEssence = 0;

    public int CurrentEssence => fleshEssence;

    public event Action<int> OnEssenceChanged;

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

    /// <summary>재화를 획득합니다. 적 등급에 따라 다른 양을 주입받습니다.</summary>
    public void AddEssence(int amount)
    {
        if (amount <= 0) return;
        fleshEssence += amount;
        Debug.Log($"[Currency] 살점 정수 획득: +{amount} (현재: {fleshEssence})");
        OnEssenceChanged?.Invoke(fleshEssence);
    }

    /// <summary>재화를 소모합니다. 부족할 경우 false를 반환합니다.</summary>
    public bool TrySpendEssence(int cost)
    {
        if (fleshEssence < cost)
        {
            Debug.LogWarning($"[Currency] 재화 부족! (보유: {fleshEssence} / 필요: {cost})");
            return false;
        }

        fleshEssence -= cost;
        Debug.Log($"[Currency] 재화 소모: -{cost} (남은 양: {fleshEssence})");
        OnEssenceChanged?.Invoke(fleshEssence);
        return true;
    }
}
