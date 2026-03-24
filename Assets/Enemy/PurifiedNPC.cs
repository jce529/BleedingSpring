using UnityEngine;

/// <summary>
/// 적이 정화(Purify)될 때 활성화되는 NPC 컴포넌트.
/// EnemyStats.Purify()에서 Activate()를 호출해 적 → NPC로 전환합니다.
///
/// 전환 시 처리:
///   - EnemyAttack, EnemyStats 비활성화 (적 AI 중지)
///   - Rigidbody2D를 Kinematic으로 변경 (물리 고정)
///   - 스프라이트 색상을 정화 색상으로 변경
///   - 플레이어가 근처에 오면 상호작용 프롬프트 표시 ([F] 키)
/// </summary>
public class PurifiedNPC : MonoBehaviour
{
    // ─── Inspector 설정 ───────────────────────────────────────────────────────

    [Header("NPC 정보")]
    [SerializeField] private string npcName = "정화된 존재";

    [TextArea(2, 5)]
    [SerializeField] private string[] dialogueLines = { "...나를 구해줘서 고마워." };

    [Header("상호작용")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("정화 색상")]
    [SerializeField] private Color purifiedColor = new Color(0.5f, 0.85f, 1f, 1f);

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private bool isActive;
    private bool playerInRange;
    private int dialogueIndex;
    private GUIStyle labelStyle;

    // ─── 활성화 ──────────────────────────────────────────────────────────────

    /// <summary>EnemyStats.Purify()에서 호출. 적 컴포넌트를 비활성화하고 NPC로 전환합니다.</summary>
    public void Activate()
    {
        // 태그 / 레이어 변경 — 플레이어 스킬의 enemyLayer 마스크에서 제외됨
        gameObject.tag = "NPC";
        gameObject.layer = LayerMask.NameToLayer("Interactable");

        // 적 컴포넌트 비활성화
        var enemyAttack = GetComponent<EnemyAttack>();
        if (enemyAttack != null) enemyAttack.enabled = false;

        // 트리거 콜라이더 비활성화 (공격 판정 제거)
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col.isTrigger) col.enabled = false;
        }

        // 스프라이트 색상 변경
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = purifiedColor;

        dialogueIndex = 0;
        isActive = true;

        Debug.Log($"[PurifiedNPC] {gameObject.name} → NPC '{npcName}'으로 전환됨 (태그: NPC, 레이어: Interactable)");
    }

    // ─── 상호작용 ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isActive) return;

        // 트리거 콜라이더 대신 OverlapCircle로 플레이어 근접 감지
        playerInRange = Physics2D.OverlapCircle(transform.position, interactionRadius, playerLayer);

        if (playerInRange && Input.GetKeyDown(interactKey))
            ShowNextDialogue();
    }

    private void ShowNextDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        string line = dialogueLines[dialogueIndex % dialogueLines.Length];
        Debug.Log($"[{npcName}] {line}");

        dialogueIndex++;
    }

    // ─── UI ──────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!isActive || !playerInRange) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box);
            labelStyle.fontSize = 14;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
        }

        string current = (dialogueLines != null && dialogueLines.Length > 0)
            ? dialogueLines[dialogueIndex % dialogueLines.Length]
            : "";

        GUI.Box(new Rect(Screen.width / 2f - 200f, Screen.height - 120f, 400f, 80f),
                $"[{npcName}]\n{current}\n[{interactKey}] 대화", labelStyle);
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
