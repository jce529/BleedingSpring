using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 물리 이동, 점프([K]), 대쉬([L])를 담당합니다. (SRP — 이동 물리만 책임)
/// IPlayerContext에 의존해 상태 변경과 무적 처리를 요청합니다. (DIP)
/// Animator 파라미터는 HeroKnight_AnimController 기준입니다.
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
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float     groundCheckRadius = 0.15f;

    // ─── 대쉬 [L] ────────────────────────────────────────────────────────────

    [Header("대쉬 [L]")]
    [SerializeField] private float      dashSpeed          = 18f;
    [SerializeField] private float      dashDuration       = 0.18f;
    [SerializeField] private float      dashCooldown       = 0.8f;
    [SerializeField] private GameObject dashAfterimagePreab;
    [SerializeField] private float      afterimageInterval  = 0.03f;

    [Header("대쉬 단계")]
    [SerializeField, Range(0, 3)] private int dashStage = 0;

    private static readonly float[] DashInvincibilityDuration = { 0f, 0.05f, 0.10f, 0.18f };

    // ─── 공개 상태 ───────────────────────────────────────────────────────────

    public bool IsGrounded    { get; private set; }
    public bool FacingRight   { get; private set; } = true;
    public bool IsDashing     { get; private set; }
    public bool IsDashCooling { get; private set; }

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private IPlayerContext context;
    private Rigidbody2D    rb;
    private Animator       animator;

    // HeroKnight_AnimController 파라미터 해시
    private static readonly int AnimState    = Animator.StringToHash("AnimState");   // Int: 0=Idle, 1=Run
    private static readonly int AnimGrounded = Animator.StringToHash("Grounded");    // Bool
    private static readonly int AnimAirSpeedY = Animator.StringToHash("AirSpeedY"); // Float

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    public void Initialize(IPlayerContext playerContext)
    {
        context  = playerContext;
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;
    }

    // ─── 외부 호출 API ───────────────────────────────────────────────────────

    public void Tick()
    {
        CheckGround();
        UpdateAnimator();
    }

    public void Move(float horizontal)
    {
        if (IsDashing) return;

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);

        if (horizontal > 0f && !FacingRight) Flip();
        else if (horizontal < 0f && FacingRight) Flip();
    }

    public bool TryJump()
    {
        if (!IsGrounded) return false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        return true;
    }

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
        FacingRight   = !FacingRight;
        Vector3 scale = transform.localScale;
        scale.x      *= -1f;
        transform.localScale = scale;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // AnimState: 0=Idle, 1=Run
        bool isMoving = context.CurrentState == PlayerState.Moving;
        animator.SetInteger(AnimState, isMoving ? 1 : 0);
        animator.SetBool(AnimGrounded, IsGrounded);
        animator.SetFloat(AnimAirSpeedY, rb.linearVelocity.y);
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
