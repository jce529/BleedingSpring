using UnityEngine;

/// <summary>
/// 플레이어의 최상위 오케스트레이터. IPlayerContext를 구현해 하위 컴포넌트에 컨텍스트를 제공합니다.
/// Animator 파라미터는 HeroKnight_AnimController 기준입니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerWaterStats))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(InputHandler))]
public class PlayerController : MonoBehaviour, IPlayerContext
{
    // ─── IPlayerContext 구현 ─────────────────────────────────────────────────

    public bool             FacingRight  => movement.FacingRight;
    public Rigidbody2D      Rigidbody    { get; private set; }
    public PlayerWaterStats Stats        { get; private set; }
    public PlayerState      CurrentState { get; private set; } = PlayerState.Idle;

    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
    }

    public void SetInvincible(bool value) => isInvincible = value;
    public bool IsInvincible => isInvincible;

    // ─── 컴포넌트 참조 ───────────────────────────────────────────────────────

    private PlayerMovement movement;
    private InputHandler   inputHandler;
    private ISkill         basicAttack;
    private ISkill         wideSlash;
    private ISkill         projectile;
    private bool           isInvincible;
    private Animator       animator;

    // 이동 입력 (InputHandler.OnMove 이벤트로 갱신)
    private Vector2 moveInput;

    // 공격 콤보 (Attack1 → Attack2 → Attack3 → 반복)
    private int   attackCombo    = 0;
    private float lastAttackTime = -99f;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        Rigidbody    = GetComponent<Rigidbody2D>();
        Stats        = GetComponent<PlayerWaterStats>();
        movement     = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<InputHandler>();

        basicAttack = GetComponent<BasicAttackSkill>()  as ISkill;
        wideSlash   = GetComponent<WideSlashSkill>()    as ISkill;
        projectile  = GetComponent<ProjectileSkill>()   as ISkill;

        TryGetComponent(out animator);
    }

    private void Start()
    {
        movement.Initialize(this);

        InitSkill(basicAttack);
        InitSkill(wideSlash);
        InitSkill(projectile);

        inputHandler.OnMove            += HandleMoveInput;
        inputHandler.OnJump            += HandleJumpInput;
        inputHandler.OnDash            += HandleDashInput;
        inputHandler.OnBasicAttack     += HandleBasicAttackInput;
        inputHandler.OnWideSlash       += HandleWideSlashInput;
        inputHandler.OnProjectile      += HandleProjectileInput;
        inputHandler.OnWaterTierSwitch += HandleWaterTierSwitchInput;

        Stats.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (inputHandler != null)
        {
            inputHandler.OnMove            -= HandleMoveInput;
            inputHandler.OnJump            -= HandleJumpInput;
            inputHandler.OnDash            -= HandleDashInput;
            inputHandler.OnBasicAttack     -= HandleBasicAttackInput;
            inputHandler.OnWideSlash       -= HandleWideSlashInput;
            inputHandler.OnProjectile      -= HandleProjectileInput;
            inputHandler.OnWaterTierSwitch -= HandleWaterTierSwitchInput;
        }

        if (Stats != null) Stats.OnDeath -= HandleDeath;
    }

    // ─── 메인 루프 ───────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) return;

        movement.Tick();
        HandleGroundStateTransitions();
        ApplyMovement();
    }

    // ─── 상태 전이 ───────────────────────────────────────────────────────────

    private void HandleGroundStateTransitions()
    {
        float vy = Rigidbody.linearVelocity.y;

        switch (CurrentState)
        {
            case PlayerState.Jumping:
                // 정점 통과 후 하강 시작
                if (vy < 0f)
                    ChangeState(PlayerState.Falling);
                break;

            case PlayerState.Falling:
                // 착지
                if (movement.IsGrounded)
                    ChangeState(PlayerState.Idle);
                break;

            case PlayerState.Idle:
            case PlayerState.Moving:
                // 발판에서 걸어서 떨어지는 경우 (점프 없이 낙하)
                if (!movement.IsGrounded && vy < 0f)
                    ChangeState(PlayerState.Falling);
                break;

            case PlayerState.Dashing:
                if (!movement.IsDashing)
                    ChangeState(movement.IsGrounded ? PlayerState.Idle : PlayerState.Falling);
                break;
        }
    }

    // ─── 이동 ────────────────────────────────────────────────────────────────

    private void ApplyMovement()
    {
        if (CurrentState == PlayerState.Dashing)  return;
        if (CurrentState == PlayerState.Attacking) return;

        float horizontal = moveInput.x;
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

    // ─── 입력 이벤트 핸들러 ──────────────────────────────────────────────────

    private void HandleMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    private void HandleJumpInput()
    {
        if (CurrentState == PlayerState.Attacking) return;
        if (movement.TryJump())
        {
            ChangeState(PlayerState.Jumping);
            animator?.SetTrigger("Jump");
        }
    }

    private void HandleDashInput()
    {
        if (CurrentState == PlayerState.Attacking) return;
        if (movement.TryDash())
        {
            ChangeState(PlayerState.Dashing);
            animator?.SetTrigger("Roll");
        }
    }

    private void HandleBasicAttackInput()
    {
        if (CurrentState == PlayerState.Dashing)  return;
        if (CurrentState == PlayerState.Attacking) return;
        basicAttack?.TryUse();

        // 1초 이상 지나면 콤보 리셋
        if (Time.time - lastAttackTime > 1.0f)
            attackCombo = 0;

        attackCombo    = (attackCombo % 3) + 1;   // 1 → 2 → 3 → 1
        lastAttackTime = Time.time;

        animator?.SetTrigger("Attack" + attackCombo);
    }

    private void HandleWideSlashInput()
    {
        if (CurrentState == PlayerState.Dashing)  return;
        if (CurrentState == PlayerState.Attacking) return;
        wideSlash?.TryUse();
        // WideSlash는 Attack1 애니메이션 사용 (별도 애니가 없으므로)
        animator?.SetTrigger("Attack1");
    }

    private void HandleProjectileInput()
    {
        if (CurrentState == PlayerState.Dashing)  return;
        if (CurrentState == PlayerState.Attacking) return;
        projectile?.TryUse();
        // Projectile도 Attack2 애니메이션 사용
        animator?.SetTrigger("Attack2");
    }

    private void HandleWaterTierSwitchInput()
    {
        Stats.CycleWaterTier();

        int tier = Stats.WaterTier;
        (basicAttack as SkillBase)?.SetStage(tier);
        (wideSlash   as SkillBase)?.SetStage(tier);
        (projectile  as SkillBase)?.SetStage(tier);
    }

    // ─── 유틸리티 ────────────────────────────────────────────────────────────

    private void InitSkill(ISkill skill)
    {
        if (skill != null) skill.Initialize(this);
    }

    // ─── 디버그 ──────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 20;
        style.normal.textColor = Color.yellow;

        float vy = Rigidbody != null ? Rigidbody.linearVelocity.y : 0f;

        string info =
            $"State     : {CurrentState}\n" +
            $"IsGrounded: {movement.IsGrounded}\n" +
            $"VelocityY : {vy:F2}\n" +
            $"IsDashing : {movement.IsDashing}\n" +
            $"WaterTier : {Stats.WaterTier}\n" +
            $"HP        : {Stats.CurrentCleanWater:F0}\n" +
            $"Corruption: {Stats.CurrentCorruption:F0}";

        GUI.Label(new Rect(10, 10, 400, 200), info, style);
    }

    // ─── 사망 처리 ───────────────────────────────────────────────────────────

    private void HandleDeath()
    {
        ChangeState(PlayerState.Dead);
        moveInput                = Vector2.zero;
        Rigidbody.linearVelocity = Vector2.zero;
        animator?.SetTrigger("Death");
    }
}
