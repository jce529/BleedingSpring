using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 물리 이동, 점프([K]), 대쉬([L])를 담당합니다. (SRP — 이동 물리만 책임)
/// IPlayerContext에 의존해 상태 변경과 무적 처리를 요청합니다. (DIP)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // ─── 이동 ────────────────────────────────────────────────────────────────

    [Header("이동")]
    [SerializeField] private float moveSpeed = 6f;

    // ─── 점프 [K] ────────────────────────────────────────────────────────────

    [Header("점프 [K]")]
    [SerializeField] private float     jumpForce        = 14f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;           // 발 아래 기준점
    [SerializeField] private float     groundCheckRadius = 0.15f;

    // ─── 대쉬 [L] ────────────────────────────────────────────────────────────

    [Header("대쉬 [L]")]
    [SerializeField] private float      dashSpeed          = 18f;
    [SerializeField] private float      dashDuration       = 0.18f;
    [SerializeField] private float      dashCooldown       = 0.8f;
    [SerializeField] private GameObject dashAfterimagePreab;  // 물 잔상 프리팹 (없어도 동작)
    [SerializeField] private float      afterimageInterval  = 0.03f;

    [Header("대쉬 단계")]
    [SerializeField, Range(0, 3)] private int dashStage = 0;

    // 단계별 무적 지속 시간: 0=없음, 1~3=단계적 증가
    private static readonly float[] DashInvincibilityDuration = { 0f, 0.05f, 0.10f, 0.18f };

    // ─── 공개 상태 ───────────────────────────────────────────────────────────

    public bool IsGrounded   { get; private set; }
    public bool FacingRight  { get; private set; } = true;
    public bool IsDashing    { get; private set; }
    public bool IsDashCooling { get; private set; }

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private IPlayerContext context;
    private Rigidbody2D    rb;
    private Animator       animator;

    // Animator 파라미터 해시 (없으면 무시됨)
    private static readonly int AnimIsMoving        = Animator.StringToHash("IsMoving");
    private static readonly int AnimIsGrounded      = Animator.StringToHash("IsGrounded");
    private static readonly int AnimIsDashing       = Animator.StringToHash("IsDashing");
    private static readonly int AnimVerticalVelocity = Animator.StringToHash("VerticalVelocity");

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    /// <summary>PlayerController.Start()에서 호출해야 합니다.</summary>
    public void Initialize(IPlayerContext playerContext)
    {
        context  = playerContext;
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;
    }

    // ─── 외부 호출 API ───────────────────────────────────────────────────────

    /// <summary>매 Update마다 PlayerController에서 호출합니다.</summary>
    public void Tick()
    {
        CheckGround();
        UpdateAnimator();
    }

    /// <summary>수평 이동. 대쉬 중에는 무시됩니다.</summary>
    public void Move(float horizontal)
    {
        if (IsDashing) return;

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);

        if (horizontal > 0f && !FacingRight) Flip();
        else if (horizontal < 0f && FacingRight) Flip();
    }

    /// <summary>점프를 시도합니다. 지면에 있을 때만 성공(true)합니다.</summary>
    public bool TryJump()
    {
        if (!IsGrounded) return false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        return true;
    }

    /// <summary>대쉬를 시도합니다. 쿨다운 중이거나 이미 대쉬 중이면 실패(false)합니다.</summary>
    public bool TryDash()
    {
        if (IsDashCooling || IsDashing) return false;
        StartCoroutine(DashCoroutine());
        return true;
    }

    // ─── 내부 로직 ───────────────────────────────────────────────────────────

    private void CheckGround()
    {
        if (groundCheck == null) return;
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private IEnumerator DashCoroutine()
    {
        IsDashCooling = true;
        IsDashing     = true;

        float direction       = FacingRight ? 1f : -1f;
        float invincibleLeft  = DashInvincibilityDuration[Mathf.Clamp(dashStage, 0, 3)];
        float originalGravity = rb.gravityScale;
        float elapsed         = 0f;

        rb.gravityScale = 0f;

        // 1단계 이상: 물 잔상 스폰
        Coroutine afterimageRoutine = null;
        if (dashStage >= 1 && dashAfterimagePreab != null)
            afterimageRoutine = StartCoroutine(SpawnAfterimages());

        if (invincibleLeft > 0f)
            context.SetInvincible(true);

        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);
            elapsed += Time.deltaTime;

            if (invincibleLeft > 0f)
            {
                invincibleLeft -= Time.deltaTime;
                if (invincibleLeft <= 0f)
                    context.SetInvincible(false);
            }

            yield return null;
        }

        // 정리
        if (afterimageRoutine != null) StopCoroutine(afterimageRoutine);
        context.SetInvincible(false);

        rb.gravityScale   = originalGravity;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        IsDashing         = false;

        yield return new WaitForSeconds(dashCooldown);
        IsDashCooling = false;
    }

    private IEnumerator SpawnAfterimages()
    {
        var originalSr = GetComponent<SpriteRenderer>();

        while (true)
        {
            GameObject ghost = Instantiate(dashAfterimagePreab, transform.position, transform.rotation);

            if (originalSr != null)
            {
                var ghostSr = ghost.GetComponent<SpriteRenderer>();
                if (ghostSr != null) ghostSr.sprite = originalSr.sprite;
            }

            Destroy(ghost, 0.25f);
            yield return new WaitForSeconds(afterimageInterval);
        }
    }

    private void Flip()
    {
        FacingRight    = !FacingRight;
        Vector3 scale  = transform.localScale;
        scale.x       *= -1f;
        transform.localScale = scale;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(AnimIsMoving,          context.CurrentState == PlayerState.Moving);
        animator.SetBool(AnimIsGrounded,        IsGrounded);
        animator.SetBool(AnimIsDashing,         IsDashing);
        animator.SetFloat(AnimVerticalVelocity, rb.linearVelocity.y);
    }

    // ─── 외부 인터페이스 ─────────────────────────────────────────────────────

    public void SetDashStage(int stage) => dashStage = Mathf.Clamp(stage, 0, 3);
    public int  DashStage => dashStage;

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
