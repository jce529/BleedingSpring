using UnityEngine;

/// <summary>
/// 보스 방 진입을 감지하는 트리거 클래스.
/// 플레이어가 트리거 영역에 들어오면 BossUIManager를 통해 보스 UI를 활성화합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BossRoomTrigger : MonoBehaviour
{
    [Header("보스 설정")]
    [SerializeField] private BossStats bossStats;
    [SerializeField] private string playerTag = "Player";

    private bool _hasTriggered = false;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            if (bossStats != null)
            {
                if (BossUIManager.Instance != null)
                {
                    BossUIManager.Instance.ShowBossUI(bossStats);
                    _hasTriggered = true;
                    Debug.Log($"[BossRoomTrigger] Player entered. Showing UI for {bossStats.BossName}");
                }
                else
                {
                    Debug.LogError("[BossRoomTrigger] BossUIManager instance not found!");
                }
            }
            else
            {
                Debug.LogWarning("[BossRoomTrigger] BossStats reference is missing!");
            }
        }
    }

    /// <summary>
    /// 필요 시 트리거를 초기화하여 재사용할 수 있도록 합니다.
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
    }
}
