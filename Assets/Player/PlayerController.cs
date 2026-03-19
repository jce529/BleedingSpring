using UnityEngine;

/// <summary>
/// 플레이어의 최상위 오케스트레이터. IPlayerContext를 구현해 하위 컴포넌트에 컨텍스트를 제공합니다.
///
/// 책임 (SRP):
///   - 입력 읽기 및 위임
///   - PlayerState 상태 머신 관리
///   - GameStateManager 연동
///
/// 의존 관계 (DIP):
///   - PlayerMovement, SkillBase 는 IPlayerContext(추상)에만 의존합니다.
///   - 새 스킬 추가 시 이 클래스를 수정할 필요가 없습니다. (OCP)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour, IPlayerContext
{
    // ─── IPlayerContext 구현 ─────────────────────────────────────────────────

    public bool        FacingRight  => movement.FacingRight;
    public Rigidbody2D Rigidbody    { get; private set; }
    public PlayerStats Stats        { get; private set; }
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
    }

    public void SetInvincible(bool value) => isInvincible = value;
    public bool IsInvincible => isInvincible;

    // ─── 컴포넌트 참조 ───────────────────────────────────────────────────────

    private PlayerMovement   movement;

    // 각 스킬을 ISkill로 참조 — 구체 타입에 의존하지 않습니다. (DIP)
    private ISkill           basicAttack;
    private ISkill           wideSlash;
    private ISkill           projectile;

    private bool             isInvincible;
    private Animator         animator;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
        Stats     = GetComponent<PlayerStats>();
        movement  = GetComponent<PlayerMovement>();

        // MonoBehaviour는 인터페이스로 직접 GetComponent 불가 — 구체 타입으로 꺼낸 뒤 ISkill로 대입합니다.
        basicAttack = GetComponent<BasicAttackSkill>()  as ISkill;
        wideSlash   = GetComponent<WideSlashSkill>()    as ISkill;
        projectile  = GetComponent<ProjectileSkill>()   as ISkill;

        TryGetComponent(out animator);   // Animator는 선택 사항
    }

    private void Start()
    {
        // 의존성 주입: 각 하위 컴포넌트에 IPlayerContext(this)를 전달합니다.
        movement.Initialize(this);

        InitSkill(basicAttack);
        InitSkill(wideSlash);
        InitSkill(projectile);

        Stats.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (Stats != null) Stats.OnDeath -= HandleDeath;
    }

    // ─── 메인 루프 ───────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) return;

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState != GameStateManager.GameState.Playing) return;

        movement.Tick();

        HandleGroundStateTransitions();
        HandleMovementInput();
        HandleJumpInput();
        HandleDashInput();
        HandleCombatInput();
    }

    // ─── 상태 전이 ───────────────────────────────────────────────────────────

    private void HandleGroundStateTransitions()
    {
        float vy = Rigidbody.linearVelocity.y;

        if (CurrentState == PlayerState.Jumping && vy < 0f)
            ChangeState(PlayerState.Falling);
        else if (CurrentState == PlayerState.Falling && movement.IsGrounded)
            ChangeState(PlayerState.Idle);

        // 대쉬가 끝나면 자동으로 Idle/Falling 으로 복귀
        if (CurrentState == PlayerState.Dashing && !movement.IsDashing)
            ChangeState(movement.IsGrounded ? PlayerState.Idle : PlayerState.Falling);
    }

    // ─── 입력 처리 ───────────────────────────────────────────────────────────

    private void HandleMovementInput()
    {
        if (CurrentState == PlayerState.Dashing) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        movement.Move(horizontal);

        if (horizontal != 0f)
        {
            if (movement.IsGrounded && CurrentState != PlayerState.Attacking)
                ChangeState(PlayerState.Moving);
        }
        else if (movement.IsGrounded && CurrentState == PlayerState.Moving)
        {
            ChangeState(PlayerState.Idle);
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.K) && movement.TryJump())
            ChangeState(PlayerState.Jumping);
    }

    private void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.L) && movement.TryDash())
            ChangeState(PlayerState.Dashing);
    }

    private void HandleCombatInput()
    {
        if (CurrentState == PlayerState.Dashing) return;

        if (Input.GetKeyDown(KeyCode.J) && basicAttack != null) basicAttack.TryUse();
        if (Input.GetKeyDown(KeyCode.U) && wideSlash   != null) wideSlash.TryUse();
        if (Input.GetKeyDown(KeyCode.I) && projectile  != null) projectile.TryUse();
    }

    // ─── 유틸리티 ────────────────────────────────────────────────────────────

    private void InitSkill(ISkill skill)
    {
        if (skill != null) skill.Initialize(this);
    }

    // ─── 사망 처리 ───────────────────────────────────────────────────────────

    private void HandleDeath()
    {
        ChangeState(PlayerState.Dead);
        Rigidbody.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetTrigger("Death");
    }
}
