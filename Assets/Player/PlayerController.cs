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
    // ─── 싱글톤 ──────────────────────────────────────────────────────────────

    public static PlayerController Instance { get; private set; }

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

    public PlayerStanceManager StanceManager { get; private set; }

    // ─── 컴포넌트 참조 ───────────────────────────────────────────────────────

    private PlayerMovement movement;
    private InputHandler   inputHandler;
    private ISkill         basicAttack;
    private bool           isInvincible;
    private Animator       animator;

    // 이동 입력 (InputHandler.OnMove 이벤트로 갱신)
    private Vector2 moveInput;

    // 공격 콤보 (Attack1 → Attack2 → Attack3 → 반복)
    private int   attackCombo    = 0;
    private float lastAttackTime = -99f;

    // 패링 타이머
    [SerializeField] private float parryDuration = 0.35f;   // 패링 판정 창 (초)
    private float parryTimer;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Rigidbody    = GetComponent<Rigidbody2D>();
        Stats        = GetComponent<PlayerWaterStats>();
        movement     = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<InputHandler>();

        StanceManager = GetComponent<PlayerStanceManager>();

        // 기본 공격 캐싱
        basicAttack = GetComponent<BasicAttackSkill>() as ISkill;

        TryGetComponent(out animator);
    }

    private void Start()
    {
        movement.Initialize(this);
        basicAttack?.Initialize(this);

        StanceManager?.Initialize(this);

        inputHandler.OnMove            += HandleMoveInput;
        inputHandler.OnJump            += HandleJumpInput;
        inputHandler.OnDash            += HandleDashInput;
        inputHandler.OnBasicAttack     += HandleBasicAttackInput;
        inputHandler.OnWideSlash       += HandleMainSkillInput;
        inputHandler.OnProjectile      += HandleSubSkillInput;
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
            inputHandler.OnWideSlash       -= HandleMainSkillInput;
            inputHandler.OnProjectile      -= HandleSubSkillInput;
            inputHandler.OnWaterTierSwitch -= HandleWaterTierSwitchInput;
        }

        if (Stats != null) Stats.OnDeath -= HandleDeath;
    }

    // ─── 메인 루프 ───────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) return;

        TickParry();
        movement.Tick();
        HandleGroundStateTransitions();
        ApplyMovement();
    }

    private void TickParry()
    {
        if (CurrentState != PlayerState.Parrying) return;

        parryTimer -= Time.deltaTime;
        if (parryTimer <= 0f)
            ChangeState(PlayerState.Idle);
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

        // 슬롯 0: 기본 공격 (캐싱된 인스턴스 사용)
        if (basicAttack == null) return;
        basicAttack.TryUse();

        // 1초 이상 지나면 콤보 리셋
        if (Time.time - lastAttackTime > 1.0f)
            attackCombo = 0;

        attackCombo    = (attackCombo % 3) + 1;   // 1 → 2 → 3 → 1
        lastAttackTime = Time.time;

        animator?.SetTrigger("Attack" + attackCombo);
    }

    private void HandleMainSkillInput()
    {
        if (CurrentState == PlayerState.Dashing)  return;
        if (CurrentState == PlayerState.Attacking) return;

        // 현재 활성 태세의 메인 스킬 호출
        ISkill skill = StanceManager?.GetActiveMainSkill();
        if (skill == null) return;

        skill.TryUse();
        animator?.SetTrigger("Attack1");
    }

    private void HandleSubSkillInput()
    {
        if (CurrentState == PlayerState.Dashing)  return;
        if (CurrentState == PlayerState.Attacking) return;
        if (CurrentState == PlayerState.Dead)     return;

        // 0단계: 패링 / 1~3단계: 서브 스킬
        if (Stats.WaterTier == 0)
        {
            HandleParryInput();
        }
        else
        {
            // 현재 활성 태세의 서브 스킬 호출
            ISkill skill = StanceManager?.GetActiveSubSkill();
            if (skill == null) return;

            skill.TryUse();
            animator?.SetTrigger("Attack2");
        }
    }

    private void HandleParryInput()
    {
        // 패링 중 재발동 불가, 이미 패링 창이 열려 있으면 무시
        if (CurrentState == PlayerState.Parrying) return;

        ChangeState(PlayerState.Parrying);
        parryTimer = parryDuration;
        animator?.SetTrigger("Block");   // 애니메이터에 Block 트리거가 없으면 무시됨
        Debug.Log("[PlayerController] 패링 시도");
    }

    private void HandleWaterTierSwitchInput()
    {
        Stats.CycleWaterTier();
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
