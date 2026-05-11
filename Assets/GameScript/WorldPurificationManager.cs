using System;
using UnityEngine;

/// <summary>
/// 월드 전체의 정화 진행도를 관리하는 싱글톤 매니저입니다.
/// 적 정화/파괴 시 점수를 누적하고 엔딩 분기 조건을 판단합니다.
/// </summary>
public class WorldPurificationManager : MonoBehaviour
{
    public static WorldPurificationManager Instance { get; private set; }

    public enum EnemyType
    {
        Pond,   // 일반 적 (소량)
        River,  // 엘리트 적 (중간)
        Lake    // 보스 (대량)
    }

    [Header("정화 점수 설정 (%)")]
    [SerializeField] private float scorePond  = 0.1f;
    [SerializeField] private float scoreRiver = 2.0f;
    [SerializeField] private float scoreLake  = 10.0f;

    // ─── 공개 상태 ───────────────────────────────────────────────────────────

    /// <summary>현재 월드 정화율 (0 ~ 100)</summary>
    public float CurrentPurificationRate { get; private set; } = 0f;

    /// <summary>정화된 호수(보스) 수</summary>
    public int PurifiedLakeCount { get; private set; } = 0;

    /// <summary>파괴된 호수(보스) 수</summary>
    public int DestroyedLakeCount { get; private set; } = 0;

    // ─── 이벤트 ──────────────────────────────────────────────────────────────

    /// <summary>정화율이 변경될 때 발생 (현재 %, 증가분 %)</summary>
    public event Action<float, float> OnPurificationChanged;

    /// <summary>호수(보스) 상태가 결정될 때 발생 (정화 여부)</summary>
    public event Action<bool> OnLakeStatusDecided;

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

    // ─── 핵심 API ────────────────────────────────────────────────────────────

    /// <summary>
    /// 적 정화 성공 시 호출합니다. 정화율을 높입니다.
    /// </summary>
    public void ReportPurification(EnemyType type)
    {
        float gain = type switch
        {
            EnemyType.Pond  => scorePond,
            EnemyType.River => scoreRiver,
            EnemyType.Lake  => scoreLake,
            _               => 0f
        };

        CurrentPurificationRate = Mathf.Min(100f, CurrentPurificationRate + gain);
        
        if (type == EnemyType.Lake)
            PurifiedLakeCount++;

        Debug.Log($"[Purification] 정화 성공! (+{gain}%). 현재: {CurrentPurificationRate:F1}%");
        
        OnPurificationChanged?.Invoke(CurrentPurificationRate, gain);
        if (type == EnemyType.Lake) OnLakeStatusDecided?.Invoke(true);
    }

    /// <summary>
    /// 적 파괴(정화 실패) 시 호출합니다. 정화율은 오르지 않으며 호수의 경우 엔딩에 영향을 줍니다.
    /// </summary>
    public void ReportDestruction(EnemyType type)
    {
        if (type == EnemyType.Lake)
        {
            DestroyedLakeCount++;
            Debug.Log("[Purification] 호수(보스) 파괴됨. 엔딩 경로 B 확정.");
            OnLakeStatusDecided?.Invoke(false);
        }
        else
        {
            Debug.Log($"[Purification] {type} 파괴됨. 정화율 변화 없음.");
        }
    }

    // ─── 엔딩 판정 API ───────────────────────────────────────────────────────

    /// <summary>
    /// 현재까지의 기록을 바탕으로 엔딩 타입을 판정합니다. (세계관 11-1, 11-2 준수)
    /// </summary>
    public string GetEndingPath()
    {
        if (CurrentPurificationRate <= 0.001f)
            return "Path A (멸망: 정화율 0%)";

        if (DestroyedLakeCount > 0)
            return "Path B (전쟁: 호수 파괴 이력 있음)";

        if (CurrentPurificationRate >= 80f)
            return "Path C-1 (안식: 모든 호수 정화 & 정화율 80% 이상)";

        return "Path C-2 (불완전한 안식: 모든 호수 정화 & 정화율 80% 미만)";
    }
}
